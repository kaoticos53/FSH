namespace FSH.Framework.Core.Authorization;

/// <summary>
/// Defines the claim types used in the FSH application's JWT tokens.
/// These claims provide identity and authorization information about the authenticated user.
/// </summary>
public static class FSHClaims
{
    /// <summary>
    /// The claim type for the tenant identifier.
    /// </summary>
    public const string Tenant = "tenant";

    /// <summary>
    /// The claim type for the user's unique identifier.
    /// </summary>
    public const string UserId = "userId";

    /// <summary>
    /// The claim type for user permissions.
    /// </summary>
    public const string Permission = "permission";

    /// <summary>
    /// The claim type for the URL of the user's profile image.
    /// </summary>
    public const string ImageUrl = "image_url";

    /// <summary>
    /// The claim type for the user's full name.
    /// </summary>
    public const string Fullname = "fullName";

    /// <summary>
    /// The claim type for the IP address of the client that made the request.
    /// </summary>
    public const string IpAddress = "ipAddress";

    /// <summary>
    /// The claim type for the token expiration time (as a Unix timestamp).
    /// </summary>
    public const string Expiration = "exp";

    /// <summary>
    /// The claim type for the token issuer.
    /// Standard JWT claim defined in RFC 7519.
    /// </summary>
    public const string Issuer = "iss";

    /// <summary>
    /// The claim type for the token audience.
    /// Standard JWT claim defined in RFC 7519.
    /// </summary>
    public const string Audience = "aud";
    
    // Agrega aquí más claims según sea necesario
}
