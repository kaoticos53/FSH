using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Auth.Jwt;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Identity.Tokens;
using FSH.Framework.Core.Identity.Tokens.Features.Generate;
using FSH.Framework.Core.Identity.Tokens.Features.Refresh;
using FSH.Framework.Core.Identity.Tokens.Models;
using FSH.Framework.Infrastructure.Auth.Jwt;
using FSH.Framework.Infrastructure.Identity.Audit;
using FSH.Framework.Infrastructure.Identity.Users;
using FSH.Framework.Infrastructure.Tenant;
using FSH.Starter.Shared.Authorization;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FSH.Framework.Infrastructure.Identity.Tokens;
public sealed class TokenService : ITokenService
{
    private readonly UserManager<FshUser> _userManager;
    private readonly IMultiTenantContextAccessor<FshTenantInfo>? _multiTenantContextAccessor;
    private readonly JwtOptions _jwtOptions;
    private readonly IPublisher _publisher;
    public TokenService(IOptions<JwtOptions> jwtOptions, UserManager<FshUser> userManager, IMultiTenantContextAccessor<FshTenantInfo>? multiTenantContextAccessor, IPublisher publisher)
    {
        _jwtOptions = jwtOptions.Value;
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _multiTenantContextAccessor = multiTenantContextAccessor;
        _publisher = publisher;
    }

    public async Task<TokenResponse> GenerateTokenAsync(TokenGenerationCommand request, string ipAddress, CancellationToken cancellationToken)
    {
        var currentTenant = _multiTenantContextAccessor!.MultiTenantContext.TenantInfo;
        if (currentTenant == null) throw new UnauthorizedException();
        if (string.IsNullOrWhiteSpace(currentTenant.Id)
           || await _userManager.FindByEmailAsync(request.Email.Trim().Normalize()) is not { } user
           || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedException();
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("user is deactivated");
        }

        if (!user.EmailConfirmed)
        {
            throw new UnauthorizedException("email not confirmed");
        }

        if (currentTenant.Id != TenantConstants.Root.Id)
        {
            if (!currentTenant.IsActive)
            {
                throw new UnauthorizedException($"tenant {currentTenant.Id} is deactivated");
            }

            if (DateTime.UtcNow > currentTenant.ValidUpto)
            {
                throw new UnauthorizedException($"tenant {currentTenant.Id} validity has expired");
            }
        }

        // Obtener roles y claims personalizados del usuario
        var roles = await _userManager.GetRolesAsync(user);
        var userClaims = await _userManager.GetClaimsAsync(user);
        
        // Generar el token incluyendo roles y claims personalizados
        return await GenerateTokensAndUpdateUser(user, ipAddress, roles, userClaims);
    }


    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenCommand request, string ipAddress, CancellationToken cancellationToken)
    {
        var userPrincipal = GetPrincipalFromExpiredToken(request.Token);
        var userId = _userManager.GetUserId(userPrincipal)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            throw new UnauthorizedException();
        }

        if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid Refresh Token");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            throw new ForbiddenException("La autenticación falló. El usuario no está activo.");
        }

        return await GenerateTokensAndUpdateUser(user, ipAddress);
    }
    private async Task<TokenResponse> GenerateTokensAndUpdateUser(
        FshUser user, 
        string ipAddress,
        IList<string>? roles = null,
        IList<Claim>? claims = null)
    {
        // Generar el token con los claims estándar, roles y claims personalizados
        string token = GenerateJwt(user, ipAddress, roles, claims);

        user.RefreshToken = GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationInDays);

        await _userManager.UpdateAsync(user);

        await _publisher.Publish(new AuditPublishedEvent(new()
        {
            new()
            {
                Id = Guid.NewGuid(),
                Operation = "Token Generated",
                Entity = "Identity",
                UserId = new Guid(user.Id),
                DateTime = DateTime.UtcNow,
            }
        }));

        return new TokenResponse(token, user.RefreshToken, user.RefreshTokenExpiryTime);
    }

    private string GenerateJwt(FshUser user, string ipAddress, IList<string>? roles = null, IList<Claim>? customClaims = null) =>
        GenerateEncryptedToken(GetSigningCredentials(), GetClaims(user, ipAddress, roles, customClaims));

    private SigningCredentials GetSigningCredentials()
    {
        byte[] secret = Encoding.UTF8.GetBytes(_jwtOptions.Key);
        return new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256);
    }

    private string GenerateEncryptedToken(SigningCredentials signingCredentials, IEnumerable<Claim> claims)
    {
        var token = new JwtSecurityToken(
           claims: claims,
           expires: DateTime.UtcNow.AddMinutes(_jwtOptions.TokenExpirationInMinutes),
           signingCredentials: signingCredentials,
           issuer: JwtAuthConstants.Issuer,
           audience: JwtAuthConstants.Audience
           );
        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    private List<Claim> GetClaims(FshUser user, string ipAddress, IList<string>? roles = null, IList<Claim>? customClaims = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FirstName ?? string.Empty),
            new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
            new(FshClaims.Fullname, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.Surname, user.LastName ?? string.Empty),
            new(FshClaims.IpAddress, ipAddress),
            new(FshClaims.Tenant, _multiTenantContextAccessor!.MultiTenantContext.TenantInfo!.Id),
            new(FshClaims.ImageUrl, user.ImageUrl == null ? string.Empty : user.ImageUrl.ToString())
        };

        // Agregar roles como claims
        if (roles?.Count > 0)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        // Agregar claims personalizados si existen
        if (customClaims?.Count > 0)
        {
            claims.AddRange(customClaims);
        }

        return claims;
    }
    private static string GenerateRefreshToken()
    {
        byte[] randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
#pragma warning disable CA5404 // Do not disable token validation checks
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = JwtAuthConstants.Audience,
            ValidIssuer = JwtAuthConstants.Issuer,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero,
            ValidateLifetime = false
        };
#pragma warning restore CA5404 // Do not disable token validation checks
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken)
            {
                throw new UnauthorizedException("invalid token");
            }

            return principal;
        } 
        catch
        {
            throw new UnauthorizedException("invalid token");
        }
    }
}
