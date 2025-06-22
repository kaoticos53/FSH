using System.Security.Claims;

namespace FSH.Starter.Shared.Authorization;

/// <summary>
/// Provides extension methods for <see cref="ClaimsPrincipal"/> to simplify access to common claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the email address of the current user from the claims principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance this method extends.</param>
    /// <returns>The email address if found; otherwise, null.</returns>
    public static string? GetEmail(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Email);

    /// <summary>
    /// Gets the tenant identifier of the current user from the claims principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance this method extends.</param>
    /// <returns>The tenant identifier if found; otherwise, null.</returns>
    public static string? GetTenant(this ClaimsPrincipal principal)
        => principal.FindFirstValue(FshClaims.Tenant);

    /// <summary>
    /// Gets the full name of the current user from the claims principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance this method extends.</param>
    /// <returns>The full name if found; otherwise, null.</returns>
    public static string? GetFullName(this ClaimsPrincipal principal)
        => principal?.FindFirst(FshClaims.Fullname)?.Value;

    /// <summary>
    /// Gets the first name of the current user from the claims principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance this method extends.</param>
    /// <returns>The first name if found; otherwise, null.</returns>
    public static string? GetFirstName(this ClaimsPrincipal principal)
        => principal?.FindFirst(ClaimTypes.Name)?.Value;

    /// <summary>
    /// Gets the surname of the current user from the claims principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance this method extends.</param>
    /// <returns>The surname if found; otherwise, null.</returns>
    public static string? GetSurname(this ClaimsPrincipal principal)
        => principal?.FindFirst(ClaimTypes.Surname)?.Value;

    /// <summary>
    /// Gets the phone number of the current user from the claims principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance this method extends.</param>
    /// <returns>The phone number if found; otherwise, null.</returns>
    public static string? GetPhoneNumber(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.MobilePhone);

    /// <summary>
    /// Gets the user identifier of the current user from the claims principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance this method extends.</param>
    /// <returns>The user identifier if found; otherwise, null.</returns>
    public static string? GetUserId(this ClaimsPrincipal principal)
       => principal.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Gets the image URL of the current user from the claims principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance this method extends.</param>
    /// <returns>A <see cref="Uri"/> representing the image URL if valid; otherwise, null.</returns>
    public static Uri? GetImageUrl(this ClaimsPrincipal principal)
    {
        var imageUrl = principal.FindFirstValue(FshClaims.ImageUrl);
        return Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ? uri : null;
    }

    /// <summary>
    /// Gets the expiration time of the current user's authentication token.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance this method extends.</param>
    /// <returns>A <see cref="DateTimeOffset"/> representing the token expiration time.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="principal"/> is null.</exception>
    public static DateTimeOffset GetExpiration(this ClaimsPrincipal principal) =>
        DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(
            principal.FindFirstValue(FshClaims.Expiration)));

    /// <summary>
    /// Gets the value of the first claim of the specified type from the claims principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance this method extends.</param>
    /// <param name="claimType">The claim type to find.</param>
    /// <returns>The value of the first matching claim if found; otherwise, null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="principal"/> is null.</exception>
    private static string? FindFirstValue(this ClaimsPrincipal principal, string claimType) =>
        principal is null
            ? throw new ArgumentNullException(nameof(principal))
            : principal.FindFirst(claimType)?.Value;
}
