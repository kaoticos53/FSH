using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant;
using FSH.Framework.Core.Exceptions;
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
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FSH.Framework.Core.Tests.Shared;

namespace FSH.Framework.Core.Tests.Identity.Roles.Features
{
    /// <summary>
    /// Pruebas unitarias para el feature UpdatePermissions.
    /// </summary>
    public class UpdatePermissionsTests : IClassFixture<TestFixture>
    {
        private readonly Mock<RoleManager<FshRole>> _roleManagerMock;
        private readonly TestFixture _fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatePermissionsTests"/> class.
        /// </summary>
        /// <param name="fixture">The test fixture.</param>
        public UpdatePermissionsTests(TestFixture fixture)
        {
            _fixture = fixture;
            _roleManagerMock = _fixture.RoleManagerMock;
        }

        /// <summary>
        /// Prueba que se lanza una excepción de tipo NotFoundException cuando el rol no existe.
        /// </summary>
        [Fact]
        public async Task UpdatePermissions_ShouldThrowNotFoundException_WhenRoleDoesNotExist()
        {
            // Arrange
            var command = new UpdatePermissionsCommand { RoleId = Guid.NewGuid().ToString(), Permissions = new List<string>() };
            
            _roleManagerMock.Setup(x => x.FindByIdAsync(command.RoleId)).ReturnsAsync((FshRole?)null);

            var roleService = CreateRoleService();

            // Act
            Func<Task> act = async () => await roleService.UpdatePermissionsAsync(command);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("role not found");
        }

        /// <summary>
        /// Prueba que se lanza una excepción cuando se intentan actualizar los permisos del rol de administrador.
        /// </summary>
        [Fact]
        public async Task UpdatePermissions_ShouldThrowException_WhenRoleIsAdmin()
        {
            // Arrange
            var adminRole = new FshRole(FshRoles.Admin, "Admin Role") { Id = Guid.NewGuid().ToString() };
            var command = new UpdatePermissionsCommand { RoleId = adminRole.Id, Permissions = new List<string>() };
            
            _roleManagerMock.Setup(x => x.FindByIdAsync(command.RoleId)).ReturnsAsync(adminRole);

            var roleService = CreateRoleService();

            // Act
            Func<Task> act = async () => await roleService.UpdatePermissionsAsync(command);

            // Assert
            await act.Should().ThrowAsync<FshException>().WithMessage("operation not permitted");
        }

