using KanbanTracker.Domain.Entities;
using KanbanTracker.Domain.Enums;

namespace KanbanTracker.Domain.Patterns.Strategy;

/// <summary>
/// Strategy pattern: різні алгоритми сортування завдань.
/// </summary>
public interface ITaskSortStrategy
{
    string Name { get; }
    IEnumerable<TaskItem> Sort(IEnumerable<TaskItem> tasks);
}

public class PrioritySortStrategy : ITaskSortStrategy
{
    public string Name => "By Priority (desc)";
    public IEnumerable<TaskItem> Sort(IEnumerable<TaskItem> tasks) =>
        tasks.OrderByDescending(t => t.Priority).ThenBy(t => t.CreatedAt);
}

public class CreatedDateSortStrategy : ITaskSortStrategy
{
    public string Name => "By Created Date";
    public IEnumerable<TaskItem> Sort(IEnumerable<TaskItem> tasks) =>
        tasks.OrderBy(t => t.CreatedAt);
}

public class TitleSortStrategy : ITaskSortStrategy
{
    public string Name => "By Title";
    public IEnumerable<TaskItem> Sort(IEnumerable<TaskItem> tasks) =>
        tasks.OrderBy(t => t.Title);
}

public class StatusSortStrategy : ITaskSortStrategy
{
    public string Name => "By Status";
    public IEnumerable<TaskItem> Sort(IEnumerable<TaskItem> tasks) =>
        tasks.OrderBy(t => t.Status).ThenByDescending(t => t.Priority);
}

/// <summary>
/// Контекст, що використовує стратегію.
/// </summary>
public class TaskSorter
{
    private ITaskSortStrategy _strategy;

    public TaskSorter(ITaskSortStrategy strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public void SetStrategy(ITaskSortStrategy strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public string CurrentStrategyName => _strategy.Name;

    public IEnumerable<TaskItem> Sort(IEnumerable<TaskItem> tasks) => _strategy.Sort(tasks);
}
