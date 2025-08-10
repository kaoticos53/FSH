using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using FSH.Framework.Core.Tenant.Abstractions;
using FSH.Framework.Core.Tenant.Features.UpgradeSubscription;

namespace FSH.Framework.Core.Tests.Tenant;

/// <summary>
/// Pruebas para <see cref="UpgradeSubscriptionHandler"/>.
/// Valida que el handler devuelve la nueva validez y conserva el tenant del comando.
/// </summary>
public class UpgradeSubscriptionHandlerTests
{
    /// <summary>
    /// Debe devolver la nueva validez desde el servicio y eco del tenant enviado.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnNewValidity_AndEchoTenant()
    {
        // Configuración: se prepara el mock del servicio para devolver la nueva fecha de validez.
        var tenantId = "tenant-3";
        var extended = new DateTime(2030, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var expectedDate = extended.AddYears(1);

        var service = new Mock<ITenantService>(MockBehavior.Strict);
        service.Setup(s => s.UpgradeSubscription(tenantId, extended))
               .ReturnsAsync(expectedDate);

        var handler = new UpgradeSubscriptionHandler(service.Object);

        var cmd = new UpgradeSubscriptionCommand { Tenant = tenantId, ExtendedExpiryDate = extended };

        // Ejecución
        var response = await handler.Handle(cmd, default);

        // Verificación: comprobamos la fecha y el eco del tenant.
        response.NewValidity.Should().Be(expectedDate, "debe reflejar el valor devuelto por el servicio");
        response.Tenant.Should().Be(tenantId, "debe devolver el tenant indicado en el comando");
        service.Verify(s => s.UpgradeSubscription(tenantId, extended), Times.Once);
        service.VerifyNoOtherCalls();
    }
}
