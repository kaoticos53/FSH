using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Core.Tests.Shared;

/// <summary>
/// Manejador de autenticación de pruebas que autentica siempre con un usuario fijo.
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// Nombre del esquema de autenticación de pruebas.
    /// </summary>
    public const string SchemeName = "Test";

    /// <summary>
    /// Constructor del manejador de autenticación de pruebas.
    /// </summary>
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <summary>
    /// Autentica la solicitud asignando un usuario de pruebas con un conjunto de claims mínimo.
    /// </summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Usuario autenticado de pruebas con NameIdentifier
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "123e4567-e89b-12d3-a456-426614174000"),
            new Claim(ClaimTypes.Name, "test-user")
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
