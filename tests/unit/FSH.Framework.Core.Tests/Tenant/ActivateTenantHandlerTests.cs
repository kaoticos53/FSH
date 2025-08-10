using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using FSH.Framework.Core.Tenant.Abstractions;
using FSH.Framework.Core.Tenant.Features.ActivateTenant;

namespace FSH.Framework.Core.Tests.Tenant;

/// <summary>
/// Pruebas para <see cref="ActivateTenantHandler"/>.
/// Verifica que el handler devuelve el estado proporcionado por el servicio.
/// </summary>
public class ActivateTenantHandlerTests
{
    /// <summary>
    /// Debe devolver el estado proporcionado por el servicio.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnStatus_FromService()
    {
        // Configuración: se prepara el mock y el handler.
        var tenantId = "tenant-1";
        var expectedStatus = "Activated";
        var token = new CancellationTokenSource().Token;

        var service = new Mock<ITenantService>(MockBehavior.Strict);
        service.Setup(s => s.ActivateAsync(tenantId, token))
               .ReturnsAsync(expectedStatus);

        var handler = new ActivateTenantHandler(service.Object);

        // Ejecución: se llama al handler.
        var response = await handler.Handle(new ActivateTenantCommand(tenantId), token);

        // Verificación: se comprueba el resultado y la interacción con el servicio.
        response.Status.Should().Be(expectedStatus, "debe devolver el estado entregado por el servicio");
        service.Verify(s => s.ActivateAsync(tenantId, token), Times.Once);
        service.VerifyNoOtherCalls();
    }
}
