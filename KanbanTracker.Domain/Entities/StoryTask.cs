using KanbanTracker.Domain.Enums;

namespace KanbanTracker.Domain.Entities;

public class StoryTask : TaskItem
{
    public int StoryPoints { get; set; }

    public StoryTask(string title, int storyPoints = 3, Priority priority = Priority.Medium)
        : base(title, TaskType.Story, priority)
    {
        StoryPoints = storyPoints;
    }

    public StoryTask(Guid id, string title, string description, Priority priority,
        KanbanTaskStatus status, DateTime createdAt, DateTime? updatedAt, Guid? assigneeId, Guid? epicId,
        int storyPoints)
        : base(id, title, description, TaskType.Story, priority, status, createdAt, updatedAt, assigneeId, epicId)
    {
        StoryPoints = storyPoints;
    }

    public override string GetDetailedInfo() =>
        base.GetDetailedInfo() + $"\nStory Points: {StoryPoints}";

    public override double CalculateEffort() => StoryPoints * 1.0;
}
