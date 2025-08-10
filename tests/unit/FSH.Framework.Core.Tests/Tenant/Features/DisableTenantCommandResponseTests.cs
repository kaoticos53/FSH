using FluentAssertions;
using Xunit;
using FSH.Framework.Core.Tenant.Features.DisableTenant;

namespace FSH.Framework.Core.Tests.Tenant.Features;

/// <summary>
/// Pruebas para <see cref="DisableTenantCommand"/> y <see cref="DisableTenantResponse"/>.
/// </summary>
public class DisableTenantCommandResponseTests
{
    /// <summary>
    /// Debe mantener el TenantId proporcionado al construir el comando.
    /// </summary>
    [Fact]
    public void DisableTenantCommand_ShouldKeep_TenantId()
    {
        var cmd = new DisableTenantCommand("tenant-XYZ");
        cmd.TenantId.Should().Be("tenant-XYZ");
    }

    /// <summary>
    /// Debe mantener el Status proporcionado al construir la respuesta.
    /// </summary>
    [Fact]
    public void DisableTenantResponse_ShouldKeep_Status()
    {
        var resp = new DisableTenantResponse("Deactivated");
        resp.Status.Should().Be("Deactivated");
    }
}
