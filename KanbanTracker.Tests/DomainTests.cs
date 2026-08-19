using KanbanTracker.Domain.Entities;
using KanbanTracker.Domain.Enums;
using KanbanTracker.Domain.Exceptions;
using KanbanTracker.Domain.Patterns.Factory;
using KanbanTracker.Domain.Patterns.Facade;
using KanbanTracker.Domain.Patterns.State;
using KanbanTracker.Domain.Patterns.Strategy;

using Moq;
using KanbanTracker.Domain.Interfaces;

namespace KanbanTracker.Tests;

public class DomainTests
{
    [Fact]
    public void TaskItem_Validation_ThrowsOnEmptyTitle()
    {
        Assert.Throws<ValidationException>(() => new TaskItem(""));
    }

    [Fact]
    public void TaskItem_StateTransitions_WorkCorrectly()
    {
        var task = new TaskItem("Test");
        Assert.Equal(KanbanTaskStatus.ToDo, task.Status);

        task.MoveNext();
        Assert.Equal(KanbanTaskStatus.InProgress, task.Status);

        task.MoveNext();
        Assert.Equal(KanbanTaskStatus.Review, task.Status);

        task.MoveNext();
        Assert.Equal(KanbanTaskStatus.Done, task.Status);

        Assert.Throws<InvalidStatusTransitionException>(() => task.MoveNext());
    }

    [Fact]
    public void TaskFactory_CreatesCorrectSubtypes()
    {
        var factory = new KanbanTracker.Domain.Patterns.Factory.TaskFactory();
        var feature = factory.Create(TaskType.Feature, "F1");
        var bug = factory.Create(TaskType.Bug, "B1");
        var story = factory.Create(TaskType.Story, "S1");

        Assert.IsType<FeatureTask>(feature);
        Assert.IsType<BugTask>(bug);
        Assert.IsType<StoryTask>(story);
    }

    [Fact]
    public void SingletonFactory_ReturnsSameInstance()
    {
        var a = SingletonTaskFactory.Instance;
        var b = SingletonTaskFactory.Instance;
        Assert.Same(a, b);
    }

    [Fact]
    public void Composite_CountsSubtasks()
    {
        var parent = new TaskItem("Parent");
        var child1 = new TaskItem("Child1");
        var child2 = new TaskItem("Child2");
        child2.MoveNext(); // ToDo → InProgress
        child2.MoveNext(); // InProgress → Review
        child2.MoveNext(); // Review → Done

        parent.Add(child1);
        parent.Add(child2);

        Assert.Equal(3, parent.CountTotal());
        Assert.Equal(1, parent.CountCompleted());
        Assert.False(parent.IsLeaf);
    }

    [Fact]
    public void Epic_OperatorPlus_AddsTask()
    {
        var epic = new Epic("E1");
        var task = new TaskItem("T1");
        epic = epic + task;
        Assert.Equal(1, epic.TaskCount);
    }

    [Fact]
    public void Strategy_SortsByPriority()
    {
        var tasks = new List<TaskItem>
        {
            new("Low", priority: Priority.Low),
            new("High", priority: Priority.High),
            new("Crit", priority: Priority.Critical)
        };
        var sorter = new TaskSorter(new PrioritySortStrategy());
        var sorted = sorter.Sort(tasks).ToList();
        Assert.Equal(Priority.Critical, sorted[0].Priority);
        Assert.Equal(Priority.High, sorted[1].Priority);
    }

    [Fact]
    public void Facade_CreatesAndMovesTasks()
    {
        var board = new KanbanBoardFacade();
        var user = board.RegisterUser("Test", "t@t.com");
        var task = board.CreateTask(TaskType.Feature, "Demo", Priority.High, user.Id);

        Assert.Equal(KanbanTaskStatus.ToDo, task.Status);
        board.MoveTaskNext(task.Id);
        Assert.Equal(KanbanTaskStatus.InProgress, board.GetTask(task.Id).Status);
    }

    [Fact]
    public void Inheritance_PolymorphicCalculateEffort()
    {
        TaskItem feature = new FeatureTask("F");
        TaskItem bug = new BugTask("B");
        TaskItem story = new StoryTask("S", 5);

        Assert.True(feature.CalculateEffort() > bug.CalculateEffort());
        Assert.Equal(5.0, story.CalculateEffort());
    }

    [Fact]
    public void User_EqualityOperators()
    {
        var u1 = new User("A", "a@a.com");
        var u2 = new User(u1.Id, "A", "a@a.com", u1.CreatedAt);
        var u3 = new User("B", "b@b.com");

        Assert.True(u1 == u2);
        Assert.True(u1 != u3);
    }

    [Fact]
    public void Repository_GenericWorks()
    {
        var repo = new KanbanTracker.Application.Repositories.InMemoryRepository<User>();
        var user = new User("X", "x@x.com");
        repo.Add(user);
        Assert.True(repo.Exists(user.Id));
        Assert.Equal(user, repo.GetById(user.Id));
    }

    [Fact]
    public void Mock_RepositoryIsolation()
    {
        var mock = new Mock<IRepository<TaskItem>>();
        mock.Setup(r => r.GetById(It.IsAny<Guid>())).Returns((TaskItem?)null);

        var result = mock.Object.GetById(Guid.NewGuid());
        Assert.Null(result);
        mock.Verify(r => r.GetById(It.IsAny<Guid>()), Times.Once);
    }
}
