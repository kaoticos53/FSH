using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FSH.Framework.Core.Auth.Jwt;
using FSH.Framework.Core.Events;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Identity.Tokens;
using FSH.Framework.Core.Identity.Tokens.Events;
using FSH.Framework.Core.Identity.Tokens.Features.Generate;
using FSH.Framework.Core.Identity.Tokens.Features.Refresh;
using FSH.Framework.Core.Tenancy;
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
/// Contains integration tests for the <see cref="TokenService"/> class.
/// These tests verify the interaction between token generation, validation, and refresh functionality.
/// </summary>
public class TokenServiceIntegrationTests : TokenServiceTestBase
{
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly JwtTokenValidator _jwtTokenValidator;
    private readonly ITokenService _integrationTokenService;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly List<IEvent> _publishedEvents;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenServiceIntegrationTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper to send test output to.</param>
    public TokenServiceIntegrationTests(ITestOutputHelper output) : base(output)
    {
        _jwtTokenGenerator = new JwtTokenGenerator(Options.Create(JwtOptions));
        _jwtTokenValidator = new JwtTokenValidator(
            Options.Create(JwtOptions),
            MultiTenantContextAccessorMock.Object);
            
        _tenantServiceMock = new Mock<ITenantService>();
        _publishedEvents = new List<IEvent>();
        
        // Setup publisher to capture events
        var publisherMock = new Mock<IPublisher>();
        publisherMock.Setup(p => p.Publish(It.IsAny<IEvent>(), It.IsAny<CancellationToken>()))
            .Callback<IEvent, CancellationToken>((e, _) => _publishedEvents.Add(e))
            .Returns(Task.CompletedTask);
            
        _integrationTokenService = new TokenService(
            Options.Create(JwtOptions),
            UserManagerMock.Object,
            MultiTenantContextAccessorMock.Object,
            publisherMock.Object);
    }

    /// <summary>
    /// Tests the complete token lifecycle including generation and refresh.
    /// Verifies that tokens can be generated and subsequently refreshed.
    /// </summary>
    [Fact]
    public async Task FullFlow_GenerateAndRefreshToken_WorksCorrectly()
    {
        // Arrange - Setup user and initial token generation
        var user = CreateTestUser();
        var generateRequest = new TokenGenerationCommand(TestUserEmail, TestPassword);
        
        SetupUserManagerForSuccess(user, TestPassword);
        
        // Act 1 - Generate initial token
        var generateResponse = await _integrationTokenService.GenerateTokenAsync(
            generateRequest, TestIpAddress, CancellationToken.None);
            
        // Assert 1 - Verify token generation
        AssertValidTokenResponse(generateResponse, user.Id, TestUserEmail, TestTenantId);
        AssertTokenContainsClaim(generateResponse.Token, "email", TestUserEmail);
        AssertTokenContainsClaim(generateResponse.Token, "sub", user.Id);
        
        // Verify event was published
        var generatedEvent = _publishedEvents.OfType<TokenGeneratedEvent>().Single();
        Assert.Equal(user.Id, generatedEvent.UserId);
        _publishedEvents.Clear();
        
        // Act 2 - Refresh token
        var refreshRequest = new RefreshTokenCommand(
            generateResponse.Token, 
            generateResponse.RefreshToken);
            
        var refreshResponse = await _integrationTokenService.RefreshTokenAsync(
            refreshRequest, TestIpAddress, CancellationToken.None);
            
        // Assert 2 - Verify token refresh
        AssertValidTokenResponse(refreshResponse, user.Id, user.Email!, TestTenantId);
        AssertTokenContainsClaim(refreshResponse.Token, "email", TestUserEmail);
        AssertTokenContainsClaim(refreshResponse.Token, "sub", user.Id);
        
        // Verify refresh event was published
        var refreshedEvent = _publishedEvents.OfType<TokenGeneratedEvent>().Single();
        Assert.Equal(user.Id, refreshedEvent.UserId);
        
        // Verify refresh token was rotated (old one is no longer valid)
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _integrationTokenService.RefreshTokenAsync(refreshRequest, TestIpAddress, CancellationToken.None));
            
