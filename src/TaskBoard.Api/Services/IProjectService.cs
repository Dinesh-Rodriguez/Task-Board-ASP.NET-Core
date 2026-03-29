using TaskBoard.Api.Models;

namespace TaskBoard.Api.Services;

public interface IProjectService
{
    Task<IReadOnlyList<Project>> GetAllByUserAsync(int userId, CancellationToken ct = default);
    Task<Project?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Project> CreateAsync(string name, string? description, int ownerId, CancellationToken ct = default);
    Task<Project> UpdateAsync(int id, string name, string? description, CancellationToken ct = default);
    Task ArchiveAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
