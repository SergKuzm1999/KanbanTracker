using KanbanTracker.Domain.Enums;

namespace KanbanTracker.Domain.Entities;

/// <summary>
/// Наслідування: Feature-завдання.
/// Демонструє override vs new.
/// </summary>
public class FeatureTask : TaskItem
{
    public string AcceptanceCriteria { get; set; } = string.Empty;

    public FeatureTask(string title, Priority priority = Priority.Medium)
        : base(title, TaskType.Feature, priority)
    {
    }

    public FeatureTask(Guid id, string title, string description, Priority priority,
        KanbanTaskStatus status, DateTime createdAt, DateTime? updatedAt, Guid? assigneeId, Guid? epicId,
        string acceptanceCriteria)
        : base(id, title, description, TaskType.Feature, priority, status, createdAt, updatedAt, assigneeId, epicId)
    {
        AcceptanceCriteria = acceptanceCriteria;
    }

    public override string GetDetailedInfo() =>
        base.GetDetailedInfo() + $"\nAcceptance Criteria: {AcceptanceCriteria}";

    public override double CalculateEffort() => base.CalculateEffort() * 1.5; // фічі зазвичай більші
}
