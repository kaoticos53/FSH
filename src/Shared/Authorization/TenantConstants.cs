namespace FSH.Starter.Shared.Authorization;

/// <summary>
/// Contains constants related to tenant configuration and default values.
/// </summary>
public static class TenantConstants
{
    /// <summary>
    /// Contains constants for the root tenant configuration.
    /// </summary>
    public static class Root
    {
        /// <summary>
        /// The unique identifier for the root tenant.
        /// </summary>
        public const string Id = "root";

        /// <summary>
        /// The display name for the root tenant.
        /// </summary>
        public const string Name = "Root";

        /// <summary>
        /// The default email address for the root tenant's admin user.
        /// </summary>
        public const string EmailAddress = "admin@root.com";

        /// <summary>
        /// The default profile picture path for the root tenant's admin user.
        /// </summary>
        public const string DefaultProfilePicture = "assets/defaults/profile-picture.webp";
    }


    /// <summary>
    /// The default password for new users in the system.
    /// Note: This should be changed in production environments.
    /// </summary>
    public const string DefaultPassword = "123Pa$$word!";


    /// <summary>
    /// The claim type identifier used for tenant information in authentication tokens.
    /// </summary>
    public const string Identifier = "tenant";

}
