using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FluentValidation;
using FSH.Framework.Core.Identity.Roles;
using FSH.Framework.Core.Identity.Roles.Features.UpdatePermissions;
using FSH.Framework.Infrastructure.Identity.Roles.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace FSH.Framework.Core.Tests.Identity.Roles.Features;

/// <summary>
/// Pruebas del endpoint de actualización de permisos para verificar validación y códigos de estado.
/// </summary>
public class UpdateRolePermissionsEndpointTests
{
    /// <summary>
    /// Construye una aplicación mínima con TestServer, registrando dependencias necesarias y el endpoint.
    /// </summary>
    private static WebApplication BuildTestApp(Mock<IRoleService> roleServiceMock)
    {
        // Ahora reutilizamos el helper compartido del TestFixture para construir la app de endpoint.
        return TestFixture.BuildRoleEndpointApp(roleServiceMock);
    }

    /// <summary>
    /// Debe devolver 400 cuando la lista contiene elementos de solo espacios.
    /// </summary>
    [Fact]
    public async Task ShouldReturnBadRequest_WhenPermissionsContainWhitespaceOnly()
    {
        var roleServiceMock = new Mock<IRoleService>(MockBehavior.Strict);
        await using var app = BuildTestApp(roleServiceMock);
        await app.StartAsync();
        var client = app.GetTestClient();

        var id = "role-1";
        var cmd = new UpdatePermissionsCommand { RoleId = id, Permissions = new() { "   " } };

        var response = await client.PutAsJsonAsync($"/{id}/permissions", cmd);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        roleServiceMock.Verify(s => s.UpdatePermissionsAsync(It.IsAny<UpdatePermissionsCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Debe devolver 400 cuando la lista de permisos es nula.
    /// </summary>
    [Fact]
    public async Task ShouldReturnBadRequest_WhenPermissionsIsNull()
    {
        var roleServiceMock = new Mock<IRoleService>(MockBehavior.Strict);
        await using var app = BuildTestApp(roleServiceMock);
        await app.StartAsync();
        var client = app.GetTestClient();

        var id = "role-2";
        var cmd = new UpdatePermissionsCommand { RoleId = id, Permissions = null! };

        var response = await client.PutAsJsonAsync($"/{id}/permissions", cmd);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        roleServiceMock.Verify(s => s.UpdatePermissionsAsync(It.IsAny<UpdatePermissionsCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Debe devolver 200 OK cuando la entrada es válida y delega al servicio.
    /// </summary>
    [Fact]
    public async Task ShouldReturnOk_WhenRequestIsValid()
    {
        var roleServiceMock = new Mock<IRoleService>(MockBehavior.Strict);
        roleServiceMock
            .Setup(s => s.UpdatePermissionsAsync(It.IsAny<UpdatePermissionsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("permissions updated");

        await using var app = BuildTestApp(roleServiceMock);
        await app.StartAsync();
        var client = app.GetTestClient();

        var id = "role-3";
        var cmd = new UpdatePermissionsCommand { RoleId = id, Permissions = new() { "Permissions.Users.View" } };

        var response = await client.PutAsJsonAsync($"/{id}/permissions", cmd);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        roleServiceMock.Verify(s => s.UpdatePermissionsAsync(It.Is<UpdatePermissionsCommand>(r => r.RoleId == id), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Debe devolver 400 cuando el id de la ruta no coincide con el RoleId del cuerpo.
    /// </summary>
    [Fact]
    public async Task ShouldReturnBadRequest_WhenRouteIdDiffersFromBodyRoleId()
    {
        var roleServiceMock = new Mock<IRoleService>(MockBehavior.Strict);
        await using var app = BuildTestApp(roleServiceMock);
        await app.StartAsync();
        var client = app.GetTestClient();

        var routeId = "route-id";
        var bodyId = "body-id";
        var cmd = new UpdatePermissionsCommand { RoleId = bodyId, Permissions = new() { "Permissions.Users.View" } };

        var response = await client.PutAsJsonAsync($"/{routeId}/permissions", cmd);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        roleServiceMock.Verify(s => s.UpdatePermissionsAsync(It.IsAny<UpdatePermissionsCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
