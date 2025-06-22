using MediatR;

namespace FSH.Framework.Core.Domain.Events;

/// <summary>
/// Base class for all domain events in the application.
/// Domain events represent things that happened in the domain that are of interest to other parts of the system.
/// </summary>
/// <remarks>
/// This class implements both <see cref="IDomainEvent"/> and MediatR's <see cref="INotification"/>,
/// allowing domain events to be published and handled using MediatR's in-process messaging.
/// </remarks>
public abstract record DomainEvent : IDomainEvent, INotification
{
    /// <summary>
    /// Gets or sets the UTC date and time when the event was raised.
    /// This is automatically set to the current UTC time when the event is created.
    /// </summary>
    public DateTime RaisedOn { get; protected set; } = DateTime.UtcNow;
}
