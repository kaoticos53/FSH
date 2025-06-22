namespace FSH.Framework.Core.Audit;

/// <summary>
/// Represents an audit trail entry that records changes made to entities in the system.
/// This class is used to track who made what changes and when.
/// </summary>
public class AuditTrail
{
    /// <summary>
    /// Gets or sets the unique identifier for this audit trail entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who made the changes.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the type of operation performed (e.g., Create, Update, Delete).
    /// </summary>
    public string? Operation { get; set; }

    /// <summary>
    /// Gets or sets the name of the entity type that was modified.
    /// </summary>
    public string? Entity { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the change was made.
    /// </summary>
    public DateTimeOffset DateTime { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized previous values of the modified properties.
    /// This is null for newly created entities.
    /// </summary>
    public string? PreviousValues { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized new values of the modified properties.
    /// This is null for deleted entities.
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Gets or sets a comma-separated list of property names that were modified.
    /// </summary>
    public string? ModifiedProperties { get; set; }

    /// <summary>
    /// Gets or sets the primary key value of the affected entity.
    /// </summary>
    public string? PrimaryKey { get; set; }
}
