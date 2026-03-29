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

## Exploring the API

**Option 1 — Swagger UI** (easiest): open `http://localhost:5173/swagger`, click Authorize, enter `X-User-Id: 1` and `X-User-Role: Admin`, then try any endpoint directly in the browser.

**Option 2 — Postman**: import `TaskBoard.postman_collection.json` from the repo root. All requests have the auth headers pre-set. Collection variables (`baseUrl`, `userId`, `userRole`, `projectId`, `taskId`) let you switch context without editing individual requests. Default user is Alice (Admin, id=1).

**Option 3 — curl quick check**:
```bash
curl http://localhost:5173/api/projects?userId=1 -H "X-User-Id: 1" -H "X-User-Role: Admin"
```

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

## Common Workflows

### Create a task, assign it to a colleague, set priority and due date

Any Member or Admin can do this. Send a `POST /api/tasks` with the colleague's user ID as `assigneeId`:

```http
POST /api/tasks
X-User-Id: 1
X-User-Role: Member
Content-Type: application/json

{
  "title": "Design landing page",
  "description": "Wireframes and mockups for the new landing page",
  "projectId": 1,
  "assigneeId": 2,
  "priority": 2,
  "dueDate": "2026-04-30"
}
```

Priority values: `0` Low · `1` Medium · `2` High · `3` Critical

`dueDate` must be a future date. `assigneeId` and `description` are optional.

With the seeded demo data: user `1` = Alice (Admin), user `2` = Bob (Member), user `3` = Carol (Member).

### Move a task through the workflow

```http
PATCH /api/tasks/1/status
X-User-Id: 1
X-User-Role: Member
Content-Type: application/json

{ "status": 1 }
```

Status values: `0` Todo → `1` InProgress → `2` InReview → `3` Done · `4` Cancelled

Invalid transitions (e.g. jumping Todo straight to Done) return `422` with the list of allowed next states.

### View all tasks in a project — filter and sort as a team lead

Use `GET /api/projects/{id}/tasks` which supports full filtering, sorting, and pagination in one call:

```
# All high-priority tasks, newest first
GET /api/projects/1/tasks?priority=2&sortBy=createdAt&sortDir=desc

# Everything assigned to Bob, sorted by due date
GET /api/projects/1/tasks?assigneeId=2&sortBy=dueDate&sortDir=asc

# Only tasks currently in review
GET /api/projects/1/tasks?status=2

# InProgress tasks assigned to Carol, page 2
GET /api/projects/1/tasks?status=1&assigneeId=3&page=2&pageSize=10
```

Filter params: `status` (0–4), `priority` (0–3), `assigneeId`, `includeArchived` (true/false)  
Sort params: `sortBy` = createdAt · dueDate · priority · status — plus `sortDir` = asc · desc  
Pagination: `page` (default 1), `pageSize` (default 20, max 100)

Response includes `totalCount`, `totalPages`, `hasNextPage` so clients can build pagination controls.

### Update a task's details

```http
PUT /api/tasks/3
X-User-Id: 1
X-User-Role: Member
Content-Type: application/json

{
  "title": "Design landing page",
  "description": "Revised scope — mobile-first only",
  "assigneeId": 3,
  "priority": 3,
  "dueDate": "2026-05-01"
}
```

All fields are optional — only send what you want to change. Status is intentionally excluded from PUT; use `PATCH /api/tasks/{id}/status` to move through the workflow so transitions are always enforced.

### Move a task through workflow stages

```http
PATCH /api/tasks/3/status
X-User-Id: 1
X-User-Role: Member
Content-Type: application/json

{ "status": 1 }
```

Status values: `0` Todo · `1` InProgress · `2` InReview · `3` Done · `4` Cancelled

Allowed paths:

```
Todo → InProgress → InReview → Done
         ↑               ↑
         └───────────────┘  (step back allowed)

Any → Cancelled → Todo  (restore)
Done → InProgress       (reopen)
```

Invalid transitions return `422` with the allowed next states listed in the response.

### Archive completed or cancelled tasks

Archiving hides a task from normal listings without deleting it. History is fully preserved and archived tasks can be retrieved with `includeArchived=true`.

```http
PATCH /api/tasks/3/archive
X-User-Id: 1
X-User-Role: Member
```

No request body needed. Works on any task regardless of status.

To view archived tasks later:

```
GET /api/projects/1/tasks?includeArchived=true
```

Projects can also be archived (Admin only):

```http
PATCH /api/projects/1/archive
X-User-Id: 1
X-User-Role: Admin
```

Nothing is permanently deleted unless you explicitly call `DELETE`.

---

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