using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FSH.Framework.Core.Auth.Jwt;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Identity.Tokens.Models;
using FSH.Framework.Core.Tenancy;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using FSH.Framework.Infrastructure.Tenant;

namespace FSH.Framework.Infrastructure.Auth.Jwt;

public class JwtTokenValidator : IJwtTokenValidator
{
    private readonly JwtOptions _jwtOptions;
    private readonly IMultiTenantContextAccessor<FshTenantInfo> _multiTenantContextAccessor;

    public JwtTokenValidator(
        IOptions<JwtOptions> jwtOptions,
        IMultiTenantContextAccessor<FshTenantInfo> multiTenantContextAccessor)
    {
        _jwtOptions = jwtOptions.Value;
        _multiTenantContextAccessor = multiTenantContextAccessor;
    }

    public ClaimsPrincipal? ValidateToken(string token, bool validateLifetime = true)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedException("Invalid token");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtOptions.Key);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOptions.Audience,
                ValidateLifetime = validateLifetime,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var tenantClaim = principal.FindFirst("tenant")?.Value;
            var currentTenant = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Identifier;

            if (string.IsNullOrEmpty(tenantClaim) || currentTenant != tenantClaim)
                throw new UnauthorizedException("Tenant validation failed");

            return principal;
        }
        catch (SecurityTokenExpiredException)
        {
            throw new UnauthorizedException("The token has expired");
        }
        catch (UnauthorizedException) // Permite propagar el mensaje original de UnauthorizedException
        {
            throw;
        }
        catch (Exception)
        {
            throw new UnauthorizedException("Invalid token");
        }
    }

    public string? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public string? GetUserEmailFromToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst(ClaimTypes.Email)?.Value;
    }
}
