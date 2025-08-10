using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FSH.Framework.Infrastructure.Exceptions;
using FSH.Framework.Core.Exceptions;

namespace FSH.Framework.Core.Tests.Infrastructure.Exceptions;

/// <summary>
/// Pruebas para <see cref="CustomExceptionHandler"/>.
/// Valida el mapeo de excepciones comunes a códigos HTTP y payloads ProblemDetails.
/// </summary>
public class CustomExceptionHandlerTests
{
    private static async Task<(HttpContext Ctx, JsonDocument Json)> InvokeAsync(Exception ex)
    {
        // Configuración del HttpContext con un MemoryStream para capturar la respuesta JSON.
        var context = new DefaultHttpContext();
        context.Request.Path = "/test";
        var ms = new MemoryStream();
        context.Response.Body = ms;

        // Logger simulado (no verificamos llamadas de log en estas pruebas).
        var logger = new Mock<ILogger<CustomExceptionHandler>>().Object;
        var handler = new CustomExceptionHandler(logger);

        var handled = await handler.TryHandleAsync(context, ex, default);
        handled.Should().BeTrue("el handler debe indicar que manejó la excepción");

        // Leemos el JSON resultante.
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync();
        var json = JsonDocument.Parse(payload);
        return (context, json);
    }

    /// <summary>
    /// Debe mapear <see cref="FluentValidation.ValidationException"/> a 400 e incluir errores en el cuerpo.
    /// </summary>
    [Fact]
    public async Task Should_Map_ValidationException_To_400_With_Errors()
    {
        // Preparamos una ValidationException con dos errores.
        var failures = new[]
        {
            new FluentValidation.Results.ValidationFailure("Name", "Nombre requerido"),
            new FluentValidation.Results.ValidationFailure("ConnectionString", "Cadena inválida")
        };
        var ex = new FluentValidation.ValidationException(failures);

        var (ctx, json) = await InvokeAsync(ex);

        // Verificaciones de estado y contenido.
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest, "las validaciones deben mapear a 400");
        json.RootElement.GetProperty("detail").GetString().Should().Be("one or more validation errors occurred");
        json.RootElement.GetProperty("instance").GetString().Should().Be("/test");

        // El diccionario Extensions se serializa como propiedades de nivel raíz; esperamos 'errors' con los mensajes.
        json.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.GetArrayLength().Should().Be(2);
    }

    /// <summary>
    /// Debe mapear <see cref="NotFoundException"/> a 404.
    /// </summary>
    [Fact]
    public async Task Should_Map_NotFoundException_To_404()
    {
        var ex = new NotFoundException("no encontrado");
        var (ctx, json) = await InvokeAsync(ex);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound, "NotFound debe mapear a 404");
        json.RootElement.GetProperty("detail").GetString().Should().Be("no encontrado");
        json.RootElement.GetProperty("instance").GetString().Should().Be("/test");
    }

    /// <summary>
    /// Debe mapear <see cref="ForbiddenException"/> a 403.
    /// </summary>
    [Fact]
    public async Task Should_Map_ForbiddenException_To_403()
    {
        var ex = new ForbiddenException();
        var (ctx, json) = await InvokeAsync(ex);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden, "Forbidden debe mapear a 403");
        json.RootElement.GetProperty("detail").GetString().Should().Be("unauthorized");
        json.RootElement.GetProperty("instance").GetString().Should().Be("/test");
    }

    /// <summary>
    /// Debe mapear excepciones no controladas a 500.
    /// </summary>
    [Fact]
    public async Task Should_Map_UnknownException_To_500()
    {
        var ex = new Exception("boom");
        var (ctx, json) = await InvokeAsync(ex);

        // Nota TDD: si falla aquí, ajustaremos el handler para asignar 500 por defecto.
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError, "excepciones no controladas deben mapear a 500");
        json.RootElement.GetProperty("detail").GetString().Should().Be("boom");
        json.RootElement.GetProperty("instance").GetString().Should().Be("/test");
    }
}
