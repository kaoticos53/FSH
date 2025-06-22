namespace FSH.Starter.Shared.Authorization;

/// <summary>
/// Contains the claim type names used throughout the FSH application.
/// These claim types are used to store user identity and authorization information.
/// </summary>
public static class FshClaims
{
    /// <summary>
    /// The claim type for the tenant identifier.
    /// </summary>
    public const string Tenant = "tenant";

    /// <summary>
    /// The claim type for the user's full name.
    /// </summary>
    public const string Fullname = "fullName";

    /// <summary>
    /// The claim type for user permissions.
    /// </summary>
    public const string Permission = "permission";

    /// <summary>
    /// The claim type for the URL of the user's profile image.
    /// </summary>
    public const string ImageUrl = "image_url";

    /// <summary>
    /// The claim type for the IP address of the client making the request.
    /// </summary>
    public const string IpAddress = "ipAddress";

    /// <summary>
    /// The claim type for the token expiration time (as a Unix timestamp).
    /// </summary>
    public const string Expiration = "exp";
}
