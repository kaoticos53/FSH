namespace FSH.Starter.Shared.Authorization;

/// <summary>
/// Defines the resource types used for authorization in the application.
/// Resources represent the different types of objects that permissions can be applied to.
/// </summary>
public static class FshResources
{
    /// <summary>
    /// Resource type for tenant management operations.
    /// </summary>
    public const string Tenants = nameof(Tenants);

    /// <summary>
    /// Resource type for dashboard access and operations.
    /// </summary>
    public const string Dashboard = nameof(Dashboard);

    /// <summary>
    /// Resource type for Hangfire dashboard and job management.
    /// </summary>
    public const string Hangfire = nameof(Hangfire);

    /// <summary>
    /// Resource type for user management operations.
    /// </summary>
    public const string Users = nameof(Users);

    /// <summary>
    /// Resource type for managing user-role assignments.
    /// </summary>
    public const string UserRoles = nameof(UserRoles);

    /// <summary>
    /// Resource type for role management operations.
    /// </summary>
    public const string Roles = nameof(Roles);

    /// <summary>
    /// Resource type for managing role-claim assignments.
    /// </summary>
    public const string RoleClaims = nameof(RoleClaims);

    /// <summary>
    /// Resource type for product management operations.
    /// </summary>
    public const string Products = nameof(Products);

    /// <summary>
    /// Resource type for brand management operations.
    /// </summary>
    public const string Brands = nameof(Brands);

    /// <summary>
    /// Resource type for to-do item management operations.
    /// </summary>
    public const string Todos = nameof(Todos);

    /// <summary>
    /// Resource type for accessing and managing audit trails.
    /// </summary>
    public const string AuditTrails = nameof(AuditTrails);
}
