using KanbanTracker.Domain.Entities;
using KanbanTracker.Domain.Enums;
using KanbanTracker.Domain.Interfaces;
using KanbanTracker.Domain.Patterns.Factory;
using KanbanTracker.Domain.Patterns.Observer;
using KanbanTracker.Domain.Patterns.Strategy;

namespace KanbanTracker.Domain.Patterns.Facade;

/// <summary>
/// Facade: єдиний простий вхід до складної підсистеми Канбан-дошки.
/// </summary>
public class KanbanBoardFacade
{
    private readonly ITaskFactory _factory;
    private readonly List<TaskItem> _tasks = new();
    private readonly List<Epic> _epics = new();
    private readonly List<User> _users = new();
    private readonly TaskEventPublisher _publisher = new();
    private readonly TaskSorter _sorter;

    public KanbanBoardFacade(ITaskFactory? factory = null)
    {
        _factory = factory ?? SingletonTaskFactory.Instance;
        _sorter = new TaskSorter(new PrioritySortStrategy());
        _publisher.Subscribe(new ConsoleTaskLogger());
    }

    public IReadOnlyList<TaskItem> Tasks => _tasks.AsReadOnly();
    public IReadOnlyList<Epic> Epics => _epics.AsReadOnly();
    public IReadOnlyList<User> Users => _users.AsReadOnly();
    public TaskEventPublisher EventPublisher => _publisher;

    public User RegisterUser(string name, string email)
    {
        var user = new User(name, email);
        _users.Add(user);
        return user;
    }

    public TaskItem CreateTask(TaskType type, string title, Priority priority = Priority.Medium,
        Guid? assigneeId = null, Guid? epicId = null)
    {
        var task = _factory.Create(type, title, priority);
        task.AssigneeId = assigneeId;
        task.EpicId = epicId;

        task.StatusChanged += (s, e) => _publisher.Notify(e.TaskId, e.NewStatus);

        _tasks.Add(task);

        if (epicId.HasValue)
        {
            var epic = _epics.FirstOrDefault(e => e.Id == epicId.Value);
            epic?.Add(task);
        }

        return task;
    }

    public Epic CreateEpic(string title, string description = "")
    {
        var epic = new Epic(title) { Description = description };
        _epics.Add(epic);
        return epic;
    }

    public void MoveTaskNext(Guid taskId)
    {
        var task = GetTask(taskId);
        task.MoveNext();
    }

    public void MoveTaskPrevious(Guid taskId)
    {
        var task = GetTask(taskId);
        task.MovePrevious();
    }

    public void ChangeTaskStatus(Guid taskId, KanbanTaskStatus status)
    {
        var task = GetTask(taskId);
        task.TransitionTo(status);
    }

    public void AssignTask(Guid taskId, Guid userId)
    {
        var task = GetTask(taskId);
        if (!_users.Any(u => u.Id == userId))
            throw new InvalidOperationException("User not found.");
        task.AssigneeId = userId;
    }

    public IEnumerable<TaskItem> GetTasksByStatus(KanbanTaskStatus status) =>
        _tasks.Where(t => t.Status == status);

    public IEnumerable<TaskItem> GetSortedTasks(ITaskSortStrategy? strategy = null)
    {
        if (strategy != null) _sorter.SetStrategy(strategy);
        return _sorter.Sort(_tasks);
    }

    public TaskItem GetTask(Guid id) =>
        _tasks.FirstOrDefault(t => t.Id == id)
        ?? throw new Exceptions.TaskNotFoundException(id);

    public void RemoveTask(Guid id)
    {
        var task = GetTask(id);
        _tasks.Remove(task);
        foreach (var epic in _epics)
            epic.Remove(task);
        task.Dispose();
    }

    public void Clear()
    {
        foreach (var t in _tasks) t.Dispose();
        foreach (var e in _epics) e.Dispose();
        _tasks.Clear();
        _epics.Clear();
        _users.Clear();
        _publisher.Clear();
    }

    // Статистика через LINQ
    public Dictionary<KanbanTaskStatus, int> GetStatusStatistics() =>
        _tasks.GroupBy(t => t.Status)
              .ToDictionary(g => g.Key, g => g.Count());

    public double GetAverageEffort() =>
        _tasks.Count == 0 ? 0 : _tasks.Average(t => t.CalculateEffort());
}
