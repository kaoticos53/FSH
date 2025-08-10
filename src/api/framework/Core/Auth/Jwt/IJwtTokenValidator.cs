using System.Security.Claims;
using FSH.Framework.Core.Exceptions;

namespace FSH.Framework.Core.Auth.Jwt;

public interface IJwtTokenValidator
{
    /// <summary>
    /// Validates the specified JWT token and returns the claims principal.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <param name="validateLifetime">Whether to validate the token lifetime.</param>
    /// <returns>The claims principal if the token is valid; otherwise, null.</returns>
    /// <exception cref="UnauthorizedException">Thrown when the token is invalid or expired.</exception>
    ClaimsPrincipal? ValidateToken(string token, bool validateLifetime = true);

    /// <summary>
    /// Gets the user ID from the specified JWT token.
    /// </summary>
    /// <param name="token">The JWT token.</param>
    /// <returns>The user ID if found in the token; otherwise, null.</returns>
    string? GetUserIdFromToken(string token);

    /// <summary>
    /// Gets the user email from the specified JWT token.
    /// </summary>
    /// <param name="token">The JWT token.</param>
    /// <returns>The user email if found in the token; otherwise, null.</returns>
    string? GetUserEmailFromToken(string token);
}
