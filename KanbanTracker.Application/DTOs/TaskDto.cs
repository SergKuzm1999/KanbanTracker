using KanbanTracker.Domain.Entities;
using KanbanTracker.Domain.Enums;
using PriorityEnum = KanbanTracker.Domain.Enums.Priority;

namespace KanbanTracker.Application.DTOs;

/// <summary>
/// DTO для безпечної серіалізації (розділення Domain і DTO).
/// </summary>
public class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Feature";
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "ToDo";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? AssigneeId { get; set; }
    public Guid? EpicId { get; set; }
    public List<TaskDto> Subtasks { get; set; } = new();

    // Додаткові поля для підтипів
    public string? AcceptanceCriteria { get; set; }
    public string? ReproductionSteps { get; set; }
    public string? Severity { get; set; }
    public int? StoryPoints { get; set; }

    public static TaskDto FromEntity(TaskItem task)
    {
        var dto = new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Type = task.Type.ToString(),
            Priority = task.Priority.ToString(),
            Status = task.Status.ToString(),
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            AssigneeId = task.AssigneeId,
            EpicId = task.EpicId,
            Subtasks = task.GetChildren()
                .OfType<TaskItem>()
                .Select(FromEntity)
                .ToList()
        };

        if (task is FeatureTask ft) dto.AcceptanceCriteria = ft.AcceptanceCriteria;
        if (task is BugTask bt)
        {
            dto.ReproductionSteps = bt.ReproductionSteps;
            dto.Severity = bt.Severity;
        }
        if (task is StoryTask st) dto.StoryPoints = st.StoryPoints;

        return dto;
    }

    public TaskItem ToEntity()
    {
        if (!Enum.TryParse<TaskType>(Type, true, out var type)) type = TaskType.Feature;
        if (!Enum.TryParse<PriorityEnum>(Priority, true, out var priority)) priority = PriorityEnum.Medium;
        if (!Enum.TryParse<KanbanTaskStatus>(Status, true, out var status)) status = KanbanTaskStatus.ToDo;

        TaskItem task = type switch
        {
            TaskType.Feature => new FeatureTask(Id, Title, Description, priority, status,
                CreatedAt, UpdatedAt, AssigneeId, EpicId, AcceptanceCriteria ?? ""),
            TaskType.Bug => new BugTask(Id, Title, Description, priority, status,
                CreatedAt, UpdatedAt, AssigneeId, EpicId, ReproductionSteps ?? "", Severity ?? "Normal"),
            TaskType.Story => new StoryTask(Id, Title, Description, priority, status,
                CreatedAt, UpdatedAt, AssigneeId, EpicId, StoryPoints ?? 3),
            _ => new TaskItem(Id, Title, Description, type, priority, status,
                CreatedAt, UpdatedAt, AssigneeId, EpicId)
        };

        foreach (var sub in Subtasks)
            task.Add(sub.ToEntity());

        return task;
    }
}

public class EpicDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public DateTime CreatedAt { get; set; }
    public List<Guid> TaskIds { get; set; } = new();
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class BoardSnapshotDto
{
    public List<TaskDto> Tasks { get; set; } = new();
    public List<EpicDto> Epics { get; set; } = new();
    public List<UserDto> Users { get; set; } = new();
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
