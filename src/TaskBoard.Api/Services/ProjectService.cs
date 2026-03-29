using TaskBoard.Api.Models;
using TaskBoard.Api.Repositories;

namespace TaskBoard.Api.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projects;

    public ProjectService(IProjectRepository projects)
    {
        _projects = projects;
    }

    public async Task<IReadOnlyList<Project>> GetAllByUserAsync(int userId, CancellationToken ct = default)
    {
        var owned = await _projects.GetByOwnerIdAsync(userId, ct);
        var member = await _projects.GetByMemberIdAsync(userId, ct);
        return owned.Union(member).DistinctBy(p => p.Id).ToList().AsReadOnly();
    }

    public Task<Project?> GetByIdAsync(int id, CancellationToken ct = default)
        => _projects.GetByIdWithDetailsAsync(id, ct);

    public async Task<Project> CreateAsync(string name, string? description, int ownerId, CancellationToken ct = default)
    {
        var project = new Project
        {
            Name = name,
            Description = description,
            OwnerId = ownerId
        };
        await _projects.AddAsync(project, ct);
        await _projects.SaveChangesAsync(ct);
        return project;
    }

    public async Task<Project> UpdateAsync(int id, string name, string? description, CancellationToken ct = default)
    {
        var project = await _projects.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Project {id} not found.");

        project.Name = name;
        project.Description = description;
        _projects.Update(project);
        await _projects.SaveChangesAsync(ct);
        return project;
    }

    public async Task ArchiveAsync(int id, CancellationToken ct = default)
    {
        var project = await _projects.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Project {id} not found.");

        project.IsArchived = true;
        _projects.Update(project);
        await _projects.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var project = await _projects.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Project {id} not found.");

        _projects.Remove(project);
        await _projects.SaveChangesAsync(ct);
    }
}
