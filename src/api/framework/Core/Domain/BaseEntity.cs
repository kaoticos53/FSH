using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Framework.Core.Domain.Events;

namespace FSH.Framework.Core.Domain;

/// <summary>
/// Represents the base class for all entities in the domain model.
/// This abstract class provides common functionality for all entities, such as ID and domain event handling.
/// </summary>
/// <typeparam name="TId">The type of the entity's primary key.</typeparam>
public abstract class BaseEntity<TId> : IEntity<TId>
{
    /// <summary>
    /// Gets or sets the unique identifier for this entity.
    /// </summary>
    public TId Id { get; protected init; } = default!;
    /// <summary>
    /// Gets the collection of domain events raised by this entity.
    /// These events will be dispatched when the entity is saved to the database.
    /// </summary>
    [NotMapped]
    public Collection<DomainEvent> DomainEvents { get; } = new Collection<DomainEvent>();
    /// <summary>
    /// Adds a domain event to this entity's collection of events.
    /// The event will be dispatched when the entity is saved to the database.
    /// </summary>
    /// <param name="event">The domain event to add.</param>
    public void QueueDomainEvent(DomainEvent @event)
    {
        if (!DomainEvents.Contains(@event))
            DomainEvents.Add(@event);
    }
}

/// <summary>
/// A convenience class for entities that use a <see cref="Guid"/> as their primary key.
/// This class automatically initializes the <see cref="BaseEntity{TId}.Id"/> property with a new <see cref="Guid"/>.
/// </summary>
public abstract class BaseEntity : BaseEntity<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntity"/> class.
    /// The <see cref="BaseEntity{TId}.Id"/> property is automatically set to a new <see cref="Guid"/>.
    /// </summary>
    protected BaseEntity() => Id = Guid.NewGuid();
}
