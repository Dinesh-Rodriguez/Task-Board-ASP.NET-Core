using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TaskBoard.Api.Data;
using TaskBoard.Api.Enums;
using TaskBoard.Api.Models;

namespace TaskBoard.IntegrationTests;

/// <summary>
/// Integration tests for /api/projects endpoints.
/// </summary>
public class ProjectsApiTests : IClassFixture<TaskBoardApiFactory>
{
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _db;

    private int _ownerId;
    private int _projectId;

    public ProjectsApiTests(TaskBoardApiFactory factory)
    {
        _client = factory.CreateClient();
        _db     = factory.CreateDbContext();

        SeedBaseData();

        // Set auth headers for all requests in this class
        _client.DefaultRequestHeaders.Remove("X-User-Id");
        _client.DefaultRequestHeaders.Add("X-User-Id", _ownerId.ToString());
    }

    // ── GET /api/projects ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ByOwner_ReturnsOwnedProjects()
    {
        var response = await _client.GetAsync($"/api/projects?userId={_ownerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Projects Integration Project");
    }

    [Fact]
    public async Task GetAll_UnrelatedUser_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/projects?userId=999999");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetArrayLength().Should().Be(0);
    }

    // ── GET /api/projects/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingProject_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/projects/{_projectId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("id").GetInt32().Should().Be(_projectId);
        json.GetProperty("ownerId").GetInt32().Should().Be(_ownerId);
    }

    [Fact]
    public async Task GetById_UnknownProject_Returns404()
    {
        var response = await _client.GetAsync("/api/projects/888777");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/projects ────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201()
    {
        var request = new { name = "New Integration Project", description = "Created by test" };

        var response = await _client.PostAsJsonAsync(
            $"/api/projects?ownerId={_ownerId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("name").GetString().Should().Be("New Integration Project");
        json.GetProperty("isArchived").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Create_EmptyName_Returns400()
    {
        var request = new { name = "", description = "Bad request" };

        var response = await _client.PostAsJsonAsync(
            $"/api/projects?ownerId={_ownerId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/projects/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task Update_ValidRequest_Returns200WithNewName()
    {
        var request = new { name = "Renamed Project", description = "Updated" };

        var response = await _client.PutAsJsonAsync($"/api/projects/{_projectId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("name").GetString().Should().Be("Renamed Project");
    }

    // ── PATCH /api/projects/{id}/archive ──────────────────────────────────────

    [Fact]
    public async Task Archive_WithAdminRole_Returns204()
    {
        var archiveProject = AddProjectToDb("Project To Archive");

        _client.DefaultRequestHeaders.Remove("X-User-Role");
        _client.DefaultRequestHeaders.Add("X-User-Role", "Admin");

        var response = await _client.PatchAsJsonAsync(
            $"/api/projects/{archiveProject.Id}/archive", new { });

        _client.DefaultRequestHeaders.Remove("X-User-Role");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── GET /api/projects/{id}/tasks ──────────────────────────────────────────

    [Fact]
    public async Task GetTasks_ReturnsPagedResponse()
    {
        var response = await _client.GetAsync(
            $"/api/projects/{_projectId}/tasks?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        json.GetProperty("page").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetTasks_FilterByStatus_ReturnsOnlyMatchingTasks()
    {
        // Add a task with InProgress status to the project
        var task = new TaskItem
        {
            Title = "Filtered Task", Status = TaskBoard.Api.Enums.TaskStatus.InProgress,
            Priority = TaskPriority.High, ProjectId = _projectId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var response = await _client.GetAsync(
            $"/api/projects/{_projectId}/tasks?status=1");  // 1 = InProgress

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = json.GetProperty("items");
        items.GetArrayLength().Should().BeGreaterThan(0);

        foreach (var item in items.EnumerateArray())
            item.GetProperty("status").GetInt32().Should().Be(1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SeedBaseData()
    {
        // Re-read IDs from DB each time the constructor runs (once per test method)
        var existingOwner = _db.Users.FirstOrDefault(u => u.Email == "proj-owner@int.test");
        if (existingOwner is not null)
        {
            _ownerId   = existingOwner.Id;
            _projectId = _db.Projects.First(p => p.OwnerId == _ownerId && !p.IsArchived).Id;
            return;
        }

        var owner = new User
        {
            Name = "Proj Owner", Email = "proj-owner@int.test",
            PasswordHash = "x", Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(owner);
        _db.SaveChanges();
        _ownerId = owner.Id;

        var project = new Project
        {
            Name = "Projects Integration Project", OwnerId = owner.Id,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Projects.Add(project);
        _db.SaveChanges();
        _projectId = project.Id;
    }

    private Project AddProjectToDb(string name)
    {
        var project = new Project
        {
            Name = name, OwnerId = _ownerId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Projects.Add(project);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return project;
    }
}
