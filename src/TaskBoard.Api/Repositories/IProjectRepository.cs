using TaskBoard.Api.Models;

namespace TaskBoard.Api.Repositories;

public interface IProjectRepository : IRepository<Project>
{
    Task<Project?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Project>> GetByOwnerIdAsync(int ownerId, CancellationToken ct = default);
    Task<IReadOnlyList<Project>> GetByMemberIdAsync(int userId, CancellationToken ct = default);
}
