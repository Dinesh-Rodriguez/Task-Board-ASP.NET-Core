using TaskStatus = TaskBoard.Api.Enums.TaskStatus;

namespace TaskBoard.Api.Services;

/// <summary>
/// Enforces allowed workflow transitions between task statuses.
/// </summary>
public static class TaskStatusTransitionRules
{
    // Defines which statuses a task can move TO from a given FROM status.
    private static readonly IReadOnlyDictionary<TaskStatus, IReadOnlySet<TaskStatus>> _allowedTransitions =
        new Dictionary<TaskStatus, IReadOnlySet<TaskStatus>>
        {
            [TaskStatus.Todo]       = new HashSet<TaskStatus> { TaskStatus.InProgress, TaskStatus.Cancelled },
            [TaskStatus.InProgress] = new HashSet<TaskStatus> { TaskStatus.InReview, TaskStatus.Todo, TaskStatus.Cancelled },
            [TaskStatus.InReview]   = new HashSet<TaskStatus> { TaskStatus.Done, TaskStatus.InProgress, TaskStatus.Cancelled },
            [TaskStatus.Done]       = new HashSet<TaskStatus> { TaskStatus.InProgress },   // allow reopening
            [TaskStatus.Cancelled]  = new HashSet<TaskStatus> { TaskStatus.Todo },          // allow restoring
        };

    /// <summary>Returns true if moving from <paramref name="current"/> to <paramref name="next"/> is valid.</summary>
    public static bool IsValid(TaskStatus current, TaskStatus next)
    {
        if (current == next) return true;
        return _allowedTransitions.TryGetValue(current, out var allowed) && allowed.Contains(next);
    }

    /// <summary>Returns all valid next statuses from a given current status.</summary>
    public static IReadOnlySet<TaskStatus> GetAllowedTransitions(TaskStatus current)
        => _allowedTransitions.TryGetValue(current, out var allowed)
            ? allowed
            : new HashSet<TaskStatus>();
}
