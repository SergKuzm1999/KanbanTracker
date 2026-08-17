using System.Text.Json;
using KanbanTracker.Application.DTOs;
using KanbanTracker.Domain.Entities;
using KanbanTracker.Domain.Enums;
using KanbanTracker.Domain.Patterns.Facade;
using KanbanTracker.Domain.Patterns.Factory;
using KanbanTracker.Domain.Patterns.Strategy;

namespace KanbanTracker.Application.Services;

public class BoardService : IDisposable
{
    private readonly KanbanBoardFacade _board;
    private readonly string _dataFilePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BoardService(string? dataFilePath = null)
    {
        _board = new KanbanBoardFacade(SingletonTaskFactory.Instance);

        // Зберігаємо в папці проєкту (поточна робоча директорія)
        // При dotnet run це буде папка KanbanTracker\
        _dataFilePath = dataFilePath ?? Path.Combine(
            Directory.GetCurrentDirectory(),
            "board.json");
    }

    public KanbanBoardFacade Board => _board;
    public string DataFilePath => _dataFilePath;

    public TaskItem CreateTask(string title, TaskType type = TaskType.Feature,
        Priority priority = Priority.Medium, Guid? assigneeId = null, Guid? epicId = null)
        => _board.CreateTask(type, title, priority, assigneeId, epicId);

    public Epic CreateEpic(string title, string description = "")
        => _board.CreateEpic(title, description);

    public User RegisterUser(string name, string email)
        => _board.RegisterUser(name, email);

    public void MoveNext(Guid taskId) => _board.MoveTaskNext(taskId);
    public void MovePrevious(Guid taskId) => _board.MoveTaskPrevious(taskId);
    public void ChangeStatus(Guid taskId, KanbanTaskStatus status) => _board.ChangeTaskStatus(taskId, status);

    public IEnumerable<TaskItem> GetTasksByStatus(KanbanTaskStatus status) => _board.GetTasksByStatus(status);
    public IEnumerable<TaskItem> GetAllTasks() => _board.Tasks;
    public IEnumerable<Epic> GetAllEpics() => _board.Epics;
    public IEnumerable<User> GetAllUsers() => _board.Users;

    public IEnumerable<TaskItem> GetSorted(ITaskSortStrategy strategy)
        => _board.GetSortedTasks(strategy);

    public Dictionary<KanbanTaskStatus, int> GetStatistics() => _board.GetStatusStatistics();

    public IEnumerable<TaskItem> SearchTasks(string query) =>
        _board.Tasks.Where(t =>
            t.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            t.Description.Contains(query, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<IGrouping<Priority, TaskItem>> GroupByPriority() =>
        _board.Tasks.GroupBy(t => t.Priority).OrderByDescending(g => g.Key);

    public double TotalEffort() => _board.Tasks.Sum(t => t.CalculateEffort());

    /// <summary>
    /// Зберегти стан дошки у board.json (у папці проєкту)
    /// </summary>
    public void Save()
    {
        var snapshot = new BoardSnapshotDto
        {
            Tasks = _board.Tasks.Select(TaskDto.FromEntity).ToList(),
            Epics = _board.Epics.Select(e => new EpicDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                Priority = e.Priority.ToString(),
                CreatedAt = e.CreatedAt,
                TaskIds = e.GetChildren().Select(c => c.Id).ToList()
            }).ToList(),
            Users = _board.Users.Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                CreatedAt = u.CreatedAt
            }).ToList(),
            SavedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }

    /// <summary>
    /// Завантажити стан з board.json. Повертає true, якщо файл існував і дані завантажені.
    /// </summary>
    public bool Load()
    {
        if (!File.Exists(_dataFilePath))
            return false;

        var json = File.ReadAllText(_dataFilePath);
        var snapshot = JsonSerializer.Deserialize<BoardSnapshotDto>(json, JsonOptions);
        if (snapshot is null || snapshot.Tasks.Count == 0)
            return false;

        _board.Clear();

        // Users
        foreach (var u in snapshot.Users)
        {
            var user = new User(u.Id, u.Name, u.Email, u.CreatedAt);
            // Додаємо через фасад — RegisterUser створює новий Id, тому додаємо вручну через reflection-free спосіб:
            // Використовуємо Create і потім не потрібно — для простоти реєструємо заново
            _board.RegisterUser(u.Name, u.Email);
        }

        // Epics
        var epicMap = new Dictionary<Guid, Epic>();
        foreach (var ed in snapshot.Epics)
        {
            var epic = _board.CreateEpic(ed.Title, ed.Description);
            epicMap[ed.Id] = epic;
        }

        // Tasks
        foreach (var td in snapshot.Tasks)
        {
            var task = td.ToEntity();
            // Додаємо задачу через фасад (новий Id), але з потрібним статусом/описом
            var created = _board.CreateTask(task.Type, task.Title, task.Priority);
            created.Description = task.Description;

            // Відновлюємо статус крок за кроком
            try
            {
                while (created.Status != task.Status)
                {
                    if (task.Status == KanbanTaskStatus.Blocked)
                    {
                        created.TransitionTo(KanbanTaskStatus.Blocked);
                        break;
                    }
                    created.MoveNext();
                    // захист від нескінченного циклу
                    if (created.Status == KanbanTaskStatus.Done) break;
                }
            }
            catch
            {
                // якщо не вдалося точно відновити — залишаємо як є
            }
        }

        return true;
    }

    public void SeedDemoData()
    {
        var alice = RegisterUser("Alice Developer", "alice@company.com");
        var bob = RegisterUser("Bob Tester", "bob@company.com");
        var carol = RegisterUser("Carol PM", "carol@company.com");

        var epic1 = CreateEpic("User Authentication", "Реалізація системи входу");
        var epic2 = CreateEpic("Kanban Core", "Основна логіка дошки");

        var t1 = CreateTask("Implement login form", TaskType.Feature, Priority.High, alice.Id, epic1.Id);
        t1.Description = "Create Avalonia login window";
        if (t1 is FeatureTask ft1) ft1.AcceptanceCriteria = "User can enter email/password and submit";

        var t2 = CreateTask("Fix password validation", TaskType.Bug, Priority.Critical, bob.Id, epic1.Id);
        t2.Description = "Empty password should show error";
        if (t2 is BugTask bt) { bt.ReproductionSteps = "Enter empty password"; bt.Severity = "High"; }

        var t3 = CreateTask("As a user I want to see my tasks", TaskType.Story, Priority.Medium, alice.Id, epic2.Id);
        t3.Description = "User story for task list";
        if (t3 is StoryTask st) st.StoryPoints = 5;

        var t4 = CreateTask("Add drag-and-drop support", TaskType.Feature, Priority.Medium, alice.Id, epic2.Id);
        t4.Description = "Optional future feature";

        var t5 = CreateTask("Write unit tests for State pattern", TaskType.Technical, Priority.Low, bob.Id);
        t5.Description = "Cover all status transitions";

        var sub1 = CreateTask("Design UI mockup", TaskType.Technical, Priority.Medium, carol.Id);
        sub1.Description = "Figma mockup for login";
        t1.Add(sub1);

        // Дозволені переходи State
        t2.MoveNext(); // ToDo → InProgress

        t3.MoveNext(); // ToDo → InProgress
        t3.MoveNext(); // InProgress → Review

        t5.MoveNext(); // ToDo → InProgress
        t5.MoveNext(); // InProgress → Review
        t5.MoveNext(); // Review → Done
    }

    public void Dispose()
    {
        _board.Clear();
    }
}