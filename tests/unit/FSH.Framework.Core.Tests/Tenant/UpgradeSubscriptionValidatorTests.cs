using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using FSH.Framework.Core.Tenant.Features.UpgradeSubscription;

namespace FSH.Framework.Core.Tests.Tenant;

/// <summary>
/// Pruebas para <see cref="UpgradeSubscriptionValidator"/>.
/// Valida que Tenant no esté vacío y que ExtendedExpiryDate sea mayor que ahora (UTC).
/// </summary>
public class UpgradeSubscriptionValidatorTests
{
    /// <summary>
    /// Debe fallar cuando Tenant está vacío.
    /// </summary>
    [Fact]
    public async Task Validate_ShouldFail_WhenTenantIsEmpty()
    {
        // Preparación (Given)
        var validator = new UpgradeSubscriptionValidator();
        var cmd = new UpgradeSubscriptionCommand
        {
            Tenant = string.Empty,
            ExtendedExpiryDate = DateTime.UtcNow.AddMinutes(5)
        };

        // Acción (When)
        var result = await validator.ValidateAsync(cmd, CancellationToken.None);

        // Verificación (Then)
        result.IsValid.Should().BeFalse("Tenant vacío debe invalidar la solicitud");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.Tenant));
    }

    /// <summary>
    /// Debe fallar cuando ExtendedExpiryDate no es mayor que ahora (UTC).
    /// </summary>
    [Fact]
    public async Task Validate_ShouldFail_WhenExtendedExpiryDateIsNotInFuture()
    {
        // Preparación (Given)
        var validator = new UpgradeSubscriptionValidator();
        var cmd = new UpgradeSubscriptionCommand
        {
            Tenant = "tenant-1",
            ExtendedExpiryDate = DateTime.UtcNow.AddSeconds(-1)
        };

        // Acción (When)
        var result = await validator.ValidateAsync(cmd, CancellationToken.None);

        // Verificación (Then)
        result.IsValid.Should().BeFalse("fecha no futura debe fallar");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.ExtendedExpiryDate));
    }

    /// <summary>
    /// Debe ser válido cuando Tenant tiene valor y la fecha es futura.
    /// </summary>
    [Fact]
    public async Task Validate_ShouldSucceed_WhenInputIsValid()
    {
        // Preparación (Given)
        var validator = new UpgradeSubscriptionValidator();
        var cmd = new UpgradeSubscriptionCommand
        {
            Tenant = "tenant-1",
            ExtendedExpiryDate = DateTime.UtcNow.AddMinutes(1)
        };

        // Acción (When)
        var result = await validator.ValidateAsync(cmd, CancellationToken.None);

        // Verificación (Then)
        result.IsValid.Should().BeTrue("entrada válida debe pasar");
        result.Errors.Should().BeEmpty();
    }
}
