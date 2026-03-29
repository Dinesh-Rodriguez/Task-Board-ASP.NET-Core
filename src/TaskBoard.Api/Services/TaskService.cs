using TaskBoard.Api.Enums;
using TaskBoard.Api.Models;
using TaskBoard.Api.Repositories;
using TaskStatus = TaskBoard.Api.Enums.TaskStatus;

namespace TaskBoard.Api.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _tasks;

    public TaskService(ITaskRepository tasks)
    {
        _tasks = tasks;
    }

    public Task<IReadOnlyList<TaskItem>> GetByProjectAsync(int projectId, CancellationToken ct = default)
        => _tasks.GetByProjectIdAsync(projectId, ct);

    public Task<IReadOnlyList<TaskItem>> GetByAssigneeAsync(int userId, CancellationToken ct = default)
        => _tasks.GetByAssigneeIdAsync(userId, ct);

    public Task<TaskItem?> GetByIdAsync(int id, CancellationToken ct = default)
        => _tasks.GetByIdWithDetailsAsync(id, ct);

    public async Task<TaskItem> CreateAsync(string title, string? description, int projectId, int? assigneeId, TaskPriority priority, DateTime? dueDate, CancellationToken ct = default)
    {
        var task = new TaskItem
        {
            Title = title,
            Description = description,
            ProjectId = projectId,
            AssigneeId = assigneeId,
            Priority = priority,
            DueDate = dueDate,
            Status = TaskStatus.Todo
        };
        await _tasks.AddAsync(task, ct);
        await _tasks.SaveChangesAsync(ct);
        return task;
    }

    public async Task<TaskItem> UpdateAsync(int id, string title, string? description, TaskStatus status, TaskPriority priority, int? assigneeId, DateTime? dueDate, CancellationToken ct = default)
    {
        var task = await _tasks.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Task {id} not found.");

        if (!TaskStatusTransitionRules.IsValid(task.Status, status))
        {
            var allowed = TaskStatusTransitionRules.GetAllowedTransitions(task.Status);
            throw new InvalidOperationException(
                $"Cannot transition task from '{task.Status}' to '{status}'. " +
                $"Allowed transitions: {string.Join(", ", allowed)}.");
        }

        task.Title = title;
        task.Description = description;
        task.Status = status;
        task.Priority = priority;
        task.AssigneeId = assigneeId;
        task.DueDate = dueDate;

        _tasks.Update(task);
        await _tasks.SaveChangesAsync(ct);
        return task;
    }

    public async Task UpdateStatusAsync(int id, TaskStatus status, CancellationToken ct = default)
    {
        var task = await _tasks.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Task {id} not found.");

        if (!TaskStatusTransitionRules.IsValid(task.Status, status))
        {
            var allowed = TaskStatusTransitionRules.GetAllowedTransitions(task.Status);
            throw new InvalidOperationException(
                $"Cannot transition task from '{task.Status}' to '{status}'. " +
                $"Allowed transitions: {string.Join(", ", allowed)}.");
        }

        task.Status = status;
        _tasks.Update(task);
        await _tasks.SaveChangesAsync(ct);
    }

    public async Task ArchiveAsync(int id, CancellationToken ct = default)
    {
        var task = await _tasks.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Task {id} not found.");

        task.IsArchived = true;
        _tasks.Update(task);
        await _tasks.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var task = await _tasks.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Task {id} not found.");

        _tasks.Remove(task);
        await _tasks.SaveChangesAsync(ct);
    }
}
