using KanbanTracker.Domain.Entities;
using KanbanTracker.Domain.Enums;
using KanbanTracker.Domain.Exceptions;
using KanbanTracker.Domain.Interfaces;

namespace KanbanTracker.Domain.Patterns.State;

public class ToDoState : ITaskState
{
    public KanbanTaskStatus Status => KanbanTaskStatus.ToDo;
    public string DisplayName => "To Do";

    public void MoveNext(TaskItem task) => task.ChangeState(new InProgressState());
    public void MovePrevious(TaskItem task) =>
        throw new InvalidStatusTransitionException(Status, Status); // вже початковий

    public bool CanTransitionTo(KanbanTaskStatus target) =>
        target is KanbanTaskStatus.InProgress or KanbanTaskStatus.Blocked;
}

public class InProgressState : ITaskState
{
    public KanbanTaskStatus Status => KanbanTaskStatus.InProgress;
    public string DisplayName => "In Progress";

    public void MoveNext(TaskItem task) => task.ChangeState(new ReviewState());
    public void MovePrevious(TaskItem task) => task.ChangeState(new ToDoState());

    public bool CanTransitionTo(KanbanTaskStatus target) =>
        target is KanbanTaskStatus.Review or KanbanTaskStatus.ToDo or KanbanTaskStatus.Blocked;
}

public class ReviewState : ITaskState
{
    public KanbanTaskStatus Status => KanbanTaskStatus.Review;
    public string DisplayName => "Review";

    public void MoveNext(TaskItem task) => task.ChangeState(new DoneState());
    public void MovePrevious(TaskItem task) => task.ChangeState(new InProgressState());

    public bool CanTransitionTo(KanbanTaskStatus target) =>
        target is KanbanTaskStatus.Done or KanbanTaskStatus.InProgress or KanbanTaskStatus.Blocked;
}

public class DoneState : ITaskState
{
    public KanbanTaskStatus Status => KanbanTaskStatus.Done;
    public string DisplayName => "Done";

    public void MoveNext(TaskItem task) =>
        throw new InvalidStatusTransitionException(Status, Status);

    public void MovePrevious(TaskItem task) => task.ChangeState(new ReviewState());

    public bool CanTransitionTo(KanbanTaskStatus target) => target == KanbanTaskStatus.Review;
}

public class BlockedState : ITaskState
{
    public KanbanTaskStatus Status => KanbanTaskStatus.Blocked;
    public string DisplayName => "Blocked";

    public void MoveNext(TaskItem task) => task.ChangeState(new InProgressState());
    public void MovePrevious(TaskItem task) => task.ChangeState(new ToDoState());

    public bool CanTransitionTo(KanbanTaskStatus target) =>
        target is KanbanTaskStatus.InProgress or KanbanTaskStatus.ToDo;
}

/// <summary>
/// Фабрика станів (допоміжна).
/// </summary>
public static class TaskStateFactory
{
    public static ITaskState FromStatus(KanbanTaskStatus status) => status switch
    {
        KanbanTaskStatus.ToDo => new ToDoState(),
        KanbanTaskStatus.InProgress => new InProgressState(),
        KanbanTaskStatus.Review => new ReviewState(),
        KanbanTaskStatus.Done => new DoneState(),
        KanbanTaskStatus.Blocked => new BlockedState(),
        _ => new ToDoState()
    };
}
