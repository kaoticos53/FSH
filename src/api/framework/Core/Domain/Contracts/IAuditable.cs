namespace FSH.Framework.Core.Domain.Contracts;

/// <summary>
/// Defines an interface for entities that include auditing information
/// such as who created/modified them and when.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Gets the date and time when the entity was created.
    /// </summary>
    DateTimeOffset Created { get; }

    /// <summary>
    /// Gets the ID of the user who created the entity.
    /// </summary>
    Guid CreatedBy { get; }

    /// <summary>
    /// Gets the date and time when the entity was last modified.
    /// </summary>
    DateTimeOffset LastModified { get; }

    /// <summary>
    /// Gets the ID of the user who last modified the entity.
    /// Returns null if the entity has never been modified.
    /// </summary>
    Guid? LastModifiedBy { get; }
}
