using KanbanTracker.Domain.Entities;
using KanbanTracker.Domain.Enums;

namespace KanbanTracker.Domain.Patterns.Factory;

/// <summary>
/// Factory Method / Abstract Factory для створення різних типів завдань.
/// </summary>
public interface ITaskFactory
{
    TaskItem Create(TaskType type, string title, Priority priority = Priority.Medium);
    TaskItem CreateFromConfig(string typeName, string title, Dictionary<string, string>? extra = null);
}

public class TaskFactory : ITaskFactory
{
    public TaskItem Create(TaskType type, string title, Priority priority = Priority.Medium)
    {
        return type switch
        {
            TaskType.Feature => new FeatureTask(title, priority),
            TaskType.Bug => new BugTask(title, priority),
            TaskType.Story => new StoryTask(title, priority: priority),
            TaskType.Technical => new TaskItem(title, TaskType.Technical, priority),
            _ => new TaskItem(title, type, priority)
        };
    }

    public TaskItem CreateFromConfig(string typeName, string title, Dictionary<string, string>? extra = null)
    {
        if (!Enum.TryParse<TaskType>(typeName, true, out var type))
            type = TaskType.Feature;

        var priority = Priority.Medium;
        if (extra != null && extra.TryGetValue("priority", out var p) &&
            Enum.TryParse<Priority>(p, true, out var parsed))
            priority = parsed;

        var task = Create(type, title, priority);

        if (extra != null)
        {
            if (extra.TryGetValue("description", out var desc))
                task.Description = desc;

            if (task is FeatureTask ft && extra.TryGetValue("acceptance", out var acc))
                ft.AcceptanceCriteria = acc;

            if (task is BugTask bt)
            {
                if (extra.TryGetValue("repro", out var repro)) bt.ReproductionSteps = repro;
                if (extra.TryGetValue("severity", out var sev)) bt.Severity = sev;
            }

            if (task is StoryTask st && extra.TryGetValue("points", out var pts) &&
                int.TryParse(pts, out var points))
                st.StoryPoints = points;
        }

        return task;
    }
}

/// <summary>
/// Singleton-варіант фабрики (демонстрація Singleton).
/// </summary>
public sealed class SingletonTaskFactory : ITaskFactory
{
    private static readonly Lazy<SingletonTaskFactory> _instance =
        new(() => new SingletonTaskFactory());

    public static SingletonTaskFactory Instance => _instance.Value;

    private readonly TaskFactory _inner = new();

    private SingletonTaskFactory() { }

    public TaskItem Create(TaskType type, string title, Priority priority = Priority.Medium)
        => _inner.Create(type, title, priority);

    public TaskItem CreateFromConfig(string typeName, string title, Dictionary<string, string>? extra = null)
        => _inner.CreateFromConfig(typeName, title, extra);
}