        /// <summary>
        /// Prueba que los permisos no seleccionados se eliminan y los nuevos permisos seleccionados se añaden correctamente.
        /// </summary>
        [Fact]
        public async Task UpdatePermissions_ShouldRemoveUnselectedPermissions_And_AddNewlySelectedPermissions()
        {
            // Arrange
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

            _roleManagerMock.Setup(x => x.FindByIdAsync(command.RoleId)).ReturnsAsync(role);
            _roleManagerMock.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(existingClaims);
            _roleManagerMock.Setup(x => x.RemoveClaimAsync(role, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);

            var (roleService, dbContext) = CreateRoleServiceWithRealDbContext();

            // Act
            var result = await roleService.UpdatePermissionsAsync(command);

            // Assert
            _roleManagerMock.Verify(x => x.FindByIdAsync(command.RoleId), Times.Once);
            _roleManagerMock.Verify(x => x.GetClaimsAsync(role), Times.Once);
            _roleManagerMock.Verify(x => x.RemoveClaimAsync(role, It.Is<Claim>(c => c.Value == "Permissions.Roles.View")), Times.Once);
            _roleManagerMock.Verify(x => x.RemoveClaimAsync(role, It.Is<Claim>(c => c.Value == "Permissions.Users.View")), Times.Never);

            var savedClaims = await dbContext.RoleClaims.ToListAsync();
            savedClaims.Should().HaveCount(1);
            savedClaims.Should().Contain(c => c.ClaimValue == "Permissions.Users.Create");
            savedClaims.Should().Contain(c => c.ClaimType == FshClaims.Permission);
            savedClaims.Should().Contain(c => c.RoleId == role.Id);

            result.Should().Be("permissions updated");
        }

        /// <summary>
        /// Verifica que GetWithPermissionsAsync devuelve los permisos almacenados en el contexto.
        /// </summary>
        [Fact]
        public async Task GetWithPermissions_ShouldReturnPermissions_FromDbContext()
        {
            // Arrange
            var role = new FshRole("TestRole", "Test Role") { Id = Guid.NewGuid().ToString() };
            _roleManagerMock.Setup(x => x.FindByIdAsync(role.Id)).ReturnsAsync(role);

            var tenantInfo = new FshTenantInfo { Id = TenantConstants.Root.Id, Identifier = "root", Name = "Root" };
            var tenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
            var multiTenantAccessor = new TestMultiTenantAccessor { MultiTenantContext = tenantContext };

            var dbOptions = Options.Create(new DatabaseOptions { Provider = "InMemory", ConnectionString = "DataSource=:memory:" });
            var efOptions = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase($"IdentityDb_{Guid.NewGuid()}")
                .Options;
            var dbContext = new IdentityDbContext(multiTenantAccessor, efOptions, dbOptions)
            {
                AuditTrails = null!
            };
            dbContext.AuditTrails = dbContext.Set<AuditTrail>();
            dbContext.Database.EnsureCreated();

            // Seed claims
            dbContext.RoleClaims.Add(new FshRoleClaim
            {
                RoleId = role.Id,
                ClaimType = FshClaims.Permission,
                ClaimValue = "Permissions.Users.View",
                CreatedBy = Guid.NewGuid().ToString()
            });
            dbContext.RoleClaims.Add(new FshRoleClaim
            {
                RoleId = role.Id,
                ClaimType = FshClaims.Permission,
                ClaimValue = "Permissions.Users.Create",
                CreatedBy = Guid.NewGuid().ToString()
            });
            await dbContext.SaveChangesAsync();

            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.GetUserId()).Returns(Guid.NewGuid());

            var service = new RoleService(
                _roleManagerMock.Object,
                dbContext,
                multiTenantAccessor,
                currentUserMock.Object);

            // Act
            var dto = await service.GetWithPermissionsAsync(role.Id, CancellationToken.None);

            // Assert
            dto.Should().NotBeNull();
            dto!.Permissions.Should().BeEquivalentTo(new[] { "Permissions.Users.View", "Permissions.Users.Create" });
        }

