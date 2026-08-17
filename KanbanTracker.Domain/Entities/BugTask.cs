using KanbanTracker.Domain.Enums;

namespace KanbanTracker.Domain.Entities;

/// <summary>
/// Наслідування: Bug-завдання.
/// Демонструє override (поліморфізм). Приховування через new зазвичай гірше.
/// </summary>
public class BugTask : TaskItem
{
    public string ReproductionSteps { get; set; } = string.Empty;
    public string Severity { get; set; } = "Normal";

    public BugTask(string title, Priority priority = Priority.High)
        : base(title, TaskType.Bug, priority)
    {
    }

    public BugTask(Guid id, string title, string description, Priority priority,
        KanbanTaskStatus status, DateTime createdAt, DateTime? updatedAt, Guid? assigneeId, Guid? epicId,
        string reproductionSteps, string severity)
        : base(id, title, description, TaskType.Bug, priority, status, createdAt, updatedAt, assigneeId, epicId)
    {
        ReproductionSteps = reproductionSteps;
        Severity = severity;
    }

    public override string GetDetailedInfo() =>
        base.GetDetailedInfo() + $"\nSeverity: {Severity}\nRepro: {ReproductionSteps}";

    public override double CalculateEffort() => base.CalculateEffort() * 0.7;
}
