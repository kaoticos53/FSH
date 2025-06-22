using System;
using System.Threading.Tasks;
using FSH.Framework.Core.Identity.Tokens.Features.Generate;
using FSH.Framework.Core.Identity.Tokens.Features.Refresh;
using FSH.Framework.Infrastructure.Identity.Users;
using FSH.Framework.Infrastructure.Tenant;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace FSH.Framework.Core.Tests.Auth.Jwt;

/// <summary>
/// Provides test scenarios and setup methods for TokenService tests.
/// </summary>
public static class TokenServiceTestScenarios
{
    /// <summary>
    /// Sets up a valid token generation scenario with the specified parameters.
    /// </summary>
    /// <param name="userManagerMock">The UserManager mock to set up.</param>
    /// <param name="email">The email address for the test user. Defaults to "test@test.com".</param>
    /// <param name="password">The password for the test user. Defaults to "ValidPass123!".</param>
    /// <param name="isActive">Whether the user is active. Defaults to true.</param>
    /// <param name="emailConfirmed">Whether the user's email is confirmed. Defaults to true.</param>
    /// <param name="tenantId">The tenant ID for the test. Defaults to "test-tenant".</param>
    /// <returns>A tuple containing the token generation request, test user, and tenant info.</returns>
    public static (TokenGenerationCommand, FshUser, FshTenantInfo) SetupValidTokenGeneration(
        Mock<UserManager<FshUser>> userManagerMock,
        string email = "test@test.com",
        string password = "ValidPass123!",
        bool isActive = true,
        bool emailConfirmed = true,
        string tenantId = "test-tenant")
    {
        var request = new TokenGenerationCommand(email, password);
        var user = new FshUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            IsActive = isActive,
            EmailConfirmed = emailConfirmed,
            TenantId = tenantId
        };

        var tenantInfo = new FshTenantInfo
        {
            Id = tenantId,
            Identifier = tenantId,
            Name = "Test Tenant",
            IsActive = true,
            ValidUpto = DateTime.UtcNow.AddDays(30)
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);
        userManagerMock.Setup(x => x.CheckPasswordAsync(user, password))
            .ReturnsAsync(true);

        return (request, user, tenantInfo);
    }

    /// <summary>
    /// Sets up a valid token refresh scenario with the specified parameters.
    /// </summary>
    /// <param name="userManagerMock">The UserManager mock to set up.</param>
    /// <param name="token">The JWT token to refresh.</param>
    /// <param name="refreshToken">The refresh token to use.</param>
    /// <param name="userId">The ID of the user for whom to refresh the token.</param>
    /// <param name="isActive">Whether the user is active. Defaults to true.</param>
    /// <returns>A tuple containing the refresh token request and the test user.</returns>
    public static (RefreshTokenCommand, FshUser) SetupValidTokenRefresh(
        Mock<UserManager<FshUser>> userManagerMock,
        string token,
        string refreshToken,
        string userId,
        bool isActive = true)
    {
        var request = new RefreshTokenCommand(token, refreshToken);
        var user = new FshUser
        {
            Id = userId,
            Email = "test@test.com",
            UserName = "testuser",
            IsActive = isActive,
            RefreshToken = refreshToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };

        userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        return (request, user);
    }
}
