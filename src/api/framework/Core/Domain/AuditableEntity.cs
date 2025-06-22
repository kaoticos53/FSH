using FSH.Framework.Core.Domain.Contracts;

namespace FSH.Framework.Core.Domain;

/// <summary>
/// Represents an entity that includes auditing information such as creation and modification details.
/// This is a base class for entities that need to track who created/modified them and when.
/// </summary>
/// <typeparam name="TId">The type of the entity's primary key.</typeparam>
public class AuditableEntity<TId> : BaseEntity<TId>, IAuditable, ISoftDeletable
{
    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created the entity.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was last modified.
    /// </summary>
    public DateTimeOffset LastModified { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who last modified the entity.
    /// </summary>
    public Guid? LastModifiedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was soft-deleted.
    /// Null if the entity has not been deleted.
    /// </summary>
    public DateTimeOffset? Deleted { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who soft-deleted the entity.
    /// Null if the entity has not been deleted.
    /// </summary>
    public Guid? DeletedBy { get; set; }
}

/// <summary>
/// A convenience class for entities that use a <see cref="Guid"/> as their primary key
/// and include auditing information.
/// </summary>
public abstract class AuditableEntity : AuditableEntity<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditableEntity"/> class.
    /// The <see cref="BaseEntity{TId}.Id"/> property is automatically set to a new <see cref="Guid"/>.
    /// </summary>
    protected AuditableEntity() => Id = Guid.NewGuid();
}