        /// <summary>
        /// Verifica que GetWithPermissionsAsync lanza NotFound cuando el rol no existe.
        /// </summary>
        [Fact]
        public async Task GetWithPermissions_ShouldThrowNotFound_WhenRoleDoesNotExist()
        {
            // Arrange
            var roleId = Guid.NewGuid().ToString();
            _roleManagerMock.Setup(x => x.FindByIdAsync(roleId)).ReturnsAsync((FshRole?)null);

            var (service, _) = CreateRoleServiceWithTenant(TenantConstants.Root.Id);

            // Act
            var act = async () => await service.GetWithPermissionsAsync(roleId, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("role not found");
        }

        

        /// <summary>
        /// Prueba que la operación falla si la eliminación de un claim no tiene éxito.
        /// </summary>
        [Fact]
        public async Task UpdatePermissions_ShouldFail_WhenRemoveClaimFails()
        {
            // Arrange
            var role = new FshRole("TestRole", "Test Role") { Id = Guid.NewGuid().ToString() };
            var command = new UpdatePermissionsCommand
            {
                RoleId = role.Id,
                Permissions = new List<string> { "Permissions.Users.View" }
            };
            var existingClaims = new List<Claim>
            {
                new Claim(FshClaims.Permission, "Permissions.Roles.View")
            };

            _roleManagerMock.Setup(x => x.FindByIdAsync(command.RoleId)).ReturnsAsync(role);
            _roleManagerMock.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(existingClaims);
            _roleManagerMock.Setup(x => x.RemoveClaimAsync(role, It.IsAny<Claim>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Failed to remove claim" }));

            var roleService = CreateRoleService();

            // Act
            Func<Task> act = async () => await roleService.UpdatePermissionsAsync(command);

            // Assert
            await act.Should().ThrowAsync<FshException>().WithMessage("operation failed");
        }

        private RoleService CreateRoleService()
        {
            var tenantInfo = new FshTenantInfo { Id = TenantConstants.Root.Id, Identifier = "root", Name = "Root" };
            var tenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
            var multiTenantAccessor = new TestMultiTenantAccessor { MultiTenantContext = tenantContext };

            var dbOptions = Options.Create(new DatabaseOptions { Provider = "InMemory", ConnectionString = "DataSource=:memory:" });
            var efOptions = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase($"IdentityDb_{Guid.NewGuid()}")
                .Options;
            var dbContext = new IdentityDbContext(multiTenantAccessor, efOptions, dbOptions)
            {
                // Propiedad requerida por el contexto, no utilizada en estos tests
                AuditTrails = null!
            };
            // Asignar el DbSet real gestionado por EF para evitar problemas de mapeo
            dbContext.AuditTrails = dbContext.Set<AuditTrail>();
            dbContext.Database.EnsureCreated();

            var currentUserMock = new Mock<ICurrentUser>();

            return new RoleService(
                _roleManagerMock.Object,
                dbContext,
                multiTenantAccessor,
                currentUserMock.Object);
        }

        private (RoleService service, IdentityDbContext dbContext) CreateRoleServiceWithRealDbContext()
        {
            var tenantInfo = new FshTenantInfo { Id = TenantConstants.Root.Id, Identifier = "root", Name = "Root" };
            var tenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
            var multiTenantAccessor = new TestMultiTenantAccessor { MultiTenantContext = tenantContext };

            var dbOptions = Options.Create(new DatabaseOptions { Provider = "InMemory", ConnectionString = "DataSource=:memory:" });
            var efOptions = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase($"IdentityDb_{Guid.NewGuid()}")
                .Options;
            var dbContext = new IdentityDbContext(multiTenantAccessor, efOptions, dbOptions)
            {
                // Propiedad requerida por el contexto, no utilizada en estos tests
                AuditTrails = null!
            };
            // Asignar el DbSet real gestionado por EF para evitar problemas de mapeo
            dbContext.AuditTrails = dbContext.Set<AuditTrail>();
            dbContext.Database.EnsureCreated();

            var currentUserMock = new Mock<ICurrentUser>();

            var roleService = new RoleService(
                _roleManagerMock.Object,
                dbContext,
                multiTenantAccessor,
                currentUserMock.Object);

            return (roleService, dbContext);
        }

        /// <summary>
        /// Crea un RoleService y un DbContext para un tenant específico.
        /// </summary>
        private (RoleService service, IdentityDbContext dbContext) CreateRoleServiceWithTenant(string tenantId)
        {
            var tenantInfo = new FshTenantInfo { Id = tenantId, Identifier = tenantId, Name = tenantId };
            var tenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
            var multiTenantAccessor = new TestMultiTenantAccessor { MultiTenantContext = tenantContext };

            var dbOptions = Options.Create(new DatabaseOptions { Provider = "InMemory", ConnectionString = "DataSource=:memory:" });
            var efOptions = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase($"IdentityDb_{Guid.NewGuid()}")
                .Options;
            var dbContext = new IdentityDbContext(multiTenantAccessor, efOptions, dbOptions)
            {
                AuditTrails = null!
            };
            dbContext.AuditTrails = dbContext.Set<AuditTrail>();
            dbContext.Database.EnsureCreated();

            var currentUserMock = new Mock<ICurrentUser>();

            var roleService = new RoleService(
                _roleManagerMock.Object,
                dbContext,
                multiTenantAccessor,
                currentUserMock.Object);

            return (roleService, dbContext);
        }

        /// <summary>
        /// Verifica que los permisos de Root se filtran para tenants no Root.
        /// </summary>
        [Fact]
        public async Task UpdatePermissions_ShouldNotAddRootPermissions_ForNonRootTenant()
        {
            // Arrange
            var role = new FshRole("TestRole", "Test Role") { Id = Guid.NewGuid().ToString() };
            var command = new UpdatePermissionsCommand
            {
                RoleId = role.Id,
                Permissions = new List<string> { "Permissions.Root.System", "Permissions.Users.View" }
            };

            _roleManagerMock.Setup(x => x.FindByIdAsync(command.RoleId)).ReturnsAsync(role);
            _roleManagerMock.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());

            var (service, db) = CreateRoleServiceWithTenant("tenant-xyz"); // No Root

            // Act
            var result = await service.UpdatePermissionsAsync(command);

            // Assert
            var saved = await db.RoleClaims.ToListAsync();
            saved.Should().HaveCount(1);
            saved.Should().OnlyContain(c => c.ClaimValue == "Permissions.Users.View");
            result.Should().Be("permissions updated");
        }

        /// <summary>
        /// Verifica que los permisos Root se permiten para el tenant Root.
        /// </summary>
        [Fact]
        public async Task UpdatePermissions_ShouldAllowRootPermissions_ForRootTenant()
        {
            // Arrange
            var role = new FshRole("TestRole", "Test Role") { Id = Guid.NewGuid().ToString() };
            var command = new UpdatePermissionsCommand
            {
                RoleId = role.Id,
                Permissions = new List<string> { "Permissions.Root.System", "Permissions.Users.View" }
            };

            _roleManagerMock.Setup(x => x.FindByIdAsync(command.RoleId)).ReturnsAsync(role);
            _roleManagerMock.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());

            var (service, db) = CreateRoleServiceWithTenant(TenantConstants.Root.Id);

            // Act
            var result = await service.UpdatePermissionsAsync(command);

            // Assert
            var saved = await db.RoleClaims.ToListAsync();
            saved.Should().HaveCount(2);
            saved.Should().Contain(c => c.ClaimValue == "Permissions.Root.System");
            saved.Should().Contain(c => c.ClaimValue == "Permissions.Users.View");
            result.Should().Be("permissions updated");
        }

        /// <summary>
        /// Verifica que no se añaden permisos duplicados si ya existen en los claims actuales.
        /// </summary>
        [Fact]
        public async Task UpdatePermissions_ShouldNotAddDuplicatePermissions_WhenAlreadyAssigned()
        {
            // Arrange
            var role = new FshRole("TestRole", "Test Role") { Id = Guid.NewGuid().ToString() };
            var command = new UpdatePermissionsCommand
            {
                RoleId = role.Id,
                Permissions = new List<string> { "Permissions.Users.View" }
            };
            var existingClaims = new List<Claim> { new Claim(FshClaims.Permission, "Permissions.Users.View") };

            _roleManagerMock.Setup(x => x.FindByIdAsync(command.RoleId)).ReturnsAsync(role);
            _roleManagerMock.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(existingClaims);

            var (service, db) = CreateRoleServiceWithTenant(TenantConstants.Root.Id);

            // Act
            var result = await service.UpdatePermissionsAsync(command);

            // Assert
            var saved = await db.RoleClaims.ToListAsync();
            saved.Should().BeEmpty();
            _roleManagerMock.Verify(x => x.RemoveClaimAsync(role, It.IsAny<Claim>()), Times.Never);
            result.Should().Be("permissions updated");
        }

        /// <summary>
        /// Verifica que el campo CreatedBy se establece con el Guid del usuario actual.
        /// </summary>
        [Fact]
        public async Task UpdatePermissions_ShouldSetCreatedBy_FromCurrentUserId()
        {
            // Arrange
            var role = new FshRole("TestRole", "Test Role") { Id = Guid.NewGuid().ToString() };
            var command = new UpdatePermissionsCommand
            {
                RoleId = role.Id,
                Permissions = new List<string> { "Permissions.Users.Create" }
            };

            _roleManagerMock.Setup(x => x.FindByIdAsync(command.RoleId)).ReturnsAsync(role);
            _roleManagerMock.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());

            var tenantInfo = new FshTenantInfo { Id = TenantConstants.Root.Id, Identifier = "root", Name = "Root" };
            var tenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
            var multiTenantAccessor = new TestMultiTenantAccessor { MultiTenantContext = tenantContext };

            var dbOptions = Options.Create(new DatabaseOptions { Provider = "InMemory", ConnectionString = "DataSource=:memory:" });
            var efOptions = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase($"IdentityDb_{Guid.NewGuid()}")
                .Options;
            var dbContext = new IdentityDbContext(multiTenantAccessor, efOptions, dbOptions)
            {
                AuditTrails = null!
            };
            dbContext.AuditTrails = dbContext.Set<AuditTrail>();
            dbContext.Database.EnsureCreated();

            var currentUserId = Guid.NewGuid();
            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.GetUserId()).Returns(currentUserId);

