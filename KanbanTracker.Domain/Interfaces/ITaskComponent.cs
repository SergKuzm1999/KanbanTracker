using KanbanTracker.Domain.Enums;

namespace KanbanTracker.Domain.Interfaces;

/// <summary>
/// Composite pattern: єдиний інтерфейс для Task та груп підзадач.
/// </summary>
public interface ITaskComponent : IEntity
{
    string Title { get; }
    KanbanTaskStatus Status { get; }
    Priority Priority { get; }

    void Add(ITaskComponent component);
    void Remove(ITaskComponent component);
    IReadOnlyList<ITaskComponent> GetChildren();
    bool IsLeaf { get; }

    /// <summary>
    /// Рекурсивний підрахунок завершених підзадач.
    /// </summary>
    int CountCompleted();
    int CountTotal();
}
