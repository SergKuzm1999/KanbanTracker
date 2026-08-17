using KanbanTracker.Domain.Enums;
using KanbanTracker.Domain.Entities;

namespace KanbanTracker.Domain.Interfaces;

/// <summary>
/// State pattern для статусів завдань.
/// </summary>
public interface ITaskState
{
    KanbanTaskStatus Status { get; }
    string DisplayName { get; }

    void MoveNext(TaskItem task);
    void MovePrevious(TaskItem task);
    bool CanTransitionTo(KanbanTaskStatus target);
}
