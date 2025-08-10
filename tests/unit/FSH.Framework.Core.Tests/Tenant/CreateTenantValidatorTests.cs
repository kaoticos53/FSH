using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using FSH.Framework.Core.Tenant.Abstractions;
using FSH.Framework.Core.Tenant.Features.CreateTenant;
using FSH.Framework.Core.Persistence;

namespace FSH.Framework.Core.Tests.Tenant;

/// <summary>
/// Pruebas para <see cref="CreateTenantValidator"/>.
/// Valida reglas de id único, nombre único, connection string y email.
/// </summary>
public class CreateTenantValidatorTests
{
    private static CreateTenantValidator BuildValidator(
        Mock<ITenantService>? tenantServiceMock = null,
        Mock<IConnectionStringValidator>? csValidatorMock = null)
    {
        tenantServiceMock ??= new Mock<ITenantService>(MockBehavior.Strict);
        csValidatorMock ??= new Mock<IConnectionStringValidator>(MockBehavior.Strict);
        return new CreateTenantValidator(tenantServiceMock.Object, csValidatorMock.Object);
    }

    /// <summary>
    /// Debe fallar cuando el identificador ya existe.
    /// </summary>
    [Fact]
    public async Task Validate_ShouldFail_WhenIdAlreadyExists()
    {
        // Preparación (Given)
        var tenantService = new Mock<ITenantService>(MockBehavior.Strict);
        var csValidator = new Mock<IConnectionStringValidator>(MockBehavior.Strict);
        var cmd = new CreateTenantCommand("dup-id", "Acme", null, "admin@acme.com", null);

        tenantService.Setup(s => s.ExistsWithIdAsync("dup-id")).ReturnsAsync(true);
        tenantService.Setup(s => s.ExistsWithNameAsync("Acme")).ReturnsAsync(false);

        var validator = BuildValidator(tenantService, csValidator);

        // Acción (When)
        var result = await validator.ValidateAsync(cmd, default);

        // Verificación (Then)
        result.IsValid.Should().BeFalse("el id duplicado debe invalidar la solicitud");
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(cmd.Id))
            .Which.ErrorMessage.Should().Be("Tenant dup-id already exists.");
        tenantService.VerifyAll();
        csValidator.VerifyAll();
    }

    /// <summary>
    /// Debe fallar cuando el nombre ya existe.
    /// </summary>
    [Fact]
    public async Task Validate_ShouldFail_WhenNameAlreadyExists()
    {
        // Preparación (Given)
        var tenantService = new Mock<ITenantService>(MockBehavior.Strict);
        var csValidator = new Mock<IConnectionStringValidator>(MockBehavior.Strict);
        var cmd = new CreateTenantCommand("id-1", "Acme", null, "admin@acme.com", null);

        tenantService.Setup(s => s.ExistsWithIdAsync("id-1")).ReturnsAsync(false);
        tenantService.Setup(s => s.ExistsWithNameAsync("Acme")).ReturnsAsync(true);

        var validator = BuildValidator(tenantService, csValidator);

        // Acción (When)
        var result = await validator.ValidateAsync(cmd, default);

        // Verificación (Then)
        result.IsValid.Should().BeFalse("el nombre duplicado debe invalidar la solicitud");
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(cmd.Name))
            .Which.ErrorMessage.Should().Be("Tenant Acme already exists.");
        tenantService.VerifyAll();
        csValidator.VerifyAll();
    }

    /// <summary>
    /// Debe fallar cuando la connection string no es válida y no está vacía.
    /// </summary>
    [Fact]
    public async Task Validate_ShouldFail_WhenConnectionStringIsInvalid_AndNotEmpty()
    {
        // Preparación (Given)
        var tenantService = new Mock<ITenantService>(MockBehavior.Strict);
        var csValidator = new Mock<IConnectionStringValidator>(MockBehavior.Strict);
        var cmd = new CreateTenantCommand("id-1", "Acme", "bad-cs", "admin@acme.com", null);

        tenantService.Setup(s => s.ExistsWithIdAsync("id-1")).ReturnsAsync(false);
        tenantService.Setup(s => s.ExistsWithNameAsync("Acme")).ReturnsAsync(false);
        csValidator.Setup(v => v.TryValidate("bad-cs", It.IsAny<string>())).Returns(false);

        var validator = BuildValidator(tenantService, csValidator);

        // Acción (When)
        var result = await validator.ValidateAsync(cmd, default);

        // Verificación (Then)
        result.IsValid.Should().BeFalse("connection string inválida debe fallar");
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(cmd.ConnectionString))
            .Which.ErrorMessage.Should().Be("Connection string invalid.");
        tenantService.VerifyAll();
        csValidator.VerifyAll();
    }

    /// <summary>
    /// Debe fallar cuando el correo del administrador está vacío.
    /// </summary>
    [Fact]
    public async Task Validate_ShouldFail_WhenAdminEmailIsEmpty()
    {
        // Preparación (Given)
        var tenantService = new Mock<ITenantService>(MockBehavior.Strict);
        var csValidator = new Mock<IConnectionStringValidator>(MockBehavior.Strict);
        var cmd = new CreateTenantCommand("id-1", "Acme", null, "", null);

        tenantService.Setup(s => s.ExistsWithIdAsync("id-1")).ReturnsAsync(false);
        tenantService.Setup(s => s.ExistsWithNameAsync("Acme")).ReturnsAsync(false);
        // cs: null/empty se permite (regla: sólo valida si no está vacío)
        var validator = BuildValidator(tenantService, csValidator);

        // Acción (When)
        var result = await validator.ValidateAsync(cmd, default);

        // Verificación (Then)
        result.IsValid.Should().BeFalse("email vacío debe ser inválido");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.AdminEmail));
        tenantService.VerifyAll();
        csValidator.VerifyAll();
    }

    /// <summary>
    /// Debe ser válido cuando todos los datos son correctos.
    /// </summary>
    [Fact]
    public async Task Validate_ShouldSucceed_WhenDataIsValid()
    {
        // Preparación (Given)
        var tenantService = new Mock<ITenantService>(MockBehavior.Strict);
        var csValidator = new Mock<IConnectionStringValidator>(MockBehavior.Strict);
        var cmd = new CreateTenantCommand("id-1", "Acme", "Server=(local);Database=Db;", "admin@acme.com", "issuer");

        tenantService.Setup(s => s.ExistsWithIdAsync("id-1")).ReturnsAsync(false);
        tenantService.Setup(s => s.ExistsWithNameAsync("Acme")).ReturnsAsync(false);
        csValidator.Setup(v => v.TryValidate(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var validator = BuildValidator(tenantService, csValidator);

        // Acción (When)
        var result = await validator.ValidateAsync(cmd, default);

        // Verificación (Then)
        result.IsValid.Should().BeTrue("todos los datos válidos deben pasar");
        result.Errors.Should().BeEmpty();
        tenantService.VerifyAll();
        csValidator.VerifyAll();
    }
}
