using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Core.Tests.Shared;

/// <summary>
/// Manejador de autenticación de pruebas que no autentica (retorna NoResult) para provocar 401 en Challenge.
/// </summary>
public sealed class NoAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// Nombre del esquema de autenticación que no autentica.
    /// </summary>
    public const string SchemeName = "NoAuth";

    /// <summary>
    /// Constructor del manejador que no autentica peticiones.
    /// Español: Utilizado en pruebas para forzar respuestas 401 (Challenge/Unauthorized).
    /// </summary>
    /// <param name="options">Opciones del esquema de autenticación.</param>
    /// <param name="logger">Fábrica de logger para instrumentación.</param>
    /// <param name="encoder">Codificador de URL requerido por el handler base.</param>
    public NoAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.NoResult());
}
