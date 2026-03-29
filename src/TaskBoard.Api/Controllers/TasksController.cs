using Microsoft.AspNetCore.Mvc;
using TaskBoard.Api.Auth;
using TaskBoard.Api.DTOs.Tasks;
using TaskBoard.Api.Enums;
using TaskBoard.Api.Mapping;
using TaskBoard.Api.Services;

namespace TaskBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    // GET /api/tasks?projectId=1
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProject([FromQuery] int projectId, CancellationToken ct)
    {
        var tasks = await _taskService.GetByProjectAsync(projectId, ct);
        return Ok(tasks.Where(t => !t.IsArchived).Select(t => t.ToResponse()));
    }

    // GET /api/tasks/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var task = await _taskService.GetByIdAsync(id, ct);
        if (task is null) return NotFound(new { error = $"Task {id} not found." });
        return Ok(task.ToResponse());
    }

    // POST /api/tasks
    [HttpPost]
    [RequireRole]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var task = await _taskService.CreateAsync(
            request.Title,
            request.Description,
            request.ProjectId,
            request.AssigneeId,
            request.Priority,
            request.DueDate,
            ct);

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task.ToResponse());
    }

    // PUT /api/tasks/{id}
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskRequest request, CancellationToken ct)
    {
        var task = await _taskService.UpdateAsync(
            id,
            request.Title,
            request.Description,
            request.Status,
            request.Priority,
            request.AssigneeId,
            request.DueDate,
            ct);

        return Ok(task.ToResponse());
    }

    // PATCH /api/tasks/{id}/status
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTaskStatusRequest request, CancellationToken ct)
    {
        await _taskService.UpdateStatusAsync(id, request.Status, ct);
        var task = await _taskService.GetByIdAsync(id, ct);
        return Ok(task!.ToResponse());
    }

    // PATCH /api/tasks/{id}/archive
    [HttpPatch("{id:int}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(int id, CancellationToken ct)
    {
        await _taskService.ArchiveAsync(id, ct);
        return NoContent();
    }

    // DELETE /api/tasks/{id}
    [HttpDelete("{id:int}")]
    [RequireRole(UserRole.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _taskService.DeleteAsync(id, ct);
        return NoContent();
    }
}
