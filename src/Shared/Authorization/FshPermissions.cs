using System.Collections.ObjectModel;

namespace FSH.Starter.Shared.Authorization;

/// <summary>
/// Defines all the permissions in the application.
/// A permission is a combination of an action and a resource.
/// </summary>
public static class FshPermissions
{
    private static readonly FshPermission[] AllPermissions =
    [     
        //tenants
        new("View Tenants", FshActions.View, FshResources.Tenants, IsRoot: true),
        new("Create Tenants", FshActions.Create, FshResources.Tenants, IsRoot: true),
        new("Update Tenants", FshActions.Update, FshResources.Tenants, IsRoot: true),
        new("Upgrade Tenant Subscription", FshActions.UpgradeSubscription, FshResources.Tenants, IsRoot: true),

        //identity
        new("View Users", FshActions.View, FshResources.Users),
        new("Search Users", FshActions.Search, FshResources.Users),
        new("Create Users", FshActions.Create, FshResources.Users),
        new("Update Users", FshActions.Update, FshResources.Users),
        new("Delete Users", FshActions.Delete, FshResources.Users),
        new("Export Users", FshActions.Export, FshResources.Users),
        new("View UserRoles", FshActions.View, FshResources.UserRoles),
        new("Update UserRoles", FshActions.Update, FshResources.UserRoles),
        new("View Roles", FshActions.View, FshResources.Roles),
        new("Create Roles", FshActions.Create, FshResources.Roles),
        new("Update Roles", FshActions.Update, FshResources.Roles),
        new("Delete Roles", FshActions.Delete, FshResources.Roles),
        new("View RoleClaims", FshActions.View, FshResources.RoleClaims),
        new("Update RoleClaims", FshActions.Update, FshResources.RoleClaims),
        
        //products
        new("View Products", FshActions.View, FshResources.Products, IsBasic: true),
        new("Search Products", FshActions.Search, FshResources.Products, IsBasic: true),
        new("Create Products", FshActions.Create, FshResources.Products),
        new("Update Products", FshActions.Update, FshResources.Products),
        new("Delete Products", FshActions.Delete, FshResources.Products),
        new("Export Products", FshActions.Export, FshResources.Products),

        //brands
        new("View Brands", FshActions.View, FshResources.Brands, IsBasic: true),
        new("Search Brands", FshActions.Search, FshResources.Brands, IsBasic: true),
        new("Create Brands", FshActions.Create, FshResources.Brands),
        new("Update Brands", FshActions.Update, FshResources.Brands),
        new("Delete Brands", FshActions.Delete, FshResources.Brands),
        new("Export Brands", FshActions.Export, FshResources.Brands),

        //todos
        new("View Todos", FshActions.View, FshResources.Todos, IsBasic: true),
        new("Search Todos", FshActions.Search, FshResources.Todos, IsBasic: true),
        new("Create Todos", FshActions.Create, FshResources.Todos),
        new("Update Todos", FshActions.Update, FshResources.Todos),
        new("Delete Todos", FshActions.Delete, FshResources.Todos),
        new("Export Todos", FshActions.Export, FshResources.Todos),

         new("View Hangfire", FshActions.View, FshResources.Hangfire),
         new("View Dashboard", FshActions.View, FshResources.Dashboard),

        //audit
        new("View Audit Trails", FshActions.View, FshResources.AuditTrails),
    ];

    /// <summary>
    /// Gets a read-only list of all permissions defined in the application.
    /// </summary>
    public static IReadOnlyList<FshPermission> All { get; } = new ReadOnlyCollection<FshPermission>(AllPermissions);

    /// <summary>
    /// Gets a read-only list of root-level permissions.
    /// These are typically permissions that can only be assigned to system administrators.
    /// </summary>
    public static IReadOnlyList<FshPermission> Root { get; } = new ReadOnlyCollection<FshPermission>(AllPermissions.Where(p => p.IsRoot).ToArray());

    /// <summary>
    /// Gets a read-only list of all non-root admin permissions.
    /// These are permissions that can be assigned to tenant administrators.
    /// </summary>
    public static IReadOnlyList<FshPermission> Admin { get; } = new ReadOnlyCollection<FshPermission>(AllPermissions.Where(p => !p.IsRoot).ToArray());

    /// <summary>
    /// Gets a read-only list of basic permissions.
    /// These are permissions that are granted to all authenticated users by default.
    /// </summary>
    public static IReadOnlyList<FshPermission> Basic { get; } = new ReadOnlyCollection<FshPermission>(AllPermissions.Where(p => p.IsBasic).ToArray());
}

/// <summary>
/// Represents a permission in the application, which is a combination of an action and a resource.
/// </summary>
/// <param name="Description">A human-readable description of the permission.</param>
/// <param name="Action">The action being permitted (e.g., View, Create, Update, Delete).</param>
/// <param name="Resource">The resource type the permission applies to.</param>
/// <param name="IsBasic">Indicates if this is a basic permission granted to all authenticated users.</param>
/// <param name="IsRoot">Indicates if this is a root-level permission (system admin only).</param>
public record FshPermission(string Description, string Action, string Resource, bool IsBasic = false, bool IsRoot = false)
{
    /// <summary>
    /// Gets the standardized name of the permission in the format "Permissions.{Resource}.{Action}".
    /// </summary>
    public string Name => NameFor(Action, Resource);
    /// <summary>
    /// Generates a standardized permission name from the given action and resource.
    /// </summary>
    /// <param name="action">The action part of the permission (e.g., "View", "Create").</param>
    /// <param name="resource">The resource part of the permission (e.g., "Users", "Products").</param>
    /// <returns>A string in the format "Permissions.{resource}.{action}".</returns>
    public static string NameFor(string action, string resource)
    {
        return $"Permissions.{resource}.{action}";
    }
}


