namespace FSH.Framework.Core.Domain.Events;

/// <summary>
/// Marker interface for domain events in the domain-driven design pattern.
/// Domain events are used to capture side effects that occur within the domain model.
/// </summary>
/// <remarks>
/// Implement this interface to create domain events that can be published and handled
/// by domain event handlers. Domain events are typically raised by aggregate roots
/// and processed after the transaction is committed to ensure consistency.
/// </remarks>
public interface IDomainEvent
{
}
