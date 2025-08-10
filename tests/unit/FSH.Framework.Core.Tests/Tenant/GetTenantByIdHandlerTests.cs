using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using FSH.Framework.Core.Tenant.Abstractions;
using FSH.Framework.Core.Tenant.Features.GetTenantById;
using FSH.Framework.Core.Tenant.Dtos;
using FSH.Framework.Core.Exceptions;

namespace FSH.Framework.Core.Tests.Tenant;

/// <summary>
/// Pruebas para <see cref="GetTenantByIdHandler"/>.
/// Verifica camino feliz y propagación de NotFoundException.
/// </summary>
public class GetTenantByIdHandlerTests
{
    /// <summary>
    /// Verifica el camino feliz: devuelve el detalle del tenant cuando existe.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnTenantDetail_WhenFound()
    {
        // Preparación (Given)
        var serviceMock = new Mock<ITenantService>(MockBehavior.Strict);
        var cts = new CancellationTokenSource();
        var expected = new TenantDetail
        {
            Id = "tenant-001",
            Name = "Acme",
            AdminEmail = "admin@acme.com",
            IsActive = true,
        };

        serviceMock
            .Setup(s => s.GetByIdAsync("tenant-001"))
            .ReturnsAsync(expected);

        var handler = new GetTenantByIdHandler(serviceMock.Object);
        var query = new GetTenantByIdQuery("tenant-001");

        // Acción (When)
        var result = await handler.Handle(query, cts.Token);

        // Verificación (Then)
        result.Should().BeSameAs(expected);
        serviceMock.Verify(s => s.GetByIdAsync("tenant-001"), Times.Once);
        serviceMock.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Verifica que la excepción <see cref="NotFoundException"/> se propaga cuando el servicio la lanza.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldPropagateNotFoundException_WhenServiceThrows()
    {
        // Preparación (Given)
        var serviceMock = new Mock<ITenantService>(MockBehavior.Strict);
        var handler = new GetTenantByIdHandler(serviceMock.Object);
        var query = new GetTenantByIdQuery("missing");

        serviceMock
            .Setup(s => s.GetByIdAsync("missing"))
            .ThrowsAsync(new NotFoundException("Tenant missing"));

        // Acción y Verificación (When/Then)
        await FluentActions
            .Awaiting(() => handler.Handle(query, default))
            .Should().ThrowAsync<NotFoundException>();

        serviceMock.Verify(s => s.GetByIdAsync("missing"), Times.Once);
        serviceMock.VerifyNoOtherCalls();
    }
}
