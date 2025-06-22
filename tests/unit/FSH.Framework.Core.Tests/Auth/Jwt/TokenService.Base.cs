using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FSH.Framework.Core.Auth.Jwt;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Identity.Tokens;
using FSH.Framework.Core.Identity.Tokens.Features.Generate;
using FSH.Framework.Core.Identity.Tokens.Features.Refresh;
using FSH.Framework.Core.Identity.Tokens.Models;
using FSH.Framework.Infrastructure.Auth.Jwt;
using FSH.Framework.Infrastructure.Identity.Tokens;
using FSH.Framework.Infrastructure.Identity.Users;
using FSH.Framework.Infrastructure.Tenant;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Finbuckle.MultiTenant.Abstractions;

namespace FSH.Framework.Core.Tests.Auth.Jwt;

/// <summary>
/// Base class for TokenService tests
/// </summary>
public abstract class TokenServiceTestBase : IDisposable
{
    /// <summary>
    /// Output helper for test logging
    /// </summary>
    protected readonly ITestOutputHelper Output;
    
    /// <summary>
    /// Mock for UserManager
    /// </summary>
    protected readonly Mock<UserManager<FshUser>> UserManagerMock;
    
    /// <summary>
    /// Mock for IMultiTenantContextAccessor
    /// </summary>
    protected readonly Mock<IMultiTenantContextAccessor<FshTenantInfo>> MultiTenantContextAccessorMock;
    
    /// <summary>
    /// Mock for IPublisher
    /// </summary>
    protected readonly Mock<IPublisher> PublisherMock;
    
    /// <summary>
    /// JWT options for testing
    /// </summary>
    protected readonly JwtOptions JwtOptions;
    
    /// <summary>
    /// TokenService instance under test
    /// </summary>
    protected readonly ITokenService TokenService;
    
    // Test data
    /// <summary>
    /// Test tenant ID
    /// </summary>
    protected const string TestTenantId = "test-tenant-1";
    
    /// <summary>
    /// Test user ID
    /// </summary>
    protected const string TestUserId = "test-user-1";
    
    /// <summary>
    /// Test user email
    /// </summary>
    protected const string TestUserEmail = "test@example.com";
    
    /// <summary>
    /// Test user password
    /// </summary>
    protected const string TestPassword = "TestPass123!";
    
    /// <summary>
    /// Test IP address
    /// </summary>
    protected const string TestIpAddress = "127.0.0.1";
    
    /// <summary>
    /// Test refresh token
    /// </summary>
    /// <summary>
    /// Test refresh token
    /// </summary>
    protected string TestRefreshToken = "test-refresh-token";
    
    /// <summary>
    /// Test JWT token
    /// </summary>
    protected string TestToken = "test-token";

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenServiceTestBase"/> class
    /// </summary>
    /// <param name="output">Test output helper</param>
    protected TokenServiceTestBase(ITestOutputHelper output)
    {
        Output = output ?? throw new ArgumentNullException(nameof(output));
        
        // Setup UserManager mock
        var userStoreMock = new Mock<IUserStore<FshUser>>();
        UserManagerMock = new Mock<UserManager<FshUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        
        // Initialize MultiTenantContextAccessor mock
        MultiTenantContextAccessorMock = new Mock<IMultiTenantContextAccessor<FshTenantInfo>>();
        
        // Setup tenant info
        var tenantInfo = new FshTenantInfo
        {
            Id = TestTenantId,
            Identifier = TestTenantId,
            Name = "Test Tenant"
        };
        
        // Create and setup tenant context
        var tenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
        MultiTenantContextAccessorMock.Setup(x => x.MultiTenantContext).Returns(tenantContext);
        
        PublisherMock = new Mock<IPublisher>();
        
        // Default JWT configuration
        JwtOptions = new JwtOptions
        {
            Key = "super-secret-key-with-at-least-32-characters",
            Issuer = "test-issuer",
            Audience = "test-audience",
            TokenExpirationInMinutes = 60,
            RefreshTokenExpirationInDays = 7
        };
        
        // Generate test tokens if not already set
        if (string.IsNullOrEmpty(TestToken))
        {
            TestToken = GenerateTestToken(JwtOptions.Key);
        }
        
        if (string.IsNullOrEmpty(TestRefreshToken))
        {
            TestRefreshToken = GenerateRefreshToken();
        }
        
        // Create the TokenService instance with the correct constructor
        TokenService = new TokenService(
            Options.Create(JwtOptions),
            UserManagerMock.Object,
            MultiTenantContextAccessorMock.Object,
            PublisherMock.Object);
    }
    
    /// <summary>
    /// Generates a cryptographically secure random refresh token.
    /// </summary>
    /// <returns>A base64-encoded random string to be used as a refresh token.</returns>
    protected static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
    
