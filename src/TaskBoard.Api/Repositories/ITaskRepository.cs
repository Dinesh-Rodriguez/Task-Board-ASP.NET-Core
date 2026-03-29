using TaskBoard.Api.DTOs.Tasks;
using TaskBoard.Api.Enums;
using TaskBoard.Api.Models;
using TaskStatus = TaskBoard.Api.Enums.TaskStatus;

namespace TaskBoard.Api.Repositories;

public interface ITaskRepository : IRepository<TaskItem>
{
    Task<TaskItem?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<TaskItem>> GetByProjectIdAsync(int projectId, CancellationToken ct = default);
    Task<IReadOnlyList<TaskItem>> GetByAssigneeIdAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<TaskItem>> GetByStatusAsync(int projectId, TaskStatus status, CancellationToken ct = default);
    Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, TaskQueryParams query, CancellationToken ct = default);
}
