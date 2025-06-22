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

namespace FSH.Framework.Infrastructure.Auth.Jwt;

public class JwtTokenValidator : IJwtTokenValidator
{
    private readonly JwtOptions _jwtOptions;
    private readonly IMultiTenantContextAccessor _multiTenantContextAccessor;

    public JwtTokenValidator(
        IOptions<JwtOptions> jwtOptions,
        IMultiTenantContextAccessor multiTenantContextAccessor)
    {
        _jwtOptions = jwtOptions.Value;
        _multiTenantContextAccessor = multiTenantContextAccessor;
    }

    public ClaimsPrincipal? ValidateToken(string token, bool validateLifetime = true)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new UnauthorizedException("Invalid token");
        }

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
            }, out var validatedToken);

            // Validate tenant if multi-tenancy is enabled
            var tenantClaim = principal.FindFirst("tenant")?.Value;
            if (!string.IsNullOrEmpty(tenantClaim))
            {
                var currentTenant = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Identifier;
                if (currentTenant != tenantClaim)
                {
                    throw new UnauthorizedException("Tenant validation failed");
                }
            }

            return principal;
        }
        catch (SecurityTokenExpiredException)
        {
            throw new UnauthorizedException("The token has expired");
        }
        catch (SecurityTokenException)
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
