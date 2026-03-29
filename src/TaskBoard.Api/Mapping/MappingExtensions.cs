using TaskBoard.Api.DTOs.Projects;
using TaskBoard.Api.DTOs.Tasks;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.Mapping;

public static class MappingExtensions
{
    public static ProjectResponse ToResponse(this Project p) => new()
    {
        Id          = p.Id,
        Name        = p.Name,
        Description = p.Description,
        OwnerId     = p.OwnerId,
        OwnerName   = p.Owner?.Name ?? string.Empty,
        IsArchived  = p.IsArchived,
        CreatedAt   = p.CreatedAt,
        UpdatedAt   = p.UpdatedAt,
        TaskCount   = p.Tasks?.Count ?? 0,
        MemberCount = p.Members?.Count ?? 0
    };

    public static TaskResponse ToResponse(this TaskItem t) => new()
    {
        Id           = t.Id,
        Title        = t.Title,
        Description  = t.Description,
        Status       = t.Status,
        Priority     = t.Priority,
        DueDate      = t.DueDate,
        IsArchived   = t.IsArchived,
        CreatedAt    = t.CreatedAt,
        UpdatedAt    = t.UpdatedAt,
        ProjectId    = t.ProjectId,
        ProjectName  = t.Project?.Name ?? string.Empty,
        AssigneeId   = t.AssigneeId,
        AssigneeName = t.Assignee?.Name
    };
}
