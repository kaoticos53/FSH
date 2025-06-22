using System.ComponentModel.DataAnnotations;

namespace FSH.Framework.Core.Auth.Jwt;

/// <summary>
/// Represents the configuration options for JWT (JSON Web Token) authentication.
/// </summary>
public class JwtOptions : IValidatableObject
{
    /// <summary>
    /// Gets or sets the secret key used to sign the JWT tokens.
    /// This should be a secure, random string and kept secret.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issuer of the JWT token.
    /// This is typically the URL of the authentication server.
    /// </summary>
    public string Issuer { get; set; } = "TestIssuer";

    /// <summary>
    /// Gets or sets the intended audience of the JWT token.
    /// This identifies the recipients that the JWT is intended for.
    /// </summary>
    public string Audience { get; set; } = "TestAudience";

    /// <summary>
    /// Gets or sets the duration in minutes after which the access token will expire.
    /// Default is 60 minutes (1 hour).
    /// </summary>
    public int TokenExpirationInMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the duration in days after which the refresh token will expire.
    /// Default is 7 days.
    /// </summary>
    public int RefreshTokenExpirationInDays { get; set; } = 7;

    /// <summary>
    /// Validates the current JwtOptions instance.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A collection of validation results. An empty collection indicates success.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Key))
        {
            yield return new ValidationResult("No Key defined in JwtSettings config", [nameof(Key)]);
        }

        if (string.IsNullOrEmpty(Issuer))
        {
            yield return new ValidationResult("No Issuer defined in JwtSettings config", [nameof(Issuer)]);
        }

        if (string.IsNullOrEmpty(Audience))
        {
            yield return new ValidationResult("No Audience defined in JwtSettings config", [nameof(Audience)]);
        }
    }
}
