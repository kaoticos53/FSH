using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using FSH.Framework.Core.Tenant.Abstractions;
using FSH.Framework.Core.Tenant.Dtos;
using FSH.Framework.Core.Tenant.Features.GetTenants;

namespace FSH.Framework.Core.Tests.Tenant;

/// <summary>
/// Pruebas para <see cref="GetTenantsHandler"/>.
/// Verifica que el handler devuelve la lista proporcionada por el servicio.
/// </summary>
public class GetTenantsHandlerTests
{
    /// <summary>
    /// Debe devolver la lista proporcionada por el servicio.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnList_FromService()
    {
        // Configuración: se mockea el servicio para devolver una lista de tenants.
        var expected = new List<TenantDetail>
        {
            new TenantDetail { Id = "t1", Name = "Tenant 1", AdminEmail = "a@b.com" },
            new TenantDetail { Id = "t2", Name = "Tenant 2", AdminEmail = "c@d.com" }
        };

        var service = new Mock<ITenantService>(MockBehavior.Strict);
        service.Setup(s => s.GetAllAsync()).ReturnsAsync(expected);

        var handler = new GetTenantsHandler(service.Object);

        // Ejecución
        var result = await handler.Handle(new GetTenantsQuery(), default);

        // Verificación
        result.Should().BeSameAs(expected, "debe devolver la misma lista entregada por el servicio");
        service.Verify(s => s.GetAllAsync(), Times.Once);
        service.VerifyNoOtherCalls();
    }
}
