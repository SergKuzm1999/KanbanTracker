using KanbanTracker.Domain.Entities;
using KanbanTracker.Domain.Enums;

namespace KanbanTracker.Domain.Patterns.Observer;

/// <summary>
/// Observer: спостерігачі за змінами статусів.
/// </summary>
public interface ITaskObserver
{
    void OnTaskStatusChanged(Guid taskId, KanbanTaskStatus newStatus);
}

public class ConsoleTaskLogger : ITaskObserver
{
    public void OnTaskStatusChanged(Guid taskId, KanbanTaskStatus newStatus)
    {
        // У реальному UI буде лог / toast
        System.Diagnostics.Debug.WriteLine($"[LOG] Task {taskId} → {newStatus} at {DateTime.Now:HH:mm:ss}");
    }
}

public class TaskStatisticsObserver : ITaskObserver
{
    public int TotalTransitions { get; private set; }
    public Dictionary<KanbanTaskStatus, int> StatusCounts { get; } = new();

    public void OnTaskStatusChanged(Guid taskId, KanbanTaskStatus newStatus)
    {
        TotalTransitions++;
        if (!StatusCounts.ContainsKey(newStatus))
            StatusCounts[newStatus] = 0;
        StatusCounts[newStatus]++;
    }
}

/// <summary>
/// Subject, який тримає список спостерігачів.
/// </summary>
public class TaskEventPublisher
{
    private readonly List<ITaskObserver> _observers = new();

    public void Subscribe(ITaskObserver observer)
    {
        if (observer != null && !_observers.Contains(observer))
            _observers.Add(observer);
    }

    public void Unsubscribe(ITaskObserver observer) => _observers.Remove(observer);

    public void Notify(Guid taskId, KanbanTaskStatus newStatus)
    {
        foreach (var obs in _observers.ToList())
            obs.OnTaskStatusChanged(taskId, newStatus);
    }

    public void Clear() => _observers.Clear();
}