            var service = new RoleService(
                _roleManagerMock.Object,
                dbContext,
                multiTenantAccessor,
                currentUserMock.Object);

            // Act
            var result = await service.UpdatePermissionsAsync(command);

            // Assert
            var saved = await dbContext.RoleClaims.ToListAsync();
            saved.Should().HaveCount(1);
            saved[0].CreatedBy.Should().Be(currentUserId.ToString());
            result.Should().Be("permissions updated");
        }

        /// <summary>
        /// Verifica idempotencia: si las permissions no cambian, no se quitan ni se añaden claims.
        /// </summary>
        [Fact]
        public async Task UpdatePermissions_ShouldBeIdempotent_WhenPermissionsUnchanged()
        {
            // Arrange
            var role = new FshRole("TestRole", "Test Role") { Id = Guid.NewGuid().ToString() };
            var permissions = new List<string> { "Permissions.Users.View", "Permissions.Users.Create" };
            var command = new UpdatePermissionsCommand { RoleId = role.Id, Permissions = new List<string>(permissions) };
            var existingClaims = new List<Claim>
            {
                new Claim(FshClaims.Permission, permissions[0]),
                new Claim(FshClaims.Permission, permissions[1])
            };

            _roleManagerMock.Setup(x => x.FindByIdAsync(command.RoleId)).ReturnsAsync(role);
            _roleManagerMock.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(existingClaims);

            var (service, db) = CreateRoleServiceWithTenant(TenantConstants.Root.Id);

            // Act
            var result = await service.UpdatePermissionsAsync(command);

            // Assert
            var saved = await db.RoleClaims.ToListAsync();
            saved.Should().BeEmpty();
            _roleManagerMock.Verify(x => x.RemoveClaimAsync(role, It.IsAny<Claim>()), Times.Never);
            result.Should().Be("permissions updated");
        }

