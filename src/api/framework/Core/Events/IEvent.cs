using MediatR;

namespace FSH.Framework.Core.Events;

public interface IEvent : INotification
{
    string EventType { get; }
    DateTime OccurredOn { get; }
}
