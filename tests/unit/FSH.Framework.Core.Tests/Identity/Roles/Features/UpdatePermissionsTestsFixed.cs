using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant;
using FSH.Framework.Core.Audit;
using FSH.Framework.Core.Identity.Roles.Features.UpdatePermissions;
using FSH.Framework.Core.Identity.Users.Abstractions;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Identity.Persistence;
using FSH.Framework.Infrastructure.Identity.RoleClaims;
using FSH.Framework.Infrastructure.Identity.Roles;
using FSH.Framework.Infrastructure.Tenant;
using FSH.Starter.Shared.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FSH.Framework.Core.Tests.Shared;

namespace FSH.Framework.Core.Tests.Identity.Roles.Features
{
    public class UpdatePermissionsTestsFixed
    {
        [Fact]
        public async Task UpdatePermissions_ShouldRemoveUnselectedPermissions_And_AddNewlySelectedPermissions()
        {
            // Arrange
            var roleStoreMock = new Mock<IRoleStore<FshRole>>();
            var roleManagerMock = new Mock<RoleManager<FshRole>>(
                roleStoreMock.Object,
                null!,
                null!,
                null!,
                null!);

            var role = new FshRole("TestRole", "Test Role") { Id = Guid.NewGuid().ToString() };
            var command = new UpdatePermissionsCommand
            {
                RoleId = role.Id,
                Permissions = new List<string> { "Permissions.Users.View", "Permissions.Users.Create" }
            };
            var existingClaims = new List<Claim>
            {
                new Claim(FshClaims.Permission, "Permissions.Roles.View"),
                new Claim(FshClaims.Permission, "Permissions.Users.View")
            };

            roleManagerMock.Setup(x => x.FindByIdAsync(command.RoleId)).ReturnsAsync(role);
            roleManagerMock.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(existingClaims);
            roleManagerMock.Setup(x => x.RemoveClaimAsync(role, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);

            var tenantInfo = new FshTenantInfo { Id = TenantConstants.Root.Id, Identifier = "root", Name = "Root" };
            var tenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
            var multiTenantAccessor = new TestMultiTenantAccessor { MultiTenantContext = tenantContext };

            var dbOptions = Options.Create(new DatabaseOptions { Provider = "InMemory", ConnectionString = "DataSource=:memory:" });
            var efOptions = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase($"IdentityDb_{Guid.NewGuid()}")
                .Options;
            var dbContext = new IdentityDbContext(multiTenantAccessor, efOptions, dbOptions)
            {
                // Propiedad requerida por el contexto, no usada en este test
                AuditTrails = null!
            };
            // Asignar el DbSet real gestionado por EF para evitar problemas de mapeo
            dbContext.AuditTrails = dbContext.Set<AuditTrail>();
            dbContext.Database.EnsureCreated();

            var currentUserMock = new Mock<ICurrentUser>();

            var roleService = new RoleService(
                roleManagerMock.Object,
                dbContext,
                multiTenantAccessor,
                currentUserMock.Object);

            // Act
            var result = await roleService.UpdatePermissionsAsync(command);

            // Assert
            roleManagerMock.Verify(x => x.FindByIdAsync(command.RoleId), Times.Once);
            roleManagerMock.Verify(x => x.GetClaimsAsync(role), Times.Once);
            roleManagerMock.Verify(x => x.RemoveClaimAsync(role, It.Is<Claim>(c => c.Value == "Permissions.Roles.View")), Times.Once);
            roleManagerMock.Verify(x => x.RemoveClaimAsync(role, It.Is<Claim>(c => c.Value == "Permissions.Users.View")), Times.Never);

            var savedClaims = await dbContext.RoleClaims.ToListAsync();
            savedClaims.Should().HaveCount(1);
            savedClaims.Should().Contain(c => c.ClaimValue == "Permissions.Users.Create");
            savedClaims.Should().Contain(c => c.ClaimType == FshClaims.Permission);
            savedClaims.Should().Contain(c => c.RoleId == role.Id);

            result.Should().Be("permissions updated");
        }

    }
}
