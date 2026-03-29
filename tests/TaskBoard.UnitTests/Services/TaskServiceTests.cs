using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using TaskBoard.Api.Enums;
using TaskBoard.Api.Models;
using TaskBoard.Api.Repositories;
using TaskBoard.Api.Services;
using TaskStatus = TaskBoard.Api.Enums.TaskStatus;

namespace TaskBoard.UnitTests.Services;

public class TaskServiceTests
{
    private readonly ITaskRepository _repo = Substitute.For<ITaskRepository>();
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(_repo);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsTaskWithTodoStatus()
    {
        _repo.AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>())
             .Returns(Task.CompletedTask);
        _repo.SaveChangesAsync(Arg.Any<CancellationToken>())
             .Returns(Task.FromResult(1));

        var result = await _sut.CreateAsync("New Task", null, 1, null, TaskPriority.Medium, null);

        result.Title.Should().Be("New Task");
        result.Status.Should().Be(TaskStatus.Todo);
        result.Priority.Should().Be(TaskPriority.Medium);
    }

    [Fact]
    public async Task CreateAsync_WithAllFields_MapsCorrectly()
    {
        var dueDate = DateTime.UtcNow.AddDays(7);
        _repo.AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _repo.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var result = await _sut.CreateAsync("Title", "Desc", 5, 3, TaskPriority.High, dueDate);

        result.Title.Should().Be("Title");
        result.Description.Should().Be("Desc");
        result.ProjectId.Should().Be(5);
        result.AssigneeId.Should().Be(3);
        result.Priority.Should().Be(TaskPriority.High);
        result.DueDate.Should().Be(dueDate);
    }

    // ── UpdateStatusAsync: valid transition ───────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_ValidTransition_SavesNewStatus()
    {
        var task = new TaskItem { Id = 1, Title = "T", Status = TaskStatus.Todo };
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(task);
        _repo.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        await _sut.UpdateStatusAsync(1, TaskStatus.InProgress);

        task.Status.Should().Be(TaskStatus.InProgress);
        _repo.Received(1).Update(task);
    }

    // ── UpdateStatusAsync: invalid transition throws ──────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_ThrowsInvalidOperationException()
    {
        var task = new TaskItem { Id = 1, Title = "T", Status = TaskStatus.Todo };
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(task);

        var act = () => _sut.UpdateStatusAsync(1, TaskStatus.Done);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot transition*");
    }

    [Theory]
    [InlineData(TaskStatus.Todo,       TaskStatus.Done)]
    [InlineData(TaskStatus.Todo,       TaskStatus.InReview)]
    [InlineData(TaskStatus.InProgress, TaskStatus.Done)]
    [InlineData(TaskStatus.Done,       TaskStatus.Cancelled)]
    public async Task UpdateStatusAsync_ForbiddenTransitions_Throw(TaskStatus from, TaskStatus to)
    {
        var task = new TaskItem { Id = 1, Title = "T", Status = from };
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(task);

        var act = () => _sut.UpdateStatusAsync(1, to);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── UpdateStatusAsync: task not found ─────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_TaskNotFound_ThrowsKeyNotFoundException()
    {
        _repo.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();

        var act = () => _sut.UpdateStatusAsync(99, TaskStatus.InProgress);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Task 99 not found*");
    }

    // ── UpdateAsync: invalid transition throws ────────────────────────────────

    [Fact]
    public async Task UpdateAsync_InvalidTransition_ThrowsAndIncludesAllowedList()
    {
        var task = new TaskItem { Id = 2, Title = "T", Status = TaskStatus.Todo };
        _repo.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(task);

        var act = () => _sut.UpdateAsync(2, "New Title", null, TaskStatus.Done,
                                         TaskPriority.Low, null, null);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*Allowed transitions*");
    }

    // ── ArchiveAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ArchiveAsync_ExistingTask_SetsIsArchivedTrue()
    {
        var task = new TaskItem { Id = 3, Title = "T", IsArchived = false };
        _repo.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(task);
        _repo.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        await _sut.ArchiveAsync(3);

        task.IsArchived.Should().BeTrue();
        _repo.Received(1).Update(task);
    }

    [Fact]
    public async Task ArchiveAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _repo.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();

        var act = () => _sut.ArchiveAsync(99);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingTask_CallsRemoveAndSave()
    {
        var task = new TaskItem { Id = 4, Title = "T" };
        _repo.GetByIdAsync(4, Arg.Any<CancellationToken>()).Returns(task);
        _repo.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        await _sut.DeleteAsync(4);

        _repo.Received(1).Remove(task);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForMissingTask()
    {
        _repo.GetByIdWithDetailsAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await _sut.GetByIdAsync(99);

        result.Should().BeNull();
    }
}
