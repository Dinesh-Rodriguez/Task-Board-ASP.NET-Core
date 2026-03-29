# Task Board API

A REST API for managing projects and tasks built with ASP.NET Core 10. Users have roles (Member / Admin), projects contain tasks, tasks follow a defined status workflow.

## Stack

- **ASP.NET Core 10** + EF Core 9 (Pomelo MySQL)
- **FluentValidation** for request validation
- **Serilog** with correlation ID enrichment
- **Swashbuckle** for Swagger UI
- **xUnit** - 75 unit tests + 24 integration tests (SQLite in-memory)

## Getting Started

Requirements: .NET 10 SDK, MySQL 8+

Clone and restore:

    git clone <repo-url>
    cd Task-Board
    dotnet restore TaskBoard.slnx

Set your DB credentials in src/TaskBoard.Api/appsettings.Development.json:

    Server=localhost;Port=3306;Database=TaskBoardDb_Dev;User=root;Password=yourpassword;

Apply migrations and run:

    dotnet tool install --global dotnet-ef
    dotnet ef database update --project src/TaskBoard.Api
    dotnet run --project src/TaskBoard.Api

API runs on http://localhost:5173 - Swagger at http://localhost:5173/swagger

Demo data (Alice/Bob/Carol + 2 projects + 6 tasks) seeds automatically in Development.

## Running Tests

    dotnet test TaskBoard.slnx

Integration tests use SQLite so no database setup needed. All 99 tests should pass.

## Project Structure

    src/TaskBoard.Api/
      Auth/          header-based RBAC middleware
      Controllers/   ProjectsController, TasksController
      Data/          DbContext, EF configs, migrations, DataSeeder
      DTOs/          request/response models
      Middleware/    ExceptionHandlingMiddleware, CorrelationIdMiddleware
      Models/        User, Project, TaskItem, ProjectMember
      Repositories/  generic + domain-specific
      Services/      ProjectService, TaskService, TaskStatusTransitionRules
      Validators/    FluentValidation validators

    tests/
      TaskBoard.UnitTests/        service + validator tests
      TaskBoard.IntegrationTests/ full HTTP pipeline tests

## Endpoints

### Projects

| Method | Endpoint | Role |
|--------|----------|------|
| GET | /api/projects?userId= | - |
| GET | /api/projects/{id} | - |
| POST | /api/projects?ownerId= | Member |
| PUT | /api/projects/{id} | Member |
| PATCH | /api/projects/{id}/archive | Admin |
| DELETE | /api/projects/{id} | Admin |
| GET | /api/projects/{id}/tasks | - |

### Tasks

| Method | Endpoint | Role |
|--------|----------|------|
| GET | /api/tasks?projectId= | - |
| GET | /api/tasks/{id} | - |
| POST | /api/tasks | Member |
| PUT | /api/tasks/{id} | Member |
| PATCH | /api/tasks/{id}/status | - |
| PATCH | /api/tasks/{id}/archive | - |
| DELETE | /api/tasks/{id} | Admin |

GET /api/projects/{id}/tasks supports: status, priority, assigneeId, includeArchived filters + sortBy/sortDir + page/pageSize.

## Auth

Add these headers to requests:

    X-User-Id: 1
    X-User-Role: Admin

X-User-Role defaults to Member if omitted. No X-User-Id on a protected endpoint = 403. Intentionally simple - no JWT, makes local testing easy.

## Task Status Workflow

    Todo -> InProgress -> InReview -> Done
               ^               ^
               +---------------+  (can step back)

    Any state -> Cancelled -> Todo (restore)
    Done -> InProgress (reopen)

Invalid transitions return 422 with the allowed next states listed.

## Error Format

All errors use RFC 7807 Problem Details:

    {
      "type": "https://taskboard.api/errors/not-found",
      "title": "Resource Not Found",
      "status": 404,
      "detail": "Task 42 not found.",
      "traceId": "00-abc...",
      "instance": "/api/tasks/42"
    }

## Design Notes

**Repository + service split** - business logic lives in services, repositories only do data access. Makes services easy to unit test with mocks.

**FluentValidation** - rules are plain classes, straightforward to test, no attribute clutter on models.

**Static transition rules** - a dictionary of From -> allowed Set<To>. One place to update, no allocations at runtime.

**Header auth** - kept simple on purpose. The interesting part of this project is the API design. JWT can replace RoleAuthMiddleware later without touching anything else.

**SQLite in integration tests** - no infra needed, but still tests the real pipeline end-to-end.

## Known Gaps / Trade-offs

- No user or membership management endpoints (users are seeded only)
- Project list has no pagination
- AutoMapper is installed but unused - ended up preferring explicit mapping extensions
- Targets .NET 10 (only SDK available locally); .NET 8 LTS would be the typical choice