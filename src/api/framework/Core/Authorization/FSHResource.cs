namespace FSH.Framework.Core.Authorization;

/// <summary>
/// Defines the resource types and default values used in the FSH application.
/// These resources are used for authorization and configuration purposes.
/// </summary>
public static class FshResource
{
    /// <summary>
    /// The name of the root resource or role with the highest level of access.
    /// </summary>
    public const string Root = "Root";

    /// <summary>
    /// The name of the administrator role with elevated privileges.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// The name of the basic user role with standard privileges.
    /// </summary>
    public const string BasicRole = "Basic";

    /// <summary>
    /// The default password for new users.
    /// Note: This should be changed in a production environment.
    /// </summary>
    public const string DefaultPassword = "Password123!";
    
    // Agrega aquí más recursos según sea necesario
}
