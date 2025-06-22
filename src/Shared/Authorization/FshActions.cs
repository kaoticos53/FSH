namespace FSH.Starter.Shared.Authorization;

/// <summary>
/// Defines the standard action names for FSH application permissions.
/// These actions represent the basic CRUD operations and other common actions
/// that can be performed on application resources.
/// </summary>
public static class FshActions
{
    /// <summary>
    /// Action name for viewing a resource.
    /// </summary>
    public const string View = nameof(View);

    /// <summary>
    /// Action name for searching resources.
    /// </summary>
    public const string Search = nameof(Search);

    /// <summary>
    /// Action name for creating a new resource.
    /// </summary>
    public const string Create = nameof(Create);

    /// <summary>
    /// Action name for updating an existing resource.
    /// </summary>
    public const string Update = nameof(Update);

    /// <summary>
    /// Action name for deleting a resource.
    /// </summary>
    public const string Delete = nameof(Delete);

    /// <summary>
    /// Action name for exporting resource data.
    /// </summary>
    public const string Export = nameof(Export);

    /// <summary>
    /// Action name for generating resources or reports.
    /// </summary>
    public const string Generate = nameof(Generate);

    /// <summary>
    /// Action name for cleaning up resources.
    /// </summary>
    public const string Clean = nameof(Clean);

    /// <summary>
    /// Action name for upgrading a subscription.
    /// </summary>
    public const string UpgradeSubscription = nameof(UpgradeSubscription);
}
