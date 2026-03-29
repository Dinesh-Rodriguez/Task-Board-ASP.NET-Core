using Microsoft.EntityFrameworkCore;
using TaskBoard.Api.Data;
using TaskBoard.Api.Models;
using TaskStatus = TaskBoard.Api.Enums.TaskStatus;

namespace TaskBoard.Api.Repositories;

public class TaskRepository : Repository<TaskItem>, ITaskRepository
{
    public TaskRepository(ApplicationDbContext context) : base(context) { }

    public Task<TaskItem?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default)
        => _dbSet.AsNoTracking()
                 .Include(t => t.Project)
                 .Include(t => t.Assignee)
                 .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<TaskItem>> GetByProjectIdAsync(int projectId, CancellationToken ct = default)
        => await _dbSet.AsNoTracking()
                       .Where(t => t.ProjectId == projectId)
                       .Include(t => t.Assignee)
                       .ToListAsync(ct);

    public async Task<IReadOnlyList<TaskItem>> GetByAssigneeIdAsync(int userId, CancellationToken ct = default)
        => await _dbSet.AsNoTracking()
                       .Where(t => t.AssigneeId == userId)
                       .Include(t => t.Project)
                       .ToListAsync(ct);

    public async Task<IReadOnlyList<TaskItem>> GetByStatusAsync(int projectId, TaskStatus status, CancellationToken ct = default)
        => await _dbSet.AsNoTracking()
                       .Where(t => t.ProjectId == projectId && t.Status == status)
                       .Include(t => t.Assignee)
                       .ToListAsync(ct);
}
