namespace KanbanTracker.Domain.Enums;

/// <summary>
/// Статуси завдань для Канбан-дошки.
/// </summary>
public enum KanbanTaskStatus
{
    ToDo = 0,
    InProgress = 1,
    Review = 2,
    Done = 3,
    Blocked = 4
}
