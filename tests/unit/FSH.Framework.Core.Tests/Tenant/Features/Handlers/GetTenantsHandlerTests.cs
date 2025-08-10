using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using FSH.Framework.Core.Tenant.Abstractions;
using FSH.Framework.Core.Tenant.Dtos;
using FSH.Framework.Core.Tenant.Features.GetTenants;

namespace FSH.Framework.Core.Tests.Tenant.Features.Handlers;

/// <summary>
/// Pruebas de unidad para <see cref="GetTenantsHandler"/>.
/// Verifica la delegación a ITenantService y el valor devuelto.
/// </summary>
public class GetTenantsHandlerTests
{
    /// <summary>
    /// Debe devolver la lista recibida del servicio.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturn_List_From_Service()
    {
        // Arrange
        var list = new List<TenantDetail>
        {
            new TenantDetail { Id = "t1", Name = "Acme" },
            new TenantDetail { Id = "t2", Name = "Contoso" }
        };

        var service = new Mock<ITenantService>(MockBehavior.Strict);
        service.Setup(s => s.GetAllAsync()).ReturnsAsync(list).Verifiable();
        var handler = new GetTenantsHandler(service.Object);

        // Act
        var result = await handler.Handle(new GetTenantsQuery(), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(list);
        service.Verify();
    }
}
