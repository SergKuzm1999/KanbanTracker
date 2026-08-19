# Kanban Tracker — Навчальна практика з ООП (C# + Avalonia UI)

Мініпроєкт для проходження навчальної практики з об'єктно-орієнтованого програмування.

## Предметна область

**Трекер завдань (Канбан-дошка)**

| Сутності | Патерни (обов'язкові за темою) |
|----------|--------------------------------|
| Task, Epic, User | **State** (статуси), **Composite** (підзадачі), **Factory** (типи задач) |

Додатково реалізовано: Strategy, Observer, Decorator, Facade, Singleton.

---

## UML-діаграма класів (Domain)

```mermaid
classDiagram
    direction TB

    class IEntity {
        <<interface>>
        +Guid Id
    }

    class ITaskComponent {
        <<interface>>
        +string Title
        +KanbanTaskStatus Status
        +Priority Priority
        +bool IsLeaf
        +Add(component)
        +Remove(component)
        +GetChildren()
        +CountCompleted() int
        +CountTotal() int
    }

    class ITaskState {
        <<interface>>
        +KanbanTaskStatus Status
        +string DisplayName
        +MoveNext(task)
        +MovePrevious(task)
        +CanTransitionTo(target) bool
    }

    class ITaskFactory {
        <<interface>>
        +Create(type, title, priority) TaskItem
        +CreateFromConfig(typeName, title, extra) TaskItem
    }

    class IRepository~T~ {
        <<interface>>
        +GetById(id) T
        +GetAll() IEnumerable~T~
        +Add(entity)
        +Update(entity)
        +Remove(id)
        +Exists(id) bool
    }

    class User {
        -string _name
        -string _email
        +Guid Id
        +string Name
        +string Email
        +DateTime CreatedAt
        +User(name, email)
        +User(id, name, email, createdAt)
        +User(other)
        +Dispose()
    }

    class TaskItem {
        -string _title
        -string _description
        -Priority _priority
        -List~ITaskComponent~ _children
        -ITaskState _state
        +Guid Id
        +TaskType Type
        +DateTime CreatedAt
        +DateTime? UpdatedAt
        +Guid? AssigneeId
        +Guid? EpicId
        +string Title
        +string Description
        +Priority Priority
        +KanbanTaskStatus Status
        +MoveNext()
        +MovePrevious()
        +TransitionTo(status)
        +Add(component)
        +Remove(component)
        +GetChildren()
        +CalculateEffort()* double
        +GetDetailedInfo()* string
        +event StatusChanged
    }

    class FeatureTask {
        +string AcceptanceCriteria
        +CalculateEffort() double
        +GetDetailedInfo() string
    }

    class BugTask {
        +string ReproductionSteps
        +string Severity
        +CalculateEffort() double
        +GetDetailedInfo() string
    }

    class StoryTask {
        +int StoryPoints
        +CalculateEffort() double
        +GetDetailedInfo() string
    }

    class Epic {
        -List~ITaskComponent~ _tasks
        +Guid Id
        +string Title
        +string Description
        +Priority Priority
        +KanbanTaskStatus Status
        +Add(component)
        +Remove(component)
        +GetChildren()
        +CountCompleted() int
        +CountTotal() int
    }

    class ToDoState
    class InProgressState
    class ReviewState
    class DoneState
    class BlockedState

    class TaskFactory {
        +Create(type, title, priority) TaskItem
        +CreateFromConfig(...) TaskItem
    }

    class SingletonTaskFactory {
        +Instance SingletonTaskFactory
        +Create(...) TaskItem
    }

    class KanbanBoardFacade {
        -ITaskFactory _factory
        -List~TaskItem~ _tasks
        -List~Epic~ _epics
        -List~User~ _users
        +CreateTask(...)
        +CreateEpic(...)
        +MoveTaskNext(id)
        +MoveTaskPrevious(id)
        +GetStatusStatistics()
    }

    class ITaskSortStrategy {
        <<interface>>
        +string Name
        +Sort(tasks) IEnumerable~TaskItem~
    }

    class PrioritySortStrategy
    class CreatedDateSortStrategy
    class TitleSortStrategy
    class StatusSortStrategy

    class TaskDecorator {
        <<abstract>>
        #ITaskComponent _inner
    }

    class LoggingTaskDecorator
    class UrgentTaskDecorator

    IEntity <|.. User
    IEntity <|.. TaskItem
    IEntity <|.. Epic

    ITaskComponent <|.. TaskItem
    ITaskComponent <|.. Epic
    ITaskComponent <|.. TaskDecorator

    TaskItem <|-- FeatureTask
    TaskItem <|-- BugTask
    TaskItem <|-- StoryTask

    ITaskState <|.. ToDoState
    ITaskState <|.. InProgressState
    ITaskState <|.. ReviewState
    ITaskState <|.. DoneState
    ITaskState <|.. BlockedState

    TaskItem o--> ITaskState : _state
    TaskItem o--> ITaskComponent : children
    Epic o--> ITaskComponent : tasks

    ITaskFactory <|.. TaskFactory
    ITaskFactory <|.. SingletonTaskFactory
    TaskFactory <.. SingletonTaskFactory : uses

    ITaskSortStrategy <|.. PrioritySortStrategy
    ITaskSortStrategy <|.. CreatedDateSortStrategy
    ITaskSortStrategy <|.. TitleSortStrategy
    ITaskSortStrategy <|.. StatusSortStrategy

    TaskDecorator <|-- LoggingTaskDecorator
    TaskDecorator <|-- UrgentTaskDecorator

    KanbanBoardFacade o--> ITaskFactory
    KanbanBoardFacade o--> TaskItem
    KanbanBoardFacade o--> Epic
    KanbanBoardFacade o--> User
```

### Спрощена схема (текстом)

```
ITaskComponent
 ├── TaskItem
 │    ├── FeatureTask
 │    ├── BugTask
 │    └── StoryTask
 └── Epic

ITaskState  ←── TaskItem._state
 ├── ToDoState
 ├── InProgressState
 ├── ReviewState
 ├── DoneState
 └── BlockedState

ITaskFactory
 ├── TaskFactory
 └── SingletonTaskFactory

KanbanBoardFacade  →  Factory, Tasks, Epics, Users
```

---

## Структура рішення

```
KanbanTracker/
├── KanbanTracker.Domain/          # Сутності, інтерфейси, патерни, винятки
│   ├── Entities/                  # User, TaskItem, FeatureTask, BugTask, StoryTask, Epic
│   ├── Enums/                     # KanbanTaskStatus, TaskType, Priority
│   ├── Exceptions/                # Domain / Validation / InvalidStatusTransition / TaskNotFound
│   ├── Interfaces/                # IEntity, ITaskComponent, ITaskState, IRepository
│   └── Patterns/
│       ├── State/
│       ├── Factory/
│       ├── Strategy/
│       ├── Observer/
│       ├── Decorator/
│       ├── Facade/
│       └── Composite (через ITaskComponent)
├── KanbanTracker.Application/     # Сервіси, DTO, Repository, JSON
├── KanbanTracker.UI/              # Avalonia MVVM (Канбан-дошка)
└── KanbanTracker.Tests/           # xUnit + Moq
```

---

## Застосовані принципи та патерни ООП

### Розділ I — Основи ООП
- Інкапсуляція + валідація в сетерах
- Конструктори: основний, з параметрами, копіювальний
- `IDisposable` / фіналізатор
- Наслідування + `virtual` / `override`
- Інтерфейси та контракти
- Індексатори, перевантаження операторів (`+`, `==`, `!=`)

### Розділ II — Дані та помилки
- Generics: `IRepository<T>`, `InMemoryRepository<T>`
- Колекції: `List<T>`, `Dictionary<TKey,TValue>`
- LINQ: `Where`, `GroupBy`, `OrderBy`, `Average`, `Sum`
- Ієрархія Custom Exceptions
- Retry Policy з експоненційною затримкою

### Розділ III — SOLID + патерни
| Патерн | Призначення в проєкті |
|--------|------------------------|
| **State** | Переходи статусів задачі (ToDo → InProgress → Review → Done / Blocked) |
| **Composite** | Дерево підзадач + Epic як контейнер |
| **Factory Method** | Створення Feature / Bug / Story / Technical |
| **Singleton** | `SingletonTaskFactory.Instance` |
| **Strategy** | Сортування задач (Priority, Date, Title, Status) |
| **Observer** | Подія `StatusChanged`, `TaskEventPublisher` |
| **Decorator** | Logging / Urgent обгортки |
| **Facade** | `KanbanBoardFacade` — єдиний вхід до домену |

SOLID: SRP (шари), OCP (розширення через патерни), LSP (підтипи TaskItem), ISP (вузькі інтерфейси), DIP (залежність від абстракцій).

### Розділ IV — Серіалізація та тести
- JSON (`board.json` у папці проєкту)
- DTO: `TaskDto`, `EpicDto`, `UserDto` + мапінг Domain ↔ DTO
- Unit-тести (xUnit) + Moq

---

## Як запустити

### Вимоги
- .NET 8 SDK
- Windows / Linux / macOS (Avalonia)

### Команди
```bash
cd KanbanTracker
dotnet restore
dotnet build
dotnet run --project KanbanTracker.UI
dotnet test
```

### Збереження даних
- Файл: `board.json` у корені рішення (поточна робоча директорія)
- **Save** — запис стану
- При наступному запуску дані підвантажуються автоматично
- Якщо файлу немає — завантажуються демо-дані

---

## Використання UI

| Дія | Як |
|-----|-----|
| Створити задачу | Title + Description + Type + Priority → **+ Add Task** |
| Перемістити | Кнопки **Next →** / **← Prev** (State) |
| Видалити | **Del** |
| Пошук | Текст → **Search** (по назві, опису, типу) |
| Сортування | Вибір стратегії → **Sort** |
| Зберегти | **Save** → `board.json` |

На картці відображаються: назва, тип, пріоритет, опис, **дата створення**.
<img width="1919" height="1032" alt="image" src="https://github.com/user-attachments/assets/f190b8ec-3a7c-4824-b869-918dff7bffbf" />

---

