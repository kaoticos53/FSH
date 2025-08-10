using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using FSH.Framework.Core.Tenant.Abstractions;
using FSH.Framework.Core.Tenant.Features.CreateTenant;

namespace FSH.Framework.Core.Tests.Tenant.Features.Handlers;

/// <summary>
/// Pruebas de unidad para <see cref="CreateTenantHandler"/>.
/// Verifica la llamada a ITenantService y la propagación del CancellationToken.
/// </summary>
public class CreateTenantHandlerTests
{
    /// <summary>
    /// Debe devolver el Id generado por el servicio y propagar el CancellationToken.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturn_Id_From_Service_And_Propagate_CancellationToken()
    {
        // Arrange
        var service = new Mock<ITenantService>(MockBehavior.Strict);
        var handler = new CreateTenantHandler(service.Object);
        var cmd = new CreateTenantCommand("t-100", "Acme", null, "admin@acme.com", null);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        service
            .Setup(s => s.CreateAsync(cmd, It.Is<CancellationToken>(ct => ct == token)))
            .ReturnsAsync("t-100")
            .Verifiable();

        // Act
        var result = await handler.Handle(cmd, token);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("t-100");
        service.Verify();
    }
}