    /// <summary>
    /// Generates a test JWT token with the specified parameters.
    /// </summary>
    /// <param name="key">The secret key used to sign the token.</param>
    /// <param name="userId">The user ID to include in the token claims.</param>
    /// <param name="email">The email to include in the token claims.</param>
    /// <returns>A JWT token string.</returns>
    protected static string GenerateTestToken(string key, string userId = "test-user-id", string email = "test@test.com")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var keyBytes = Encoding.ASCII.GetBytes(key);
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(60),
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes), 
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    
    /// <summary>
    /// Creates a test user with the specified parameters.
    /// </summary>
    /// <param name="userId">The user ID. If null, uses TestUserId.</param>
    /// <param name="email">The user email. If null, uses TestUserEmail.</param>
    /// <param name="isActive">Whether the user is active. Default is true.</param>
    /// <param name="emailConfirmed">Whether the user's email is confirmed. Default is true.</param>
    /// <param name="tenantId">The tenant ID. If null, uses TestTenantId.</param>
    /// <returns>A new instance of FshUser with the specified properties.</returns>
    protected FshUser CreateTestUser(
        string? userId = null,
        string? email = null,
        bool isActive = true,
        bool emailConfirmed = true,
        string? tenantId = null)
    {
        return new FshUser
        {
            Id = userId ?? TestUserId,
            Email = email ?? TestUserEmail,
            UserName = email ?? TestUserEmail,
            IsActive = isActive,
            EmailConfirmed = emailConfirmed,
            TenantId = tenantId ?? TestTenantId,
            RefreshToken = TestRefreshToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };
    }
    
    /// <summary>
    /// Sets up the UserManager mock to simulate successful user operations.
    /// </summary>
    /// <param name="user">The user to set up in the mock.</param>
    /// <param name="password">The password to use for password verification. If null, password verification is not set up.</param>
    protected void SetupUserManagerForSuccess(FshUser user, string? password = null)
    {
        // Use null-conditional operator to safely access Email property
        var userEmail = user.Email ?? throw new ArgumentNullException(nameof(user.Email), "User email cannot be null");
        UserManagerMock.Setup(x => x.FindByEmailAsync(userEmail))
            .ReturnsAsync(user);
            
        if (password != null)
        {
            UserManagerMock.Setup(x => x.CheckPasswordAsync(user, password))
                .Returns(Task.FromResult(true)); // Simulate successful password check
        }
        
        UserManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });
            
        UserManagerMock.Setup(x => x.GetClaimsAsync(user))
            .ReturnsAsync(new List<Claim>());
            
        UserManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);
    }
    
    /// <summary>
    /// Verifies that a token generated event was published exactly once.
    /// </summary>
    protected void VerifyTokenGeneratedEventPublished()
    {
        PublisherMock.Verify(
            x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    /// <summary>
    /// Verifies that a token refreshed event was published exactly once.
    /// </summary>
    protected void VerifyTokenRefreshedEventPublished()
    {
        PublisherMock.Verify(
            x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    /// <summary>
    /// Verifies that no events were published.
    /// </summary>
    protected void VerifyNoEventsPublished()
    {
        PublisherMock.Verify(
            x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    /// <summary>
    /// Asserts that a token response is valid and contains the expected claims.
    /// </summary>
    /// <param name="response">The token response to validate.</param>
    /// <param name="userId">The expected user ID in the token claims.</param>
    /// <param name="email">The expected email in the token claims.</param>
    /// <param name="tenantId">The expected tenant ID in the token claims.</param>
    protected void AssertValidTokenResponse(TokenResponse response, string userId, string email, string tenantId)
    {
        Assert.NotNull(response);
        Assert.False(string.IsNullOrEmpty(response.Token));
        
        // Verify token contains expected claims
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(response.Token);
        
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId);
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Email && c.Value == email);
        Assert.Contains(jwtToken.Claims, c => c.Type == "tenant" && c.Value == tenantId);
    }
    
    /// <summary>
    /// Verifies that the specified JWT token contains a claim with the given type and optional value.
    /// </summary>
    /// <param name="token">The JWT token to verify.</param>
    /// <param name="claimType">The type of the claim to verify.</param>
    /// <param name="expectedValue">The expected value of the claim, or null to only check for existence.</param>
    protected static void AssertTokenContainsClaim(string token, string claimType, string? expectedValue = null)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        
        var claim = jwt.Claims.FirstOrDefault(c => c.Type == claimType);
        Assert.NotNull(claim);
        
        if (expectedValue != null)
        {
            Assert.Equal(expectedValue, claim.Value);
        }
    }

    /// <summary>
    /// Performs cleanup of resources used by the test class.
    /// </summary>
    public virtual void Dispose()
    {
        // Cleanup if needed
    }
}
