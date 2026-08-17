using KanbanTracker.Domain.Enums;
using KanbanTracker.Domain.Exceptions;
using KanbanTracker.Domain.Interfaces;
using KanbanTracker.Domain.Patterns.State;

namespace KanbanTracker.Domain.Entities;

/// <summary>
/// Основна сутність завдання.
/// Реалізує Composite (підзадачі), State (статуси), інкапсуляцію, валідацію.
/// Підтримує наслідування (FeatureTask, BugTask тощо).
/// </summary>
public class TaskItem : ITaskComponent, IDisposable
{
    private string _title = string.Empty;
    private string _description = string.Empty;
    private Priority _priority = Priority.Medium;
    private readonly List<ITaskComponent> _children = new();
    private ITaskState _state;
    private bool _disposed;

    public Guid Id { get; protected set; }
    public TaskType Type { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
    public Guid? AssigneeId { get; set; }
    public Guid? EpicId { get; set; }

    public string Title
    {
        get => _title;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ValidationException(nameof(Title), "Title cannot be empty.");
            if (value.Length > 200)
                throw new ValidationException(nameof(Title), "Title max length is 200.");
            _title = value.Trim();
            Touch();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            _description = value?.Trim() ?? string.Empty;
            Touch();
        }
    }

    public Priority Priority
    {
        get => _priority;
        set
        {
            if (!Enum.IsDefined(typeof(Priority), value))
                throw new ValidationException(nameof(Priority), "Invalid priority value.");
            _priority = value;
            Touch();
        }
    }

    public KanbanTaskStatus Status => _state.Status;
    public string StatusDisplayName => _state.DisplayName;
    public bool IsLeaf => _children.Count == 0;

    // Індексатор для доступу до підзадач
    public ITaskComponent this[int index]
    {
        get
        {
            if (index < 0 || index >= _children.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _children[index];
        }
    }

    public int SubtaskCount => _children.Count;

    // Основний конструктор
    public TaskItem(string title, TaskType type = TaskType.Feature, Priority priority = Priority.Medium)
    {
        Id = Guid.NewGuid();
        Title = title;
        Type = type;
        Priority = priority;
        CreatedAt = DateTime.UtcNow;
        _state = new ToDoState();
    }

    // Конструктор для відновлення (серіалізація)
    public TaskItem(Guid id, string title, string description, TaskType type, Priority priority,
        KanbanTaskStatus status, DateTime createdAt, DateTime? updatedAt, Guid? assigneeId, Guid? epicId)
    {
        Id = id;
        Title = title;
        Description = description;
        Type = type;
        Priority = priority;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        AssigneeId = assigneeId;
        EpicId = epicId;
        _state = TaskStateFactory.FromStatus(status);
    }

    // Копіювальний конструктор
    public TaskItem(TaskItem other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        Id = Guid.NewGuid();
        Title = other.Title;
        Description = other.Description;
        Type = other.Type;
        Priority = other.Priority;
        CreatedAt = DateTime.UtcNow;
        AssigneeId = other.AssigneeId;
        EpicId = other.EpicId;
        _state = TaskStateFactory.FromStatus(other.Status);
        foreach (var child in other._children)
        {
            if (child is TaskItem ti)
                _children.Add(new TaskItem(ti));
        }
    }

    /// <summary>
    /// Внутрішній метод для State pattern.
    /// </summary>
    internal void ChangeState(ITaskState newState)
    {
        _state = newState ?? throw new ArgumentNullException(nameof(newState));
        Touch();
        OnStatusChanged();
    }

    public void MoveNext() => _state.MoveNext(this);
    public void MovePrevious() => _state.MovePrevious(this);

    public void TransitionTo(KanbanTaskStatus target)
    {
        if (!_state.CanTransitionTo(target))
            throw new InvalidStatusTransitionException(_state.Status, target);
        _state = TaskStateFactory.FromStatus(target);
        Touch();
        OnStatusChanged();
    }

    // Composite
    public void Add(ITaskComponent component)
    {
        if (component is null) throw new ArgumentNullException(nameof(component));
        if (component.Id == Id) throw new DomainException("Cannot add task to itself.");
        _children.Add(component);
        Touch();
    }

    public void Remove(ITaskComponent component)
    {
        _children.Remove(component);
        Touch();
    }

    public IReadOnlyList<ITaskComponent> GetChildren() => _children.AsReadOnly();

    public int CountCompleted()
    {
        int count = Status == KanbanTaskStatus.Done ? 1 : 0;
        foreach (var child in _children)
            count += child.CountCompleted();
        return count;
    }

    public int CountTotal()
    {
        int count = 1;
        foreach (var child in _children)
            count += child.CountTotal();
        return count;
    }

    // Подія для Observer
    public event EventHandler<TaskStatusChangedEventArgs>? StatusChanged;

    protected virtual void OnStatusChanged()
    {
        StatusChanged?.Invoke(this, new TaskStatusChangedEventArgs(Id, Status));
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;

    // Перевантаження операторів
    public static TaskItem operator +(TaskItem left, TaskItem right)
    {
        // Об'єднує два завдання в одне (нова задача з об'єднаним описом)
        var combined = new TaskItem($"{left.Title} + {right.Title}", left.Type,
            left.Priority > right.Priority ? left.Priority : right.Priority)
        {
            Description = $"{left.Description}\n---\n{right.Description}"
        };
        return combined;
    }

    public static bool operator ==(TaskItem? left, TaskItem? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Id == right.Id;
    }

    public static bool operator !=(TaskItem? left, TaskItem? right) => !(left == right);

    public override bool Equals(object? obj) => obj is TaskItem t && t.Id == Id;
    public override int GetHashCode() => Id.GetHashCode();
    public override string ToString() => $"[{StatusDisplayName}] {Title} ({Type}, {Priority})";

    // Virtual методи для наслідування / поліморфізму
    public virtual string GetDetailedInfo() =>
        $"Task: {Title}\nType: {Type}\nStatus: {StatusDisplayName}\nPriority: {Priority}\nSubtasks: {SubtaskCount}";

    public virtual double CalculateEffort() => Priority switch
    {
        Priority.Low => 1.0,
        Priority.Medium => 2.0,
        Priority.High => 4.0,
        Priority.Critical => 8.0,
        _ => 2.0
    };

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var child in _children.OfType<IDisposable>())
            child.Dispose();
        _children.Clear();
        StatusChanged = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~TaskItem() => Dispose();
}

/// <summary>
/// Аргументи події зміни статусу (Observer).
/// </summary>
public class TaskStatusChangedEventArgs : EventArgs
{
    public Guid TaskId { get; }
    public KanbanTaskStatus NewStatus { get; }

    public TaskStatusChangedEventArgs(Guid taskId, KanbanTaskStatus newStatus)
    {
        TaskId = taskId;
        NewStatus = newStatus;
    }
}
