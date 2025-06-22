using System.Collections.ObjectModel;
using FSH.Framework.Core.Domain.Events;

namespace FSH.Framework.Core.Domain.Contracts;

/// <summary>
/// Defines the base interface that all domain entities must implement.
/// This interface provides common functionality for all domain entities.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets the collection of domain events raised by this entity.
    /// These events will be dispatched when the entity is saved to the database.
    /// </summary>
    Collection<DomainEvent> DomainEvents { get; }
}

/// <summary>
/// Defines an interface for entities with an ID of a specific type.
/// </summary>
/// <typeparam name="TId">The type of the entity's ID.</typeparam>
public interface IEntity<out TId> : IEntity
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    TId Id { get; }
}
