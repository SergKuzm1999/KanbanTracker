using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KanbanTracker.Application.Services;
using KanbanTracker.Domain.Entities;
using KanbanTracker.Domain.Enums;
using KanbanTracker.Domain.Patterns.Strategy;

namespace KanbanTracker.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly BoardService _service;

    [ObservableProperty] private string _newTaskTitle = string.Empty;
    [ObservableProperty] private string _newTaskDescription = string.Empty;
    [ObservableProperty] private TaskType _selectedTaskType = TaskType.Feature;
    [ObservableProperty] private Priority _selectedPriority = Priority.Medium;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string _selectedSort = "Priority";

    public ObservableCollection<TaskItemViewModel> ToDoTasks { get; } = new();
    public ObservableCollection<TaskItemViewModel> InProgressTasks { get; } = new();
    public ObservableCollection<TaskItemViewModel> ReviewTasks { get; } = new();
    public ObservableCollection<TaskItemViewModel> DoneTasks { get; } = new();
    public ObservableCollection<TaskItemViewModel> BlockedTasks { get; } = new();

    public Array TaskTypes => Enum.GetValues(typeof(TaskType));
    public Array Priorities => Enum.GetValues(typeof(Priority));
    public string[] SortOptions { get; } = { "Priority", "Created Date", "Title", "Status" };

    public MainViewModel()
    {
        _service = new BoardService();

        // Спочатку пробуємо завантажити з board.json у папці проєкту
        if (_service.Load())
        {
            RefreshBoard();
            StatusMessage = $"Loaded from: {_service.DataFilePath}";
        }
        else
        {
            _service.SeedDemoData();
            RefreshBoard();
            StatusMessage = "Demo data loaded (no board.json found).";
        }
    }

    [RelayCommand]
    private void AddTask()
    {
        if (string.IsNullOrWhiteSpace(NewTaskTitle))
        {
            StatusMessage = "Title cannot be empty.";
            return;
        }

        try
        {
            var task = _service.CreateTask(NewTaskTitle.Trim(), SelectedTaskType, SelectedPriority);
            task.Description = NewTaskDescription?.Trim() ?? string.Empty;

            NewTaskTitle = string.Empty;
            NewTaskDescription = string.Empty;
            RefreshBoard();
            StatusMessage = $"Created: {task.Title}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    public void MoveTaskNext(Guid id)
    {
        try
        {
            _service.MoveNext(id);
            RefreshBoard();
            StatusMessage = "Moved forward.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Cannot move: {ex.Message}";
        }
    }

    public void MoveTaskPrevious(Guid id)
    {
        try
        {
            _service.MovePrevious(id);
            RefreshBoard();
            StatusMessage = "Moved back.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Cannot move: {ex.Message}";
        }
    }

    public void DeleteTaskById(Guid id)
    {
        try
        {
            _service.Board.RemoveTask(id);
            RefreshBoard();
            StatusMessage = "Task deleted.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplySort()
    {
        ITaskSortStrategy strategy = SelectedSort switch
        {
            "Created Date" => new CreatedDateSortStrategy(),
            "Title" => new TitleSortStrategy(),
            "Status" => new StatusSortStrategy(),
            _ => new PrioritySortStrategy()
        };
        RefreshBoard(strategy);
        StatusMessage = $"Sorted by: {strategy.Name}";
    }

    [RelayCommand]
    private void Search()
    {
        RefreshBoard();
        if (string.IsNullOrWhiteSpace(SearchQuery))
            StatusMessage = "Showing all tasks.";
        else
            StatusMessage = $"Search results for: '{SearchQuery}'";
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        RefreshBoard();
        StatusMessage = "Search cleared. Showing all tasks.";
    }

    [RelayCommand]
    private void SaveBoard()
    {
        try
        {
            _service.Save();
            StatusMessage = $"Saved to: {_service.DataFilePath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        RefreshBoard();
        StatusMessage = "Board refreshed.";
    }

    private void RefreshBoard(ITaskSortStrategy? strategy = null)
    {
        ToDoTasks.Clear();
        InProgressTasks.Clear();
        ReviewTasks.Clear();
        DoneTasks.Clear();
        BlockedTasks.Clear();

        IEnumerable<TaskItem> tasks = strategy != null
            ? _service.GetSorted(strategy)
            : _service.GetAllTasks();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.Trim();
            tasks = tasks.Where(t =>
                t.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                t.Type.ToString().Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var t in tasks)
        {
            var vm = new TaskItemViewModel(t, this);

            switch (t.Status)
            {
                case KanbanTaskStatus.ToDo: ToDoTasks.Add(vm); break;
                case KanbanTaskStatus.InProgress: InProgressTasks.Add(vm); break;
                case KanbanTaskStatus.Review: ReviewTasks.Add(vm); break;
                case KanbanTaskStatus.Done: DoneTasks.Add(vm); break;
                case KanbanTaskStatus.Blocked: BlockedTasks.Add(vm); break;
            }
        }
    }

    public string StatsText
    {
        get
        {
            var stats = _service.GetStatistics();
            var total = stats.Values.Sum();
            var effort = _service.TotalEffort();
            return $"Total: {total} | Effort: {effort:F1} | " +
                   string.Join(" | ", stats.Select(kv => $"{kv.Key}: {kv.Value}"));
        }
    }
}

public partial class TaskItemViewModel : ObservableObject
{
    private readonly MainViewModel _parent;

    public Guid Id { get; }
    public string Title { get; }
    public string Type { get; }
    public string Priority { get; }
    public string Status { get; }
    public string Description { get; }
    public string CreatedAt { get; }

    public string PriorityColor => Priority switch
    {
        "Critical" => "#E53935",
        "High" => "#FB8C00",
        "Medium" => "#1E88E5",
        "Low" => "#43A047",
        _ => "#757575"
    };

    public TaskItemViewModel(TaskItem task, MainViewModel parent)
    {
        _parent = parent;
        Id = task.Id;
        Title = task.Title;
        Type = task.Type.ToString();
        Priority = task.Priority.ToString();
        Status = task.StatusDisplayName;
        Description = string.IsNullOrWhiteSpace(task.Description) ? "(no description)" : task.Description;
        CreatedAt = task.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
    }

    [RelayCommand]
    private void MoveNext() => _parent.MoveTaskNext(Id);

    [RelayCommand]
    private void MovePrevious() => _parent.MoveTaskPrevious(Id);

    [RelayCommand]
    private void Delete() => _parent.DeleteTaskById(Id);
}