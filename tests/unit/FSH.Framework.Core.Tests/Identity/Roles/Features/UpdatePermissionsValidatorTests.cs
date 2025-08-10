using FluentAssertions;
using FluentValidation.TestHelper;
using FSH.Framework.Core.Identity.Roles.Features.UpdatePermissions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FSH.Framework.Core.Tests.Identity.Roles.Features;

/// <summary>
/// Pruebas unitarias para el validador de UpdatePermissions.
/// </summary>
public class UpdatePermissionsValidatorTests : IClassFixture<TestFixture>
{
    private readonly UpdatePermissionsValidator _validator;

    /// <summary>
    /// Inicializa la clase de pruebas del validador de UpdatePermissions.
    /// </summary>
    /// <param name="fixture">Fixture de pruebas que proporciona el contenedor DI.</param>
    public UpdatePermissionsValidatorTests(TestFixture fixture)
    {
        _validator = fixture.ServiceProvider.GetRequiredService<UpdatePermissionsValidator>();
    }

    /// <summary>
    /// Verifica que se produce error cuando la lista de permisos es nula.
    /// </summary>
    [Fact]
    public void Validator_ShouldFail_WhenPermissionsIsNull()
    {
        var cmd = new UpdatePermissionsCommand { RoleId = "role-id", Permissions = null! };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Permissions);
    }

    /// <summary>
    /// Verifica que se produce error cuando existen elementos vacíos o whitespace en la lista.
    /// </summary>
    [Fact]
    public void Validator_ShouldFail_WhenPermissionsContainsWhitespaceOnly()
    {
        var cmd = new UpdatePermissionsCommand { RoleId = "role-id", Permissions = new() { " ", "\t", "\n" } };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveAnyValidationError();
        result.ShouldHaveValidationErrorFor("Permissions[0]");
        result.ShouldHaveValidationErrorFor("Permissions[1]");
        result.ShouldHaveValidationErrorFor("Permissions[2]");
    }

    /// <summary>
    /// Verifica que pasa la validación cuando los permisos son válidos (aun con espacios, tras recorte).
    /// </summary>
    [Fact]
    public void Validator_ShouldSucceed_WhenPermissionsAreValid()
    {
        var cmd = new UpdatePermissionsCommand { RoleId = "role-id", Permissions = new() { "  Permissions.Users.View  " } };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
