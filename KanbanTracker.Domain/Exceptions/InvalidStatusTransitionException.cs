using KanbanTracker.Domain.Enums;

namespace KanbanTracker.Domain.Exceptions;

public class InvalidStatusTransitionException : DomainException
{
    public KanbanTaskStatus From { get; }
    public KanbanTaskStatus To { get; }

    public InvalidStatusTransitionException(KanbanTaskStatus from, KanbanTaskStatus to)
        : base($"Cannot transition from {from} to {to}.")
    {
        From = from;
        To = to;
    }
}
