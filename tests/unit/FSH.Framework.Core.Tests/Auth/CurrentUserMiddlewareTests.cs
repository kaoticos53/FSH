using System.Security.Claims;
using FluentAssertions;
using FSH.Framework.Infrastructure.Auth;
using FSH.Framework.Core.Identity.Users.Abstractions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace FSH.Framework.Core.Tests.Auth;

/// <summary>
/// Pruebas unitarias para <see cref="CurrentUserMiddleware"/> verificando que inicializa el usuario actual
/// y continúa el pipeline correctamente.
/// </summary>
public class CurrentUserMiddlewareTests
{
    /// <summary>
    /// Verifica que con un usuario autenticado el middleware establece el usuario actual
    /// y continúa el pipeline invocando al siguiente delegado.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_Should_Set_CurrentUser_And_Call_Next()
    {
        // Arrange
        var initializerMock = new Mock<ICurrentUserInitializer>(MockBehavior.Strict);
        ClaimsPrincipal? captured = null;
        initializerMock
            .Setup(i => i.SetCurrentUser(It.IsAny<ClaimsPrincipal>()))
            .Callback<ClaimsPrincipal>(p => captured = p);

        var middleware = new CurrentUserMiddleware(initializerMock.Object);
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") }, "test");
        context.User = new ClaimsPrincipal(identity);

        var called = false;
        RequestDelegate next = _ => { called = true; return Task.CompletedTask; };

        // Act
        await middleware.InvokeAsync(context, next);

        // Assert
        called.Should().BeTrue("el siguiente middleware debe ejecutarse");
        captured.Should().NotBeNull();
        captured!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be("user-1", "debe establecer el usuario actual correctamente");
        initializerMock.Verify(i => i.SetCurrentUser(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    /// <summary>
    /// Verifica que con usuario anónimo el middleware sigue estableciendo el usuario en el contexto
    /// y continúa el pipeline sin errores.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_Should_Handle_Anonymous_User_And_Call_Next()
    {
        // Arrange
        var initializerMock = new Mock<ICurrentUserInitializer>(MockBehavior.Strict);
        initializerMock
            .Setup(i => i.SetCurrentUser(It.IsAny<ClaimsPrincipal>()));

        var middleware = new CurrentUserMiddleware(initializerMock.Object);
        var context = new DefaultHttpContext(); // Usuario anónimo por defecto

        var called = false;
        RequestDelegate next = _ => { called = true; return Task.CompletedTask; };

        // Act
        await middleware.InvokeAsync(context, next);

        // Assert
        called.Should().BeTrue("debe continuar aunque el usuario sea anónimo");
        initializerMock.Verify(i => i.SetCurrentUser(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }
}
