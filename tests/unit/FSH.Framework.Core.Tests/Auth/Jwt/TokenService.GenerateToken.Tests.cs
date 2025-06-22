using System;
using System.Threading.Tasks;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Identity.Tokens.Features.Generate;
using FSH.Framework.Infrastructure.Identity.Users;
using FSH.Framework.Infrastructure.Tenant;
using MediatR;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace FSH.Framework.Core.Tests.Auth.Jwt;

/// <summary>
/// Contains unit tests for the token generation functionality of TokenService.
/// </summary>
public class TokenServiceGenerateTokenTests : TokenServiceTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenServiceGenerateTokenTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper for logging test output.</param>
    public TokenServiceGenerateTokenTests(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// Tests that GenerateTokenAsync returns a valid token response when provided with valid credentials.
    /// </summary>
    [Fact]
    public async Task GenerateToken_WithValidCredentials_ReturnsTokenResponse()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new TokenGenerationCommand(TestUserEmail, TestPassword);
        
        SetupUserManagerForSuccess(user, TestPassword);

        // Act
        var result = await TokenService.GenerateTokenAsync(request, TestIpAddress, CancellationToken.None);

        // Assert
        AssertValidTokenResponse(result, user.Id, TestUserEmail, TestTenantId);
        AssertTokenContainsClaim(result.Token, "email", TestUserEmail);
        AssertTokenContainsClaim(result.Token, "sub", user.Id);
        AssertTokenContainsClaim(result.Token, "tenant", TestTenantId);
        
        VerifyTokenGeneratedEventPublished();
    }

    /// <summary>
    /// Tests that GenerateTokenAsync throws an UnauthorizedException when the user does not exist.
    /// </summary>
    [Fact]
    public async Task GenerateToken_WithNonexistentUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new TokenGenerationCommand("nonexistent@test.com", TestPassword);
        
        UserManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((FshUser)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => TokenService.GenerateTokenAsync(request, TestIpAddress, CancellationToken.None));
            
        Assert.Equal("Usuario o contraseña incorrectos.", exception.Message);
        VerifyNoEventsPublished();
    }

    /// <summary>
    /// Tests that GenerateTokenAsync throws an UnauthorizedException when the password is incorrect.
    /// </summary>
    [Fact]
    public async Task GenerateToken_WithIncorrectPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new TokenGenerationCommand(TestUserEmail, "WrongPassword!");
        
        UserManagerMock.Setup(x => x.FindByEmailAsync(TestUserEmail))
            .ReturnsAsync(user);
            
        UserManagerMock.Setup(x => x.CheckPasswordAsync(user, "WrongPassword!"))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => TokenService.GenerateTokenAsync(request, TestIpAddress, CancellationToken.None));
            
        Assert.Equal("Usuario o contraseña incorrectos.", exception.Message);
        VerifyNoEventsPublished();
    }

    /// <summary>
    /// Tests that GenerateTokenAsync throws an UnauthorizedException when the user account is inactive.
    /// </summary>
    [Fact]
    public async Task GenerateToken_WithInactiveUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser(isActive: false);
        var request = new TokenGenerationCommand(TestUserEmail, TestPassword);
        
        SetupUserManagerForSuccess(user, TestPassword);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => TokenService.GenerateTokenAsync(request, TestIpAddress, CancellationToken.None));
            
        Assert.Equal("User is not active. Please contact the administrator.", exception.Message);
        VerifyNoEventsPublished();
    }

    /// <summary>
    /// Tests that GenerateTokenAsync throws an UnauthorizedException when the user's email is not confirmed.
    /// </summary>
    [Fact]
    public async Task GenerateToken_WithUnconfirmedEmail_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser(emailConfirmed: false);
        var request = new TokenGenerationCommand(TestUserEmail, TestPassword);
        
        SetupUserManagerForSuccess(user, TestPassword);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => TokenService.GenerateTokenAsync(request, TestIpAddress, CancellationToken.None));
            
        Assert.Equal("Email not confirmed. Please confirm your email before logging in.", exception.Message);
        VerifyNoEventsPublished();
    }

    /// <summary>
    /// Tests that GenerateTokenAsync throws an UnauthorizedException when the tenant is inactive.
    /// </summary>
    [Fact]
    public async Task GenerateToken_WithInactiveTenant_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new TokenGenerationCommand(TestUserEmail, TestPassword);
        
        // Set up an inactive tenant
        var tenantInfo = new FshTenantInfo
        {
            Id = TestTenantId,
            Identifier = TestTenantId,
            Name = "Inactive Tenant",
            IsActive = false,
            ValidUpto = DateTime.UtcNow.AddYears(1)
        };
        var multiTenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
        MultiTenantContextAccessorMock.Setup(x => x.MultiTenantContext).Returns(multiTenantContext);
        
        SetupUserManagerForSuccess(user, TestPassword);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => TokenService.GenerateTokenAsync(request, TestIpAddress, CancellationToken.None));
            
        Assert.Equal("Tenant is not active. Please contact the administrator.", exception.Message);
        VerifyNoEventsPublished();
    }

    /// <summary>
    /// Tests that GenerateTokenAsync throws an UnauthorizedException when the tenant's subscription has expired.
    /// </summary>
    [Fact]
    public async Task GenerateToken_WithExpiredTenant_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new TokenGenerationCommand(TestUserEmail, TestPassword);
        
        // Set up an expired tenant
        var tenantInfo = new FshTenantInfo
        {
            Id = TestTenantId,
            Identifier = TestTenantId,
            Name = "Expired Tenant",
            IsActive = true,
            ValidUpto = DateTime.UtcNow.AddDays(-1) // Expired yesterday
        };
        var multiTenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
        MultiTenantContextAccessorMock.Setup(x => x.MultiTenantContext).Returns(multiTenantContext);
        
        SetupUserManagerForSuccess(user, TestPassword);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => TokenService.GenerateTokenAsync(request, TestIpAddress, CancellationToken.None));
            
        Assert.Equal("Tenant subscription has expired. Please renew your subscription.", exception.Message);
        VerifyNoEventsPublished();
    }
}
