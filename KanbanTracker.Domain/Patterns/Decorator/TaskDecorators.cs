using KanbanTracker.Domain.Entities;
using KanbanTracker.Domain.Enums;
using KanbanTracker.Domain.Interfaces;

namespace KanbanTracker.Domain.Patterns.Decorator;

/// <summary>
/// Decorator: динамічне розширення поведінки завдання.
/// </summary>
public abstract class TaskDecorator : ITaskComponent
{
    protected readonly ITaskComponent _inner;

    protected TaskDecorator(ITaskComponent inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public Guid Id => _inner.Id;
    public virtual string Title => _inner.Title;
    public virtual KanbanTaskStatus Status => _inner.Status;
    public virtual Priority Priority => _inner.Priority;
    public bool IsLeaf => _inner.IsLeaf;

    public virtual void Add(ITaskComponent component) => _inner.Add(component);
    public virtual void Remove(ITaskComponent component) => _inner.Remove(component);
    public virtual IReadOnlyList<ITaskComponent> GetChildren() => _inner.GetChildren();
    public virtual int CountCompleted() => _inner.CountCompleted();
    public virtual int CountTotal() => _inner.CountTotal();
}

/// <summary>
/// Декоратор, що додає логування змін.
/// </summary>
public class LoggingTaskDecorator : TaskDecorator
{
    private readonly List<string> _log = new();

    public LoggingTaskDecorator(ITaskComponent inner) : base(inner) { }

    public IReadOnlyList<string> Log => _log.AsReadOnly();

    public override void Add(ITaskComponent component)
    {
        _log.Add($"[{DateTime.Now:HH:mm:ss}] Added child {component.Title}");
        base.Add(component);
    }

    public override void Remove(ITaskComponent component)
    {
        _log.Add($"[{DateTime.Now:HH:mm:ss}] Removed child {component.Title}");
        base.Remove(component);
    }
}

/// <summary>
/// Декоратор, що підвищує пріоритет (urgent).
/// </summary>
public class UrgentTaskDecorator : TaskDecorator
{
    public UrgentTaskDecorator(ITaskComponent inner) : base(inner) { }

    public override Priority Priority =>
        _inner.Priority == Priority.Critical ? Priority.Critical : (Priority)((int)_inner.Priority + 1);

    public override string Title => $"[URGENT] {_inner.Title}";
}
