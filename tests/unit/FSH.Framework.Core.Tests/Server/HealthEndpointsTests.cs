using System.Net;
using FluentAssertions;
using FSH.Starter.Aspire.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FSH.Framework.Core.Tests.Server;

/// <summary>
/// Pruebas de integración ligeras para verificar los endpoints de salud
/// mapeados por <see cref="FSH.Starter.Aspire.ServiceDefaults.Extensions.MapDefaultEndpoints"/>.
/// </summary>
public class HealthEndpointsTests
{
    /// <summary>
    /// Verifica que el endpoint de liveness <c>/alive</c> responde 200 OK cuando la aplicación está levantada.
    /// </summary>
    [Fact]
    public async Task Alive_Should_Return_200()
    {
        // Arrange: construir una app mínima con TestServer y el framework FSH
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        builder.AddServiceDefaults();

        var app = builder.Build();
        app.MapDefaultEndpoints();
        await app.StartAsync();

        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/alive");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "el endpoint /alive debe considerar la app viva");
    }

    /// <summary>
    /// Verifica que el endpoint de health <c>/health</c> responde 200 OK con los health checks por defecto.
    /// </summary>
    [Fact]
    public async Task Health_Should_Return_200()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        builder.AddServiceDefaults();

        var app = builder.Build();
        app.MapDefaultEndpoints();

        await app.StartAsync();

        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "el endpoint /health debe devolver salud OK");
    }
}
