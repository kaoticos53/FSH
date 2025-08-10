using FluentAssertions;
using FluentValidation.TestHelper;
using FSH.Framework.Core.Audit;
using FSH.Framework.Core.Identity.Roles;
using FSH.Framework.Core.Identity.Roles.Features.CreateOrUpdateRole;
using FSH.Framework.Core.Identity.Users.Abstractions;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Infrastructure.Identity.Persistence;
using FSH.Framework.Infrastructure.Identity.Roles;
using FSH.Framework.Infrastructure.Tenant;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FSH.Framework.Core.Tests.Identity.Roles.Features;

/// <summary>
/// Pruebas unitarias para el feature CreateOrUpdateRole.
/// </summary>
public class CreateOrUpdateRoleTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly IRoleService _roleService;
    private readonly Mock<RoleManager<FshRole>> _roleManagerMock;
    private readonly CreateOrUpdateRoleValidator _validator;

    public CreateOrUpdateRoleTests(TestFixture fixture)
    {
        _fixture = fixture;
        _roleService = _fixture.ServiceProvider.GetRequiredService<IRoleService>();
        _roleManagerMock = _fixture.RoleManagerMock;
        _validator = _fixture.ServiceProvider.GetRequiredService<CreateOrUpdateRoleValidator>();
    }

    /// <summary>
    /// Prueba que la creación de un rol tiene éxito cuando el nombre es válido.
    /// </summary>
    [Fact]
    public async Task CreateRole_ShouldSucceed_WhenNameIsValid()
    {
        // Arrange
        var command = new CreateOrUpdateRoleCommand { Name = "New Role", Description = "Role Description" };
        _roleManagerMock.Setup(m => m.FindByNameAsync(command.Name)).ReturnsAsync((FshRole?)null);
        _roleManagerMock.Setup(m => m.CreateAsync(It.IsAny<FshRole>())).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _roleService.CreateOrUpdateRoleAsync(command);

        // Assert
        result.Should().NotBeNull();
        _roleManagerMock.Verify(m => m.CreateAsync(It.Is<FshRole>(r => r.Name == command.Name)), Times.Once);
    }

    /// <summary>
    /// Prueba que la actualización de un rol tiene éxito cuando el rol ya existe.
    /// </summary>
    [Fact]
    public async Task UpdateRole_ShouldSucceed_WhenRoleExists()
    {
        var existingRole = new FshRole("Existing Role", "Existing Description") { Id = Guid.NewGuid().ToString() };
        var command = new CreateOrUpdateRoleCommand
        {
            Id = existingRole.Id,
            Name = "Updated Role",
            Description = "Updated Description"
        };

                _roleManagerMock.Setup(x => x.FindByIdAsync(command.Id)).ReturnsAsync(existingRole);
        _roleManagerMock.Setup(x => x.UpdateAsync(It.IsAny<FshRole>())).ReturnsAsync(IdentityResult.Success);

        var result = await _roleService.CreateOrUpdateRoleAsync(command);

        result.Should().NotBeNull();
        _roleManagerMock.Verify(x => x.UpdateAsync(It.Is<FshRole>(r => r.Id == command.Id)), Times.Once);
    }

        /// <summary>
    /// Prueba que se lanza una excepción de validación al intentar crear o actualizar un rol con un nombre vacío.
    /// </summary>
    [Fact]
    public void CreateOrUpdateRole_ShouldThrowValidationException_WhenNameIsEmpty()
    {
        var command = new CreateOrUpdateRoleCommand { Name = string.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}

