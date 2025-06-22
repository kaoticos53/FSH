using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FSH.Framework.Core.Auth.Jwt;
using FSH.Framework.Core.Identity.Tokens.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FSH.Framework.Infrastructure.Auth.Jwt;

public class JwtTokenGenerator
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenGenerator(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public TokenResponse GenerateToken(string userId, string email, IList<Claim> claims, string? tenantId = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtOptions.Key);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] 
            { 
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email)
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.TokenExpirationInMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience
        };

        // Add additional claims
        if (claims?.Any() == true)
        {
            tokenDescriptor.Subject.AddClaims(claims);
        }

        // Add tenant claim if provided
        if (!string.IsNullOrEmpty(tenantId))
        {
            tokenDescriptor.Subject.AddClaim(new Claim("tenant", tenantId));
        }

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var refreshToken = GenerateRefreshToken();

        return new TokenResponse(
            tokenHandler.WriteToken(token),
            refreshToken,
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationInDays));
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
