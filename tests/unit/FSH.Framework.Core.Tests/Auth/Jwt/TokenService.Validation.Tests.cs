using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Infrastructure.Auth.Jwt;
using FSH.Framework.Infrastructure.Tenant;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Xunit.Abstractions;
using Finbuckle.MultiTenant.Abstractions;

namespace FSH.Framework.Core.Tests.Auth.Jwt;

/// <summary>
/// Contains unit tests for JWT token validation functionality in the TokenService.
/// </summary>
public class TokenServiceValidationTests : TokenServiceTestBase
{
    private readonly JwtTokenValidator _tokenValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenServiceValidationTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper for logging test output.</param>
    public TokenServiceValidationTests(ITestOutputHelper output) : base(output)
    {
        _tokenValidator = new JwtTokenValidator(
            Options.Create(JwtOptions),
            MultiTenantContextAccessorMock.Object);
    }

    /// <summary>
    /// Tests that ValidateToken returns a valid principal when provided with a valid token.
    /// </summary>
    [Fact]
    public void ValidateToken_WithValidToken_ReturnsPrincipal()
    {
        // Arrange
        var token = TestToken;

        // Act
        var principal = _tokenValidator.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        Assert.True(principal.Identity?.IsAuthenticated);
        Assert.Equal(TestUserId, principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(TestUserEmail, principal.FindFirstValue(ClaimTypes.Email));
    }

    /// <summary>
    /// Tests that ValidateToken throws a SecurityTokenException when provided with an invalid token format.
    /// </summary>
    [Fact]
    public void ValidateToken_WithInvalidToken_ThrowsSecurityTokenException()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act & Assert
        Assert.Throws<SecurityTokenException>(
            () => _tokenValidator.ValidateToken(invalidToken));
    }

    /// <summary>
    /// Tests that ValidateToken throws a SecurityTokenExpiredException when provided with an expired token.
    /// </summary>
    [Fact]
    public void ValidateToken_WithExpiredToken_ThrowsSecurityTokenExpiredException()
    {
        // Arrange - Create an expired token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(JwtOptions.Key);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] 
            { 
                new Claim(ClaimTypes.NameIdentifier, TestUserId),
                new Claim(ClaimTypes.Email, TestUserEmail)
            }),
            Expires = DateTime.UtcNow.AddMinutes(-5), // Expired 5 minutes ago
            Issuer = JwtOptions.Issuer,
            Audience = JwtOptions.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };
        var expiredToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

        // Act & Assert
        Assert.Throws<SecurityTokenExpiredException>(
            () => _tokenValidator.ValidateToken(expiredToken));
    }

    /// <summary>
    /// Tests that ValidateToken throws a SecurityTokenInvalidIssuerException when provided with a token from an invalid issuer.
    /// </summary>
    [Fact]
    public void ValidateToken_WithInvalidIssuer_ThrowsSecurityTokenInvalidIssuerException()
    {
        // Arrange - Create token with invalid issuer
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(JwtOptions.Key);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] 
            { 
                new Claim(ClaimTypes.NameIdentifier, TestUserId)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "InvalidIssuer",
            Audience = JwtOptions.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };
        var invalidIssuerToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

        // Act & Assert
        Assert.Throws<SecurityTokenInvalidIssuerException>(
            () => _tokenValidator.ValidateToken(invalidIssuerToken));
    }

    /// <summary>
    /// Tests that ValidateToken throws a SecurityTokenInvalidAudienceException when provided with a token for an invalid audience.
    /// </summary>
    [Fact]
    public void ValidateToken_WithInvalidAudience_ThrowsSecurityTokenInvalidAudienceException()
    {
        // Arrange - Create token with invalid audience
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(JwtOptions.Key);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] 
            { 
                new Claim(ClaimTypes.NameIdentifier, TestUserId)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = JwtOptions.Issuer,
            Audience = "InvalidAudience",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };
        var invalidAudienceToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

        // Act & Assert
        Assert.Throws<SecurityTokenInvalidAudienceException>(
            () => _tokenValidator.ValidateToken(invalidAudienceToken));
    }

    /// <summary>
    /// Tests that ValidateToken throws a SecurityTokenSignatureKeyNotFoundException when provided with a token with an invalid signature.
    /// </summary>
    [Fact]
    public void ValidateToken_WithInvalidSignature_ThrowsSecurityTokenSignatureKeyNotFoundException()
    {
        // Arrange - Create token with invalid signature
        var tokenHandler = new JwtSecurityTokenHandler();
        var invalidKey = Encoding.ASCII.GetBytes("InvalidKeyWithDifferentLengthThanExpected");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] 
            { 
                new Claim(ClaimTypes.NameIdentifier, TestUserId)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = JwtOptions.Issuer,
            Audience = JwtOptions.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(invalidKey), 
                SecurityAlgorithms.HmacSha256Signature)
        };
        var invalidSignatureToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

        // Act & Assert
        Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(
            () => _tokenValidator.ValidateToken(invalidSignatureToken));
    }

    /// <summary>
    /// Tests that ValidateToken throws an UnauthorizedException when the token is missing a tenant claim.
    /// </summary>
    [Fact]
    public void ValidateToken_WithMissingTenant_ThrowsUnauthorizedException()
    {
        // Arrange - Create token without tenant claim
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(JwtOptions.Key);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] 
            { 
                new Claim(ClaimTypes.NameIdentifier, TestUserId),
                new Claim(ClaimTypes.Email, TestUserEmail)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = JwtOptions.Issuer,
            Audience = JwtOptions.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };
        var tokenWithoutTenant = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedException>(
            () => _tokenValidator.ValidateToken(tokenWithoutTenant));
            
        Assert.Equal("Invalid token: missing tenant identifier.", exception.Message);
    }

    /// <summary>
    /// Tests that ValidateToken throws an UnauthorizedException when the token's tenant doesn't match the current tenant context.
    /// </summary>
    [Fact]
    public void ValidateToken_WithInvalidTenant_ThrowsUnauthorizedException()
    {
        // Arrange - Setup tenant context with different tenant than token
        var differentTenantId = "different-tenant";
        var tenantInfo = new FshTenantInfo
        {
            Id = differentTenantId,
            Identifier = differentTenantId,
            Name = "Different Tenant",
            IsActive = true,
            ValidUpto = DateTime.UtcNow.AddYears(1)
        };
        var multiTenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
        MultiTenantContextAccessorMock.Setup(x => x.MultiTenantContext).Returns(multiTenantContext);

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedException>(
            () => _tokenValidator.ValidateToken(TestToken));
            
        Assert.Equal("Token inválido: el inquilino no coincide.", exception.Message);
    }
}
