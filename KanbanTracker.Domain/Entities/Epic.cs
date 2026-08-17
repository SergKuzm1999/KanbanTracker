using KanbanTracker.Domain.Enums;
using KanbanTracker.Domain.Exceptions;
using KanbanTracker.Domain.Interfaces;

namespace KanbanTracker.Domain.Entities;

/// <summary>
/// Epic — контейнер для завдань (Composite + агрегація).
/// </summary>
public class Epic : ITaskComponent, IDisposable
{
    private string _title = string.Empty;
    private readonly List<ITaskComponent> _tasks = new();
    private bool _disposed;

    public Guid Id { get; private set; }
    public string Title
    {
        get => _title;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ValidationException(nameof(Title), "Epic title cannot be empty.");
            _title = value.Trim();
        }
    }

    public string Description { get; set; } = string.Empty;
    public KanbanTaskStatus Status { get; private set; } = KanbanTaskStatus.ToDo;
    public Priority Priority { get; set; } = Priority.Medium;
    public DateTime CreatedAt { get; private set; }
    public bool IsLeaf => false;

    // Індексатор
    public ITaskComponent this[int index] => _tasks[index];
    public ITaskComponent this[Guid id] =>
        _tasks.FirstOrDefault(t => t.Id == id)
        ?? throw new TaskNotFoundException(id);

    public int TaskCount => _tasks.Count;

    public Epic(string title)
    {
        Id = Guid.NewGuid();
        Title = title;
        CreatedAt = DateTime.UtcNow;
    }

    public Epic(Guid id, string title, string description, Priority priority, DateTime createdAt)
    {
        Id = id;
        Title = title;
        Description = description;
        Priority = priority;
        CreatedAt = createdAt;
    }

    public void Add(ITaskComponent component)
    {
        if (component is null) throw new ArgumentNullException(nameof(component));
        if (component is TaskItem ti) ti.EpicId = Id;
        _tasks.Add(component);
        RecalculateStatus();
    }

    public void Remove(ITaskComponent component)
    {
        _tasks.Remove(component);
        if (component is TaskItem ti) ti.EpicId = null;
        RecalculateStatus();
    }

    public IReadOnlyList<ITaskComponent> GetChildren() => _tasks.AsReadOnly();

    public int CountCompleted() => _tasks.Sum(t => t.CountCompleted());
    public int CountTotal() => _tasks.Sum(t => t.CountTotal());

    private void RecalculateStatus()
    {
        if (_tasks.Count == 0)
        {
            Status = KanbanTaskStatus.ToDo;
            return;
        }
        var allDone = _tasks.All(t => t.Status == KanbanTaskStatus.Done);
        var anyInProgress = _tasks.Any(t => t.Status is KanbanTaskStatus.InProgress or KanbanTaskStatus.Review);
        Status = allDone ? KanbanTaskStatus.Done : anyInProgress ? KanbanTaskStatus.InProgress : KanbanTaskStatus.ToDo;
    }

    // Оператор додавання Epic + Task
    public static Epic operator +(Epic epic, TaskItem task)
    {
        epic.Add(task);
        return epic;
    }

    public override string ToString() =>
        $"Epic: {Title} [{Status}] ({CountCompleted()}/{CountTotal()} completed)";

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var t in _tasks.OfType<IDisposable>())
            t.Dispose();
        _tasks.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