        /// <summary>
        /// Verifica que los valores vacíos o de solo espacios en blanco no se persisten como claims.
        /// </summary>
        [Fact]
        public async Task UpdatePermissions_ShouldIgnoreWhitespacePermissions()
        {
            // Arrange
            var role = new FshRole("TestRole", "Test Role") { Id = Guid.NewGuid().ToString() };
            var command = new UpdatePermissionsCommand
            {
                RoleId = role.Id,
                Permissions = new List<string> { "Permissions.Users.View", string.Empty, "   " }
            };

            _roleManagerMock.Setup(x => x.FindByIdAsync(command.RoleId)).ReturnsAsync(role);
            _roleManagerMock.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());

            var (service, db) = CreateRoleServiceWithTenant(TenantConstants.Root.Id);

            // Act
            var result = await service.UpdatePermissionsAsync(command);

            // Assert
            var saved = await db.RoleClaims.ToListAsync();
            saved.Should().HaveCount(1);
            saved.Should().OnlyContain(c => c.ClaimValue == "Permissions.Users.View");
            result.Should().Be("permissions updated");
        }

        /// <summary>
        /// Verifica que GetWithPermissionsAsync solo devuelve claims de tipo permiso y excluye otros tipos.
        /// </summary>
        [Fact]
        public async Task GetWithPermissions_ShouldNotIncludeNonPermissionClaims()
        {
            // Arrange
            var role = new FshRole("TestRole", "Test Role") { Id = Guid.NewGuid().ToString() };
            _roleManagerMock.Setup(x => x.FindByIdAsync(role.Id)).ReturnsAsync(role);

            var (service, db) = CreateRoleServiceWithRealDbContext();

            db.RoleClaims.AddRange(
                new FshRoleClaim { RoleId = role.Id, ClaimType = FshClaims.Permission, ClaimValue = "Permissions.Users.View" },
                new FshRoleClaim { RoleId = role.Id, ClaimType = "Other", ClaimValue = "SHOULD_NOT_APPEAR" }
            );
            await db.SaveChangesAsync();

            // Act
            var dto = await service.GetWithPermissionsAsync(role.Id, CancellationToken.None);

            // Assert
            dto.Permissions.Should().ContainSingle().Which.Should().Be("Permissions.Users.View");
        }

        /// <summary>
        /// Verifica que si falta el contexto de tenant, se tratan como no-root y se filtran permisos Root.
        /// </summary>
        [Fact]
        public async Task UpdatePermissions_ShouldFilterRootPermissions_WhenTenantContextMissing()
        {
            // Arrange
            var role = new FshRole("TestRole", "Test Role") { Id = Guid.NewGuid().ToString() };
            var command = new UpdatePermissionsCommand
            {
                RoleId = role.Id,
                Permissions = new List<string> { "Permissions.Root.System", "Permissions.Users.View" }
            };

            _roleManagerMock.Setup(x => x.FindByIdAsync(command.RoleId)).ReturnsAsync(role);
            _roleManagerMock.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());

            // DbContext requiere un accessor válido; el servicio puede usar uno sin contexto
            var tenantInfo = new FshTenantInfo { Id = TenantConstants.Root.Id, Identifier = "root", Name = "Root" };
            var tenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
            var accessorForDb = new TestMultiTenantAccessor { MultiTenantContext = tenantContext };

            var dbOptions = Options.Create(new DatabaseOptions { Provider = "InMemory", ConnectionString = "DataSource=:memory:" });
            var efOptions = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase($"IdentityDb_{Guid.NewGuid()}")
                .Options;
            var dbContext = new IdentityDbContext(accessorForDb, efOptions, dbOptions)
            {
                AuditTrails = null!
            };
            dbContext.AuditTrails = dbContext.Set<AuditTrail>();
            dbContext.Database.EnsureCreated();

            var missingAccessor = new TestMultiTenantAccessor { MultiTenantContext = null! };
            var currentUserMock = new Mock<ICurrentUser>();

            var service = new RoleService(
                _roleManagerMock.Object,
                dbContext,
                missingAccessor,
                currentUserMock.Object);

            // Act
            var result = await service.UpdatePermissionsAsync(command);

            // Assert
            var saved = await dbContext.RoleClaims.ToListAsync();
            saved.Should().HaveCount(1);
            saved.Should().OnlyContain(c => c.ClaimValue == "Permissions.Users.View");
            result.Should().Be("permissions updated");
        }

        /// <summary>
        /// Verifica que se respeta el CancellationToken cancelado y no se persisten cambios.
        /// </summary>
        [Fact]
        public async Task UpdatePermissions_ShouldHonorCancellationToken_AndNotPersist()
        {
            // Arrange
            var role = new FshRole("TestRole", "Test Role") { Id = Guid.NewGuid().ToString() };
            var command = new UpdatePermissionsCommand
            {
                RoleId = role.Id,
                Permissions = new List<string> { "Permissions.Users.View" }
            };

            _roleManagerMock.Setup(x => x.FindByIdAsync(command.RoleId)).ReturnsAsync(role);
            _roleManagerMock.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());

            var (service, db) = CreateRoleServiceWithTenant(TenantConstants.Root.Id);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            Func<Task> act = async () => await service.UpdatePermissionsAsync(command, cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
            var saved = await db.RoleClaims.ToListAsync();
            saved.Should().BeEmpty();
        }

        /// <summary>
        /// Verifica que permisos duplicados en la petición se desduplican (case-insensitive) y solo se persiste un claim.
        /// </summary>
        [Fact]
        public async Task UpdatePermissions_ShouldDeduplicateInputPermissions()
        {
            // Arrange
            var role = new FshRole("TestRole", "Test Role") { Id = Guid.NewGuid().ToString() };
            var command = new UpdatePermissionsCommand
            {
                RoleId = role.Id,
                Permissions = new List<string> { "Permissions.Users.View", "Permissions.Users.View", "permissions.users.view" }
            };

            _roleManagerMock.Setup(x => x.FindByIdAsync(command.RoleId)).ReturnsAsync(role);
            _roleManagerMock.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());

            var (service, db) = CreateRoleServiceWithTenant(TenantConstants.Root.Id);

            // Act
            var result = await service.UpdatePermissionsAsync(command);

            // Assert
            var saved = await db.RoleClaims.ToListAsync();
            saved.Should().HaveCount(1);
            saved.Should().OnlyContain(c => c.ClaimType == FshClaims.Permission && c.ClaimValue == "Permissions.Users.View");
            result.Should().Be("permissions updated");
        }

        /// <summary>
        /// Verifica que una excepción de persistencia (DbUpdateException) se propaga cuando SaveChangesAsync falla.
        /// </summary>
        [Fact]
        public async Task UpdatePermissions_ShouldBubbleUpDbUpdateException_WhenSaveChangesFails()
        {
            // Arrange
            var role = new FshRole("TestRole", "Test Role") { Id = Guid.NewGuid().ToString() };
            var command = new UpdatePermissionsCommand
            {
                RoleId = role.Id,
                Permissions = new List<string> { "Permissions.Users.View" }
            };

            _roleManagerMock.Setup(x => x.FindByIdAsync(command.RoleId)).ReturnsAsync(role);
            _roleManagerMock.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());

            // Contexto que lanza DbUpdateException al guardar
            var tenantInfo = new FshTenantInfo { Id = TenantConstants.Root.Id, Identifier = "root", Name = "Root" };
            var tenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
            var accessor = new TestMultiTenantAccessor { MultiTenantContext = tenantContext };

            var dbOptions = Options.Create(new DatabaseOptions { Provider = "InMemory", ConnectionString = "DataSource=:memory:" });
            var efOptions = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase($"IdentityDb_{Guid.NewGuid()}")
                .Options;
            var throwingDb = new ThrowingIdentityDbContext(accessor, efOptions, dbOptions)
            {
                AuditTrails = null!
            };
            throwingDb.AuditTrails = throwingDb.Set<AuditTrail>();
            throwingDb.Database.EnsureCreated();

            var currentUserMock = new Mock<ICurrentUser>();
            var roleService = new RoleService(_roleManagerMock.Object, throwingDb, accessor, currentUserMock.Object);

            // Act
            Func<Task> act = async () => await roleService.UpdatePermissionsAsync(command);

            // Assert
            await act.Should().ThrowAsync<DbUpdateException>();
            var saved = await throwingDb.RoleClaims.ToListAsync();
            saved.Should().BeEmpty();
        }

        /// <summary>
        /// DbContext de pruebas que fuerza un fallo en SaveChangesAsync.
        /// </summary>
        private sealed class ThrowingIdentityDbContext : IdentityDbContext
        {
            public ThrowingIdentityDbContext(
                IMultiTenantContextAccessor<FshTenantInfo> multiTenantContextAccessor,
                DbContextOptions<IdentityDbContext> options,
                IOptions<DatabaseOptions> settings)
                : base(multiTenantContextAccessor, options, settings)
            {
            }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                throw new DbUpdateException("forced failure", new Exception("boom"));
            }
        }
    }
}
