namespace FSH.Starter.Shared.Authorization;

/// <summary>
/// Contains constants related to identity configuration and settings.
/// </summary>
public static class IdentityConstants
{
    /// <summary>
    /// The minimum required length for user passwords.
    /// </summary>
    public const int PasswordLength = 6;
    /// <summary>
    /// The name of the database schema used for identity-related tables.
    /// </summary>
    public const string SchemaName = "identity";
}
