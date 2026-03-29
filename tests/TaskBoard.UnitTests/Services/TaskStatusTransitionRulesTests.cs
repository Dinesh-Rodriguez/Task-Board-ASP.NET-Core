using FluentAssertions;
using TaskBoard.Api.Services;
using TaskStatus = TaskBoard.Api.Enums.TaskStatus;

namespace TaskBoard.UnitTests.Services;

public class TaskStatusTransitionRulesTests
{
    // ── IsValid: same-status is always allowed ────────────────────────────────

    [Theory]
    [InlineData(TaskStatus.Todo)]
    [InlineData(TaskStatus.InProgress)]
    [InlineData(TaskStatus.InReview)]
    [InlineData(TaskStatus.Done)]
    [InlineData(TaskStatus.Cancelled)]
    public void IsValid_SameStatus_ReturnsTrue(TaskStatus status)
    {
        TaskStatusTransitionRules.IsValid(status, status).Should().BeTrue();
    }

    // ── Allowed forward transitions ───────────────────────────────────────────

    [Theory]
    [InlineData(TaskStatus.Todo,       TaskStatus.InProgress)]
    [InlineData(TaskStatus.Todo,       TaskStatus.Cancelled)]
    [InlineData(TaskStatus.InProgress, TaskStatus.InReview)]
    [InlineData(TaskStatus.InProgress, TaskStatus.Todo)]
    [InlineData(TaskStatus.InProgress, TaskStatus.Cancelled)]
    [InlineData(TaskStatus.InReview,   TaskStatus.Done)]
    [InlineData(TaskStatus.InReview,   TaskStatus.InProgress)]
    [InlineData(TaskStatus.InReview,   TaskStatus.Cancelled)]
    [InlineData(TaskStatus.Done,       TaskStatus.InProgress)]  // reopen
    [InlineData(TaskStatus.Cancelled,  TaskStatus.Todo)]        // restore
    public void IsValid_AllowedTransition_ReturnsTrue(TaskStatus from, TaskStatus to)
    {
        TaskStatusTransitionRules.IsValid(from, to).Should().BeTrue();
    }

    // ── Forbidden transitions ─────────────────────────────────────────────────

    [Theory]
    [InlineData(TaskStatus.Todo,       TaskStatus.InReview)]
    [InlineData(TaskStatus.Todo,       TaskStatus.Done)]
    [InlineData(TaskStatus.InProgress, TaskStatus.Done)]
    [InlineData(TaskStatus.InReview,   TaskStatus.Todo)]
    [InlineData(TaskStatus.Done,       TaskStatus.Todo)]
    [InlineData(TaskStatus.Done,       TaskStatus.Cancelled)]
    [InlineData(TaskStatus.Cancelled,  TaskStatus.Done)]
    [InlineData(TaskStatus.Cancelled,  TaskStatus.InProgress)]
    [InlineData(TaskStatus.Cancelled,  TaskStatus.InReview)]
    public void IsValid_ForbiddenTransition_ReturnsFalse(TaskStatus from, TaskStatus to)
    {
        TaskStatusTransitionRules.IsValid(from, to).Should().BeFalse();
    }

    // ── GetAllowedTransitions ─────────────────────────────────────────────────

    [Fact]
    public void GetAllowedTransitions_Todo_ReturnsInProgressAndCancelled()
    {
        var allowed = TaskStatusTransitionRules.GetAllowedTransitions(TaskStatus.Todo);
        allowed.Should().BeEquivalentTo(new[] { TaskStatus.InProgress, TaskStatus.Cancelled });
    }

    [Fact]
    public void GetAllowedTransitions_InProgress_ReturnsThreeStatuses()
    {
        var allowed = TaskStatusTransitionRules.GetAllowedTransitions(TaskStatus.InProgress);
        allowed.Should().BeEquivalentTo(new[] { TaskStatus.InReview, TaskStatus.Todo, TaskStatus.Cancelled });
    }

    [Fact]
    public void GetAllowedTransitions_InReview_ReturnsThreeStatuses()
    {
        var allowed = TaskStatusTransitionRules.GetAllowedTransitions(TaskStatus.InReview);
        allowed.Should().BeEquivalentTo(new[] { TaskStatus.Done, TaskStatus.InProgress, TaskStatus.Cancelled });
    }

    [Fact]
    public void GetAllowedTransitions_Done_ReturnsInProgressOnly()
    {
        var allowed = TaskStatusTransitionRules.GetAllowedTransitions(TaskStatus.Done);
        allowed.Should().BeEquivalentTo(new[] { TaskStatus.InProgress });
    }

    [Fact]
    public void GetAllowedTransitions_Cancelled_ReturnsTodoOnly()
    {
        var allowed = TaskStatusTransitionRules.GetAllowedTransitions(TaskStatus.Cancelled);
        allowed.Should().BeEquivalentTo(new[] { TaskStatus.Todo });
    }

    // ── Edge case: every status has at least one allowed next step ────────────

    [Theory]
    [InlineData(TaskStatus.Todo)]
    [InlineData(TaskStatus.InProgress)]
    [InlineData(TaskStatus.InReview)]
    [InlineData(TaskStatus.Done)]
    [InlineData(TaskStatus.Cancelled)]
    public void GetAllowedTransitions_AlwaysHasAtLeastOneOption(TaskStatus status)
    {
        TaskStatusTransitionRules.GetAllowedTransitions(status).Should().NotBeEmpty();
    }
}
