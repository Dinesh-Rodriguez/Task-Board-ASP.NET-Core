using Microsoft.EntityFrameworkCore;
using TaskBoard.Api.Data;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.Repositories;

public class ProjectRepository : Repository<Project>, IProjectRepository
{
    public ProjectRepository(ApplicationDbContext context) : base(context) { }

    public Task<Project?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default)
        => _dbSet.AsNoTracking()
                 .Include(p => p.Owner)
                 .Include(p => p.Members).ThenInclude(m => m.User)
                 .Include(p => p.Tasks)
                 .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Project>> GetByOwnerIdAsync(int ownerId, CancellationToken ct = default)
        => await _dbSet.AsNoTracking()
                       .Where(p => p.OwnerId == ownerId)
                       .ToListAsync(ct);

    public async Task<IReadOnlyList<Project>> GetByMemberIdAsync(int userId, CancellationToken ct = default)
        => await _dbSet.AsNoTracking()
                       .Where(p => p.Members.Any(m => m.UserId == userId))
                       .ToListAsync(ct);
}
