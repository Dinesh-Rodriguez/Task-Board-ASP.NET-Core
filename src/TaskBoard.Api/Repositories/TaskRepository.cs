using Microsoft.EntityFrameworkCore;
using TaskBoard.Api.Data;
using TaskBoard.Api.DTOs.Tasks;
using TaskBoard.Api.Enums;
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

    public async Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, TaskQueryParams query, CancellationToken ct = default)
    {
        var q = _dbSet.AsNoTracking()
                      .Where(t => t.ProjectId == projectId)
                      .Include(t => t.Assignee)
                      .AsQueryable();

        if (!query.IncludeArchived)
            q = q.Where(t => !t.IsArchived);

        if (query.Status.HasValue)
            q = q.Where(t => t.Status == query.Status.Value);

        if (query.Priority.HasValue)
            q = q.Where(t => t.Priority == query.Priority.Value);

        if (query.AssigneeId.HasValue)
            q = q.Where(t => t.AssigneeId == query.AssigneeId.Value);

        var totalCount = await q.CountAsync(ct);

        q = (query.SortBy.ToLowerInvariant(), query.SortDir.ToLowerInvariant()) switch
        {
            ("duedate",   "desc") => q.OrderByDescending(t => t.DueDate),
            ("duedate",   _)     => q.OrderBy(t => t.DueDate),
            ("priority",  "desc") => q.OrderByDescending(t => t.Priority),
            ("priority",  _)     => q.OrderBy(t => t.Priority),
            ("status",    "desc") => q.OrderByDescending(t => t.Status),
            ("status",    _)     => q.OrderBy(t => t.Status),
            (_,           "desc") => q.OrderByDescending(t => t.CreatedAt),
            _                    => q.OrderBy(t => t.CreatedAt)
        };

        var page     = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var items    = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, totalCount);
    }
}
