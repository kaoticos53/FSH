using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Identity.Tokens.Events;
using FSH.Framework.Core.Identity.Tokens.Features.Refresh;
using FSH.Framework.Infrastructure.Identity.Audit;
using FSH.Framework.Infrastructure.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
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
        SetupUserManagerForSuccess(user, TestPassword);
        var request = new RefreshTokenCommand(TestToken, TestRefreshToken);
        var ipAddress = TestIpAddress;
        
        UserManagerMock.Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
            
        UserManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Setup event publishing verification for AuditPublishedEvent
        AuditPublishedEvent? publishedEvent = null;
        PublisherMock.Setup(x => x.Publish(It.IsAny<AuditPublishedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditPublishedEvent, CancellationToken>((e, _) =>
            {
                publishedEvent = e;
                Output.WriteLine($"Audit event published: {e?.GetType().Name}, UserId: {e?.Trails?.FirstOrDefault()?.UserId}");
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await TokenService.RefreshTokenAsync(request, ipAddress, CancellationToken.None);

        // Assert
        AssertValidTokenResponse(result, user.Id, user.Email!, TestTenantId);
        
        // Verify refresh token was updated
        Assert.NotNull(user.RefreshToken);
        Assert.NotEqual(TestRefreshToken, user.RefreshToken);
        Assert.True(user.RefreshTokenExpiryTime > DateTime.UtcNow);

        // Verify token generated event was published
        PublisherMock.Verify(
            x => x.Publish(It.IsAny<AuditPublishedEvent>(), It.IsAny<CancellationToken>()),
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
            
        Assert.Equal("invalid token", exception.Message);
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
        SetupUserManagerForSuccess(user, TestPassword);

        var request = new RefreshTokenCommand(TestToken, TestRefreshToken);
        var ipAddress = TestIpAddress;
        
        UserManagerMock.Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForbiddenException>(
            () => TokenService.RefreshTokenAsync(request, ipAddress, CancellationToken.None));
            
        Assert.Equal("La autenticación falló. El usuario no está activo.", exception.Message);
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
        SetupUserManagerForSuccess(user, TestPassword);
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1); // Expired yesterday
        
        var request = new RefreshTokenCommand(TestToken, TestRefreshToken);
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
    /// Tests that RefreshTokenAsync throws an UnauthorizedException when the provided refresh token does not match the stored one.
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithMismatchedRefreshToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser();
        SetupUserManagerForSuccess(user, TestPassword);
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
        SetupUserManagerForSuccess(user, TestPassword);
        user.RefreshToken = null; // Simulate revoked token
        
        var request = new RefreshTokenCommand(TestToken, TestRefreshToken);
        var ipAddress = TestIpAddress;
        
        UserManagerMock.Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => TokenService.RefreshTokenAsync(request, ipAddress, CancellationToken.None));
            
        Assert.Equal("Invalid Refresh Token", exception.Message);
        VerifyNoEventsPublished();
    }
}
