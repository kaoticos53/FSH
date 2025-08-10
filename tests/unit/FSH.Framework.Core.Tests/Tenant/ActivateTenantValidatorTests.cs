using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using FSH.Framework.Core.Tenant.Features.ActivateTenant;

namespace FSH.Framework.Core.Tests.Tenant;

/// <summary>
/// Pruebas para <see cref="ActivateTenantValidator"/>.
/// Valida que el TenantId no esté vacío.
/// </summary>
public class ActivateTenantValidatorTests
{
    /// <summary>
    /// Debe fallar cuando TenantId está vacío.
    /// </summary>
    [Fact]
    public async Task Validate_ShouldFail_WhenTenantIdIsEmpty()
    {
        // Preparación (Given)
        var validator = new ActivateTenantValidator();
        var cmd = new ActivateTenantCommand("");

        // Acción (When)
        var result = await validator.ValidateAsync(cmd, CancellationToken.None);

        // Verificación (Then)
        result.IsValid.Should().BeFalse("TenantId vacío debe invalidar la solicitud");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.TenantId));
    }

    /// <summary>
    /// Debe ser válido cuando TenantId tiene contenido.
    /// </summary>
    [Fact]
    public async Task Validate_ShouldSucceed_WhenTenantIdIsProvided()
    {
        // Preparación (Given)
        var validator = new ActivateTenantValidator();
        var cmd = new ActivateTenantCommand("tenant-1");

        // Acción (When)
        var result = await validator.ValidateAsync(cmd, CancellationToken.None);

        // Verificación (Then)
        result.IsValid.Should().BeTrue("TenantId con valor debe ser válido");
        result.Errors.Should().BeEmpty();
    }
}
