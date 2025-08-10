using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using FSH.Framework.Core.Tenant.Abstractions;
using FSH.Framework.Core.Tenant.Features.DisableTenant;

namespace FSH.Framework.Core.Tests.Tenant;

/// <summary>
/// Pruebas para <see cref="DisableTenantHandler"/>.
/// Verifica que el handler devuelve el estado proporcionado por el servicio.
/// </summary>
public class DisableTenantHandlerTests
{
    /// <summary>
    /// Debe devolver el estado proporcionado por el servicio.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnStatus_FromService()
    {
        // Configuración: se prepara el mock de servicio y el handler.
        var tenantId = "tenant-2";
        var expectedStatus = "Deactivated";

        var service = new Mock<ITenantService>(MockBehavior.Strict);
        service.Setup(s => s.DeactivateAsync(tenantId))
               .ReturnsAsync(expectedStatus);

        var handler = new DisableTenantHandler(service.Object);

        // Ejecución: se invoca el handler con el comando.
        var response = await handler.Handle(new DisableTenantCommand(tenantId), default);

        // Verificación: se comprueba el estado y la interacción.
        response.Status.Should().Be(expectedStatus, "debe devolver el estado entregado por el servicio");
        service.Verify(s => s.DeactivateAsync(tenantId), Times.Once);
        service.VerifyNoOtherCalls();
    }
}
