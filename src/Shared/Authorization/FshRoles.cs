using System.Collections.ObjectModel;

namespace FSH.Starter.Shared.Authorization;

/// <summary>
/// Defines the role names and related functionality for the application.
/// </summary>
public static class FshRoles
{
    /// <summary>
    /// The name of the administrator role, which has full access to all features.
    /// </summary>
    public const string Admin = nameof(Admin);

    /// <summary>
    /// The name of the basic user role, which has limited access to features.
    /// </summary>
    public const string Basic = nameof(Basic);

    /// <summary>
    /// Gets a read-only list of all default role names in the application.
    /// </summary>
    public static IReadOnlyList<string> DefaultRoles { get; } = new ReadOnlyCollection<string>(new[]
    {
        Admin,
        Basic
    });

    /// <summary>
    /// Determines whether the specified role name is one of the default roles.
    /// </summary>
    /// <param name="roleName">The name of the role to check.</param>
    /// <returns>true if the role is a default role; otherwise, false.</returns>
    public static bool IsDefault(string roleName) => DefaultRoles.Any(r => r == roleName);
}
