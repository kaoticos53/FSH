namespace FSH.Framework.Core.Audit;

/// <summary>
/// Specifies the type of operation that was performed on an audited entity.
/// This enum is used to categorize audit trail entries.
/// </summary>
public enum TrailType
{
    /// <summary>
    /// No operation type specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates that a new entity was created.
    /// </summary>
    Create = 1,

    /// <summary>
    /// Indicates that an existing entity was modified.
    /// </summary>
    Update = 2,

    /// <summary>
    /// Indicates that an entity was deleted.
    /// </summary>
    Delete = 3
}
