using System;
using System.Threading;
using System.Threading.Tasks;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Identity.Tokens.Events;
using FSH.Framework.Core.Identity.Tokens.Features.Refresh;
using FSH.Framework.Infrastructure.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace FSH.Framework.Core.Tests.Auth.Jwt;

/// <summary>
/// Contains unit tests for the token refresh functionality of TokenService.
/// </summary>
public class TokenServiceRefreshTokenTests : TokenServiceTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenServiceRefreshTokenTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper for logging test output.</param>
    public TokenServiceRefreshTokenTests(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// Tests that RefreshTokenAsync returns a new token response when provided with a valid token and refresh token.
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokenResponse()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new RefreshTokenCommand(TestToken, TestRefreshToken);
        var ipAddress = TestIpAddress;
        
        UserManagerMock.Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
            
        UserManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await TokenService.RefreshTokenAsync(request, ipAddress, CancellationToken.None);

        // Assert
        AssertValidTokenResponse(result, user.Id, user.Email!, TestTenantId);
        
        // Verify refresh token was updated
        Assert.NotNull(user.RefreshToken);
        Assert.NotEqual(TestRefreshToken, user.RefreshToken);
        Assert.True(user.RefreshTokenExpiryTime > DateTime.UtcNow);
        
        // Verify token generated event was published
        PublisherMock.Verify(x => x.Publish(It.Is<TokenGeneratedEvent>(e => 
            e.UserId == user.Id &&
            !string.IsNullOrEmpty(e.Token) &&
            e.RefreshToken == user.RefreshToken &&
            e.RefreshTokenExpiryTime == user.RefreshTokenExpiryTime),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that RefreshTokenAsync throws an UnauthorizedException when provided with an invalid token.
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithInvalidToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var invalidToken = "invalid.token.here";
        var request = new RefreshTokenCommand(invalidToken, TestRefreshToken);
        var ipAddress = TestIpAddress;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => TokenService.RefreshTokenAsync(request, ipAddress, CancellationToken.None));
            
        Assert.Equal("Invalid security token.", exception.Message);
        VerifyNoEventsPublished();
    }

    /// <summary>
    /// Tests that RefreshTokenAsync throws an UnauthorizedException when provided with an expired token.
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithExpiredToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var expiredToken = GenerateTestToken("expired-key", TestUserId, TestUserEmail);
        var request = new RefreshTokenCommand(expiredToken, TestRefreshToken);
        var ipAddress = TestIpAddress;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => TokenService.RefreshTokenAsync(request, ipAddress, CancellationToken.None));
            
        Assert.Equal("Invalid or expired security token.", exception.Message);
        VerifyNoEventsPublished();
    }

    /// <summary>
    /// Tests that RefreshTokenAsync throws an UnauthorizedException when the user does not exist.
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithNonexistentUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new RefreshTokenCommand(TestToken, TestRefreshToken);
        var ipAddress = TestIpAddress;
        
        UserManagerMock.Setup(x => x.FindByIdAsync(TestUserId))
            .ReturnsAsync(default(FshUser)!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => TokenService.RefreshTokenAsync(request, ipAddress, CancellationToken.None));
            
        Assert.Equal("User not found.", exception.Message);
        VerifyNoEventsPublished();
    }

    /// <summary>
    /// Tests that RefreshTokenAsync throws an UnauthorizedException when the user account is inactive.
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithInactiveUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser(isActive: false);
        var request = new RefreshTokenCommand(TestToken, TestRefreshToken);
        var ipAddress = TestIpAddress;
        
        UserManagerMock.Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => TokenService.RefreshTokenAsync(request, ipAddress, CancellationToken.None));
            
        Assert.Equal("User is not active. Please contact the administrator.", exception.Message);
        VerifyNoEventsPublished();
    }

    /// <summary>
    /// Tests that RefreshTokenAsync throws an UnauthorizedException when the refresh token has expired.
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithExpiredRefreshToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1); // Expired yesterday
        
        var request = new RefreshTokenCommand(TestToken, TestRefreshToken);
        var ipAddress = TestIpAddress;
        
        UserManagerMock.Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => TokenService.RefreshTokenAsync(request, ipAddress, CancellationToken.None));
            
        Assert.Equal("Refresh token has expired. Please log in again.", exception.Message);
        VerifyNoEventsPublished();
    }

    /// <summary>
    /// Tests that RefreshTokenAsync throws an UnauthorizedException when the provided refresh token does not match the stored one.
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithMismatchedRefreshToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser();
        var invalidRefreshToken = GenerateRefreshToken(); // Different from user's refresh token
        
        var request = new RefreshTokenCommand(TestToken, invalidRefreshToken);
        var ipAddress = TestIpAddress;
        
        UserManagerMock.Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => TokenService.RefreshTokenAsync(request, ipAddress, CancellationToken.None));
            
        Assert.Equal("Invalid Refresh Token", exception.Message);
        VerifyNoEventsPublished();
    }

    /// <summary>
    /// Tests that RefreshTokenAsync throws an UnauthorizedException when the refresh token has been revoked.
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithRevokedRefreshToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser();
        user.RefreshToken = null; // Simulate revoked token
        
        var request = new RefreshTokenCommand(TestToken, TestRefreshToken);
        var ipAddress = TestIpAddress;
        
        UserManagerMock.Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => TokenService.RefreshTokenAsync(request, ipAddress, CancellationToken.None));
            
        Assert.Equal("Refresh token has been revoked.", exception.Message);
        VerifyNoEventsPublished();
    }

    /// <summary>
    /// Tests that RefreshTokenAsync throws an UnauthorizedException when the request comes from a different IP address than the original request.
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithDifferentIpAddress_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser();
        var differentIpAddress = "192.168.1.100";
        
        // Set the IP address in the token's claims
        var tokenWithIp = GenerateTestToken(JwtOptions.Key, TestUserId, TestUserEmail);
        
        var request = new RefreshTokenCommand(tokenWithIp, TestRefreshToken);
        
        UserManagerMock.Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => TokenService.RefreshTokenAsync(request, differentIpAddress, CancellationToken.None));
            
        Assert.Equal("IP address mismatch. For security reasons, refresh tokens can only be used from the same IP address where they were issued.", exception.Message);
        VerifyNoEventsPublished();
    }
}
