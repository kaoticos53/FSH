namespace FSH.Framework.Core.Domain.Contracts;

/// <summary>
/// Defines an interface for entities that support soft deletion.
/// Soft deletion allows entities to be marked as deleted without actually removing them from the database.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets or sets the date and time when the entity was soft-deleted.
    /// Returns null if the entity has not been deleted.
    /// </summary>
    DateTimeOffset? Deleted { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who soft-deleted the entity.
    /// Returns null if the entity has not been deleted or if the deleter is unknown.
    /// </summary>
    Guid? DeletedBy { get; set; }
}