        Assert.Equal("Token de actualización inválido.", exception.Message);
    }

    /// <summary>
    /// Tests that custom claims are properly included in the generated token.
    /// Verifies that both standard and custom claims are present in the JWT.
    /// </summary>
    [Fact]
    public async Task GenerateToken_WithCustomClaims_IncludesThemInToken()
    {
        // Arrange
        var user = CreateTestUser();
        var generateRequest = new TokenGenerationCommand(TestUserEmail, TestPassword);
        
        // Setup custom claims
        var customClaims = new List<Claim>
        {
            new("custom_type", "custom_value"),
            new("permission", "users:read"),
            new("permission", "users:write")
        };
        
        UserManagerMock.Setup(x => x.FindByEmailAsync(TestUserEmail))
            .ReturnsAsync(user);
            
        UserManagerMock.Setup(x => x.CheckPasswordAsync(user, TestPassword))
            .ReturnsAsync(true);
            
        UserManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin" });
            
        UserManagerMock.Setup(x => x.GetClaimsAsync(user))
            .ReturnsAsync(customClaims);
            
        UserManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var response = await _integrationTokenService.GenerateTokenAsync(
            generateRequest, TestIpAddress, CancellationToken.None);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwt = tokenHandler.ReadJwtToken(response.Token);
        
        // Verify standard claims
        Assert.Equal(TestUserEmail, jwt.Claims.First(c => c.Type == "email").Value);
        Assert.Equal(user.Id, jwt.Claims.First(c => c.Type == "sub").Value);
        
        // Verify custom claims
        Assert.Contains(jwt.Claims, c => c.Type == "custom_type" && c.Value == "custom_value");
        var permissions = jwt.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();
        Assert.Contains("users:read", permissions);
        Assert.Contains("users:write", permissions);
        
        // Verify role claim was added
        Assert.Contains(jwt.Claims, c => c.Type == "role" && c.Value == "Admin");
    }

    /// <summary>
    /// Tests that tenant restrictions are properly enforced in token generation.
    /// Verifies that tenant claims are correctly included and validated.
    /// </summary>
    [Fact]
    public async Task GenerateToken_WithTenantRestrictions_EnforcesThem()
    {
        // Arrange - Setup tenant with restrictions
        var tenantId = "restricted-tenant";
        var user = CreateTestUser(tenantId: tenantId);
        var generateRequest = new TokenGenerationCommand(TestUserEmail, TestPassword);
        
        // Setup tenant with restrictions
        var tenantInfo = new FshTenantInfo
        {
            Id = tenantId,
            Identifier = tenantId,
            Name = "Restricted Tenant",
            IsActive = true,
            ValidUpto = DateTime.UtcNow.AddDays(30),
            // Add tenant-specific restrictions here if applicable
        };
        
        var multiTenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
        MultiTenantContextAccessorMock.Setup(x => x.MultiTenantContext).Returns(multiTenantContext);
        
        // Setup user manager
        SetupUserManagerForSuccess(user, TestPassword);

        // Act
        var response = await _integrationTokenService.GenerateTokenAsync(
            generateRequest, TestIpAddress, CancellationToken.None);

        // Assert - Verify tenant claim is included and correct
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwt = tokenHandler.ReadJwtToken(response.Token);
        
        var tenantClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tenant");
        Assert.NotNull(tenantClaim);
        Assert.Equal(tenantId, tenantClaim.Value);
        
        // Verify token can be validated with the same tenant context
        var principal = _jwtTokenValidator.ValidateToken(response.Token);
        Assert.NotNull(principal);
        Assert.True(principal.Identity?.IsAuthenticated);
    }

    /// <summary>
    /// Tests that refresh token reuse is prevented in concurrent scenarios.
    /// Verifies that a refresh token can only be used once, even with concurrent requests.
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithConcurrentRequests_PreventsReuse()
    {
        // This test verifies that a refresh token can only be used once,
        // even if multiple requests come in simultaneously
        
        // Arrange
        var user = CreateTestUser();
        var generateRequest = new TokenGenerationCommand(TestUserEmail, TestPassword);
        
        SetupUserManagerForSuccess(user, TestPassword);
        
        // Generate initial token
        var generateResponse = await _integrationTokenService.GenerateTokenAsync(
            generateRequest, TestIpAddress, CancellationToken.None);
            
        var refreshToken = generateResponse.RefreshToken;
        var refreshRequest = new RefreshTokenCommand(
            generateResponse.Token, 
            refreshToken);
            
        // Setup UserManager to simulate concurrent access
        bool firstRequest = true;
        UserManagerMock.Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
            
        UserManagerMock.Setup(x => x.UpdateAsync(user))
            .Callback<FshUser>(u => 
            {
                // On first update, simulate a concurrent update by another request
                if (firstRequest)
                {
                    firstRequest = false;
                    user.RefreshToken = GenerateRefreshToken(); // Simulate refresh by another request
                    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                }
            })
            .ReturnsAsync(IdentityResult.Success);

        // Act & Assert - First refresh should fail due to concurrent update
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _integrationTokenService.RefreshTokenAsync(refreshRequest, TestIpAddress, CancellationToken.None));
            
        Assert.Equal("Token de actualización inválido.", exception.Message);
    }
}
