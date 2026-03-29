using TaskBoard.Api.Enums;
using TaskBoard.Api.Models;
using TaskStatus = TaskBoard.Api.Enums.TaskStatus;

namespace TaskBoard.Api.Services;

public interface ITaskService
{
    Task<IReadOnlyList<TaskItem>> GetByProjectAsync(int projectId, CancellationToken ct = default);
    Task<IReadOnlyList<TaskItem>> GetByAssigneeAsync(int userId, CancellationToken ct = default);
    Task<TaskItem?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TaskItem> CreateAsync(string title, string? description, int projectId, int? assigneeId, TaskPriority priority, DateTime? dueDate, CancellationToken ct = default);
    Task<TaskItem> UpdateAsync(int id, string title, string? description, TaskStatus status, TaskPriority priority, int? assigneeId, DateTime? dueDate, CancellationToken ct = default);
    Task UpdateStatusAsync(int id, TaskStatus status, CancellationToken ct = default);
    Task ArchiveAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
