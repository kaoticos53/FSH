using FluentAssertions;
using Xunit;
using FSH.Framework.Core.Tenant.Features.ActivateTenant;

namespace FSH.Framework.Core.Tests.Tenant.Features;

/// <summary>
/// Pruebas de unidad para <see cref="ActivateTenantCommand"/> y <see cref="ActivateTenantResponse"/>.
/// Verifica mapeo simple de propiedades.
/// </summary>
public class ActivateTenantCommandResponseTests
{
    /// <summary>
    /// Debe mantener el TenantId proporcionado al construir el comando.
    /// </summary>
    [Fact]
    public void ActivateTenantCommand_ShouldKeep_TenantId()
    {
        var cmd = new ActivateTenantCommand("tenant-001");
        cmd.TenantId.Should().Be("tenant-001");
    }

    /// <summary>
    /// Debe mantener el Status proporcionado al construir la respuesta.
    /// </summary>
    [Fact]
    public void ActivateTenantResponse_ShouldKeep_Status()
    {
        var resp = new ActivateTenantResponse("Activated");
        resp.Status.Should().Be("Activated");
    }
}
