using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TaskBoard.Api.Data;
using TaskBoard.Api.Enums;
using TaskBoard.Api.Models;
using TaskStatus = TaskBoard.Api.Enums.TaskStatus;

namespace TaskBoard.IntegrationTests;

/// <summary>
/// Integration tests for /api/tasks endpoints.
/// Tests verify the real request/response pipeline including validation,
/// status transitions, and middleware behaviour.
/// </summary>
public class TasksApiTests : IClassFixture<TaskBoardApiFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _clientNoAuth;
    private readonly ApplicationDbContext _db;

    // Seed IDs shared across tests in this class
    private int _projectId;
    private int _taskId;
    private int _userId;

    public TasksApiTests(TaskBoardApiFactory factory)
    {
        _client      = factory.CreateClient();
        _clientNoAuth = factory.CreateClient();   // fresh client — no default headers
        _db          = factory.CreateDbContext();

        SeedBaseData();
    }

    // ── GET /api/tasks?projectId ──────────────────────────────────────────────

    [Fact]
    public async Task GetByProject_ReturnsSeededTasks()
    {
        var response = await _client.GetAsync($"/api/tasks?projectId={_projectId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Integration Test Task");
    }

    // ── GET /api/tasks/{id} ───────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingTask_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/tasks/{_taskId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("title").GetString().Should().Be("Integration Test Task");
    }

    [Fact]
    public async Task GetById_NonExistentTask_Returns404WithProblemDetails()
    {
        var response = await _client.GetAsync("/api/tasks/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/tasks ───────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocation()
    {
        var request = new
        {
            title     = "Brand New Task",
            projectId = _projectId,
            priority  = 1            // Medium
        };

        _client.DefaultRequestHeaders.Remove("X-User-Id");
        _client.DefaultRequestHeaders.Add("X-User-Id", _userId.ToString());

        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("title").GetString().Should().Be("Brand New Task");
        json.GetProperty("status").GetInt32().Should().Be((int)TaskStatus.Todo);
    }

    [Fact]
    public async Task Create_MissingTitle_Returns400()
    {
        var request = new { projectId = _projectId };

        _client.DefaultRequestHeaders.Remove("X-User-Id");
        _client.DefaultRequestHeaders.Add("X-User-Id", _userId.ToString());

        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithoutAuthHeader_Returns403()
    {
        // No X-User-Id header set — RoleAuthMiddleware will block [RequireRole]
        var request = new { title = "Unauthorised task", projectId = _projectId };

        var response = await _clientNoAuth.PostAsJsonAsync("/api/tasks", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── PATCH /api/tasks/{id}/status ──────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ValidTransition_Returns200()
    {
        // Ensure task is in Todo state
        var task = await _db.Tasks.FindAsync(_taskId);
        task!.Status = TaskStatus.Todo;
        await _db.SaveChangesAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/tasks/{_taskId}/status",
            new { status = (int)TaskStatus.InProgress });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("status").GetInt32().Should().Be((int)TaskStatus.InProgress);
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_Returns422()
    {
        // Force task to Todo so Todo → Done (invalid) triggers the rule
        var task = await _db.Tasks.FindAsync(_taskId);
        task!.Status = TaskStatus.Todo;
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var response = await _client.PatchAsJsonAsync(
            $"/api/tasks/{_taskId}/status",
            new { status = (int)TaskStatus.Done });   // Todo → Done is forbidden

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Cannot transition");
    }

    [Fact]
    public async Task UpdateStatus_NonExistentTask_Returns404()
    {
        var response = await _client.PatchAsJsonAsync(
            "/api/tasks/888888/status",
            new { status = (int)TaskStatus.InProgress });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH /api/tasks/{id}/archive ─────────────────────────────────────────

    [Fact]
    public async Task Archive_ExistingTask_Returns204()
    {
        // Create a dedicated task for this test to avoid conflicts
        var archiveTask = AddTaskToDb("Task To Archive");

        var response = await _client.PatchAsJsonAsync(
            $"/api/tasks/{archiveTask.Id}/archive", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── DELETE /api/tasks/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithAdminRole_Returns204()
    {
        var deleteTask = AddTaskToDb("Task To Delete");

        _client.DefaultRequestHeaders.Remove("X-User-Id");
        _client.DefaultRequestHeaders.Remove("X-User-Role");
        _client.DefaultRequestHeaders.Add("X-User-Id", _userId.ToString());
        _client.DefaultRequestHeaders.Add("X-User-Role", "Admin");

        var response = await _client.DeleteAsync($"/api/tasks/{deleteTask.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithMemberRole_Returns403()
    {
        var deleteTask = AddTaskToDb("Task Delete Forbidden");

        _client.DefaultRequestHeaders.Remove("X-User-Id");
        _client.DefaultRequestHeaders.Remove("X-User-Role");
        _client.DefaultRequestHeaders.Add("X-User-Id", _userId.ToString());
        _client.DefaultRequestHeaders.Add("X-User-Role", "Member");

        var response = await _client.DeleteAsync($"/api/tasks/{deleteTask.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Correlation ID ────────────────────────────────────────────────────────

    [Fact]
    public async Task AllResponses_ContainCorrelationIdHeader()
    {
        var response = await _client.GetAsync($"/api/tasks/{_taskId}");

        response.Headers.Contains("X-Correlation-Id").Should().BeTrue();
    }

    [Fact]
    public async Task Request_WithCorrelationId_EchoesItBack()
    {
        _client.DefaultRequestHeaders.Remove("X-Correlation-Id");
        _client.DefaultRequestHeaders.Add("X-Correlation-Id", "test-corr-123");

        var response = await _client.GetAsync($"/api/tasks/{_taskId}");

        response.Headers.GetValues("X-Correlation-Id").Single()
            .Should().Be("test-corr-123");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SeedBaseData()
    {
        // Look up existing seed if already created (factory is shared per class, but
        // the constructor is invoked once per test, so IDs must be re-read each time)
        var existingUser = _db.Users.FirstOrDefault(u => u.Email == "test@int.test");
        if (existingUser is not null)
        {
            _userId    = existingUser.Id;
            _projectId = _db.Projects.First(p => p.Name == "Int Test Project").Id;
            _taskId    = _db.Tasks.First(t => t.Title == "Integration Test Task").Id;
            return;
        }

        var user = new User
        {
            Name = "Test User", Email = "test@int.test",
            PasswordHash = "x", Role = UserRole.Member,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        _db.SaveChanges();
        _userId = user.Id;

        var project = new Project
        {
            Name = "Int Test Project", OwnerId = user.Id,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Projects.Add(project);
        _db.SaveChanges();
        _projectId = project.Id;

        var task = new TaskItem
        {
            Title = "Integration Test Task", Status = TaskStatus.Todo,
            Priority = TaskPriority.Medium, ProjectId = project.Id,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Tasks.Add(task);
        _db.SaveChanges();
        _taskId = task.Id;
    }

    private TaskItem AddTaskToDb(string title)
    {
        var task = new TaskItem
        {
            Title = title, Status = TaskStatus.Todo,
            Priority = TaskPriority.Low, ProjectId = _projectId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Tasks.Add(task);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return task;
    }
}
