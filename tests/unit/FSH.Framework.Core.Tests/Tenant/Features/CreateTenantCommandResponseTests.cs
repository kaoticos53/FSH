using FluentAssertions;
using Xunit;
using FSH.Framework.Core.Tenant.Features.CreateTenant;

namespace FSH.Framework.Core.Tests.Tenant.Features;

/// <summary>
/// Pruebas para <see cref="CreateTenantCommand"/> y <see cref="CreateTenantResponse"/>.
/// </summary>
public class CreateTenantCommandResponseTests
{
    /// <summary>
    /// Debe mantener los valores proporcionados al construir el comando (incluyendo nulos cuando aplica).
    /// </summary>
    [Fact]
    public void CreateTenantCommand_ShouldKeep_AllProperties()
    {
        var cmd1 = new CreateTenantCommand("t-1", "Acme", null, "admin@acme.com", null);
        cmd1.Id.Should().Be("t-1");
        cmd1.Name.Should().Be("Acme");
        cmd1.ConnectionString.Should().BeNull();
        cmd1.AdminEmail.Should().Be("admin@acme.com");
        cmd1.Issuer.Should().BeNull();

        var cmd2 = new CreateTenantCommand("t-2", "Contoso", "Server=(local);Db=contoso;", "ops@contoso.com", "https://issuer");
        cmd2.Id.Should().Be("t-2");
        cmd2.Name.Should().Be("Contoso");
        cmd2.ConnectionString.Should().Be("Server=(local);Db=contoso;");
        cmd2.AdminEmail.Should().Be("ops@contoso.com");
        cmd2.Issuer.Should().Be("https://issuer");
    }

    /// <summary>
    /// Debe mantener el Id proporcionado al construir la respuesta.
    /// </summary>
    [Fact]
    public void CreateTenantResponse_ShouldKeep_Id()
    {
        var resp = new CreateTenantResponse("tenant-xyz");
        resp.Id.Should().Be("tenant-xyz");
    }
}
