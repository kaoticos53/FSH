using System;
using FluentAssertions;
using Xunit;
using FSH.Framework.Core.Tenant.Features.UpgradeSubscription;

namespace FSH.Framework.Core.Tests.Tenant.Features;

/// <summary>
/// Pruebas para <see cref="UpgradeSubscriptionCommand"/> y <see cref="UpgradeSubscriptionResponse"/>.
/// </summary>
public class UpgradeSubscriptionCommandResponseTests
{
    /// <summary>
    /// Debe inicializar valores por defecto coherentes y permitir asignación de propiedades.
    /// </summary>
    [Fact]
    public void UpgradeSubscriptionCommand_Defaults_And_Setters_ShouldWork()
    {
        var cmd = new UpgradeSubscriptionCommand();

        // Valores por defecto
        cmd.Tenant.Should().BeNull(); // default! -> null al inicio
        cmd.ExtendedExpiryDate.Should().Be(default);

        // Asignaciones
        var newDate = new DateTime(2032, 6, 30);
        cmd.Tenant = "tenant-555";
        cmd.ExtendedExpiryDate = newDate;

        // Verificaciones
        cmd.Tenant.Should().Be("tenant-555");
        cmd.ExtendedExpiryDate.Should().Be(newDate);
    }

    /// <summary>
    /// Debe mantener los valores proporcionados en la respuesta.
    /// </summary>
    [Fact]
    public void UpgradeSubscriptionResponse_ShouldKeep_Values()
    {
        var newValidity = new DateTime(2033, 1, 1);
        var resp = new UpgradeSubscriptionResponse(newValidity, "tenant-abc");

        resp.NewValidity.Should().Be(newValidity);
        resp.Tenant.Should().Be("tenant-abc");
    }
}
