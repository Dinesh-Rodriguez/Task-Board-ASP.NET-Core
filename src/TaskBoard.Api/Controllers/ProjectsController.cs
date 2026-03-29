using Microsoft.AspNetCore.Mvc;
using TaskBoard.Api.Auth;
using TaskBoard.Api.DTOs.Projects;
using TaskBoard.Api.DTOs.Tasks;
using TaskBoard.Api.Enums;
using TaskBoard.Api.Mapping;
using TaskBoard.Api.Services;

namespace TaskBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;

    public ProjectsController(IProjectService projectService, ITaskService taskService)
    {
        _projectService = projectService;
        _taskService    = taskService;
    }

    // GET /api/projects?userId=1
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int userId, CancellationToken ct)
    {
        var projects = await _projectService.GetAllByUserAsync(userId, ct);
        return Ok(projects.Select(p => p.ToResponse()));
    }

    // GET /api/projects/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var project = await _projectService.GetByIdAsync(id, ct);
        if (project is null) return NotFound(new { error = $"Project {id} not found." });
        return Ok(project.ToResponse());
    }

    // POST /api/projects
    [HttpPost]
    [RequireRole]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, [FromQuery] int ownerId, CancellationToken ct)
    {
        var project = await _projectService.CreateAsync(request.Name, request.Description, ownerId, ct);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project.ToResponse());
    }

    // PUT /api/projects/{id}
    [HttpPut("{id:int}")]
    [RequireRole]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectRequest request, CancellationToken ct)
    {
        var project = await _projectService.UpdateAsync(id, request.Name, request.Description, ct);
        return Ok(project.ToResponse());
    }

    // PATCH /api/projects/{id}/archive
    [HttpPatch("{id:int}/archive")]
    [RequireRole(UserRole.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(int id, CancellationToken ct)
    {
        await _projectService.ArchiveAsync(id, ct);
        return NoContent();
    }

    // DELETE /api/projects/{id}
    [HttpDelete("{id:int}")]
    [RequireRole(UserRole.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _projectService.DeleteAsync(id, ct);
        return NoContent();
    }

    // GET /api/projects/{projectId}/tasks
    [HttpGet("{projectId:int}/tasks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTasks(
        int projectId,
        [FromQuery] TaskQueryParams query,
        CancellationToken ct)
    {
        var project = await _projectService.GetByIdAsync(projectId, ct);
        if (project is null) return NotFound(new { error = $"Project {projectId} not found." });

        var paged = await _taskService.GetPagedByProjectAsync(projectId, query, ct);

        return Ok(new
        {
            paged.Page,
            paged.PageSize,
            paged.TotalCount,
            paged.TotalPages,
            paged.HasPreviousPage,
            paged.HasNextPage,
            Items = paged.Items.Select(t => t.ToResponse())
        });
    }
}
