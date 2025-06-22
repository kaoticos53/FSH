using System;
using System.Threading;
using System.Threading.Tasks;
using FSH.Framework.Core.Events;
using FSH.Framework.Core.Identity.Tokens.Events;
using FSH.Framework.Core.Identity.Tokens.Features.Generate;
using FSH.Framework.Core.Identity.Tokens.Features.Refresh;
using FSH.Framework.Infrastructure.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace FSH.Framework.Core.Tests.Auth.Jwt;

/// <summary>
/// Contains unit tests for event publishing functionality of TokenService.
/// </summary>
public class TokenServiceEventPublishingTests : TokenServiceTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenServiceEventPublishingTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper for logging test output.</param>
    public TokenServiceEventPublishingTests(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// Tests that GenerateTokenAsync publishes a TokenGeneratedEvent with the correct data.
    /// </summary>
    [Fact]
    public async Task GenerateToken_PublishesTokenGeneratedEvent()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new TokenGenerationCommand(TestUserEmail, TestPassword);
        
        SetupUserManagerForSuccess(user, TestPassword);

        // Setup event publishing verification
        TokenGeneratedEvent? publishedEvent = null;
        PublisherMock.Setup(x => x.Publish(It.IsAny<TokenGeneratedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<IEvent, CancellationToken>((e, _) => publishedEvent = e as TokenGeneratedEvent)
            .Returns(Task.CompletedTask);

        // Act
        var result = await TokenService.GenerateTokenAsync(request, TestIpAddress, CancellationToken.None);

        // Assert
        PublisherMock.Verify(
            x => x.Publish(It.IsAny<TokenGeneratedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
            
        Assert.NotNull(publishedEvent);
        Assert.NotNull(publishedEvent!.UserId);
        Assert.Equal(user.Id, publishedEvent.UserId);
        Assert.Equal(result.Token, publishedEvent.Token);
        Assert.Equal(result.RefreshToken, publishedEvent.RefreshToken);
        Assert.Equal(result.RefreshTokenExpiryTime, publishedEvent.RefreshTokenExpiryTime);
        Assert.Equal(nameof(TokenGeneratedEvent), publishedEvent.EventType);
        // OccurredOn is a value type (DateTime), so no need to check for null
    }

    /// <summary>
    /// Tests that RefreshTokenAsync publishes a TokenGeneratedEvent with the correct data.
    /// </summary>
    [Fact]
    public async Task RefreshToken_PublishesTokenGeneratedEvent()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new RefreshTokenCommand(TestToken, TestRefreshToken);
        var ipAddress = TestIpAddress;
        
        UserManagerMock.Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
            
        UserManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Setup event publishing verification
        TokenGeneratedEvent? publishedEvent = null;
        PublisherMock.Setup(x => x.Publish(It.IsAny<TokenGeneratedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<IEvent, CancellationToken>((e, _) => publishedEvent = (TokenGeneratedEvent)e)
            .Returns(Task.CompletedTask);

        // Act
        var result = await TokenService.RefreshTokenAsync(request, ipAddress, CancellationToken.None);

        // Assert
        PublisherMock.Verify(
            x => x.Publish(It.IsAny<TokenGeneratedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
            
        Assert.NotNull(publishedEvent);
        Assert.NotNull(publishedEvent!.UserId);
        Assert.Equal(user.Id, publishedEvent.UserId);
        Assert.Equal(result.Token, publishedEvent.Token);
        Assert.Equal(result.RefreshToken, publishedEvent.RefreshToken);
        Assert.Equal(result.RefreshTokenExpiryTime, publishedEvent.RefreshTokenExpiryTime);
        Assert.Equal(nameof(TokenGeneratedEvent), publishedEvent.EventType);
        // OccurredOn is a value type (DateTime), so no need to check for null
    }

    /// <summary>
    /// Tests that GenerateTokenAsync still publishes a TokenGeneratedEvent even if the user update fails.
    /// </summary>
    [Fact]
    public async Task GenerateToken_WithFailedUserUpdate_StillPublishesEvent()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new TokenGenerationCommand(TestUserEmail, TestPassword);
        var ipAddress = TestIpAddress;
        
        // Setup UserManager to fail on UpdateAsync
        UserManagerMock.Setup(x => x.FindByEmailAsync(TestUserEmail))
            .ReturnsAsync(user);
            
        UserManagerMock.Setup(x => x.CheckPasswordAsync(user, TestPassword))
            .ReturnsAsync(true);
            
        UserManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });
            
        UserManagerMock.Setup(x => x.GetClaimsAsync(user))
            .ReturnsAsync(new List<System.Security.Claims.Claim>());
            
        UserManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

        // Setup event publishing verification
        bool eventPublished = false;
        PublisherMock.Setup(x => x.Publish(It.IsAny<TokenGeneratedEvent>(), It.IsAny<CancellationToken>()))
            .Callback(() => eventPublished = true)
            .Returns(Task.CompletedTask);

        // Act
        var result = await TokenService.GenerateTokenAsync(request, ipAddress, CancellationToken.None);

        // Assert
        Assert.True(eventPublished, "TokenGeneratedEvent should still be published even if user update fails");
        Assert.NotNull(result);
        Assert.NotNull(result.Token);
    }

    /// <summary>
    /// Tests that GenerateTokenAsync still returns a valid token even if event publishing fails.
    /// </summary>
    [Fact]
    public async Task GenerateToken_WithEventPublishingFailure_StillReturnsToken()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new TokenGenerationCommand(TestUserEmail, TestPassword);
        var ipAddress = TestIpAddress;
        
        SetupUserManagerForSuccess(user, TestPassword);

        // Setup event publishing to fail
        PublisherMock.Setup(x => x.Publish(It.IsAny<TokenGeneratedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Event publishing failed"));

        // Act & Assert - Should not throw
        var exception = await Record.ExceptionAsync(async () => 
            await TokenService.GenerateTokenAsync(request, ipAddress, CancellationToken.None));
            
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that RefreshTokenAsync still returns a valid token even if event publishing fails.
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithEventPublishingFailure_StillReturnsToken()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new RefreshTokenCommand(TestToken, TestRefreshToken);
        var ipAddress = TestIpAddress;
        
        UserManagerMock.Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
            
        UserManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Setup event publishing to fail
        PublisherMock.Setup(x => x.Publish(It.IsAny<TokenGeneratedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Event publishing failed"));

        // Act & Assert - Should not throw
        var exception = await Record.ExceptionAsync(async () => 
            await TokenService.RefreshTokenAsync(request, ipAddress, CancellationToken.None));
            
        Assert.Null(exception);
    }
}
