# Task Board API

A production-oriented RESTful API for managing projects and tasks, built with **ASP.NET Core 10** and **Entity Framework Core 10**. Includes structured logging, request tracing, role-based access control, FluentValidation, and a comprehensive test suite.

---

## Table of Contents

- [Project Overview](#project-overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Quick Start (Docker)](#quick-start-docker)
- [Local Development Setup](#local-development-setup)
- [Running the Tests](#running-the-tests)
- [API Reference](#api-reference)
- [Authentication](#authentication)
- [Status Transition Rules](#status-transition-rules)
- [Configuration Reference](#configuration-reference)
- [Design Decisions](#design-decisions)
- [Assumptions & Trade-offs](#assumptions--trade-offs)
- [Limitations & Future Work](#limitations--future-work)

---

## Project Overview

Task Board is a back-end API that models collaborative project management:

| Concept | Description |
|---|---|
| **User** | Has a role of `Member` or `Admin` |
| **Project** | Owned by a user; members can be added |
| **Task** | Belongs to a project; can be assigned to a user |

Core capabilities:

- Full CRUD for projects and tasks
- Enforced task status workflow (`Todo → InProgress → InReview → Done`, with cancel/reopen paths)
- Role-based access control via request headers
- Pagination, sorting, and filtering on task lists
- Structured JSON logging with per-request correlation IDs
- RFC 7807 Problem Details on all error responses
- Demo data seeding for instant exploration
- 99 automated tests (75 unit + 24 integration)

---

## Architecture

```
d:\Task Board\
├── src/
│   └── TaskBoard.Api/
│       ├── Auth/              # Header-based RBAC middleware + CurrentUser
│       ├── Controllers/       # ProjectsController, TasksController
│       ├── Data/              # EF DbContext, entity configs, migrations, seeder
│       ├── DTOs/              # Request/response models (Projects/, Tasks/)
│       ├── Enums/             # UserRole, TaskStatus, TaskPriority
│       ├── Mapping/           # Hand-written MappingExtensions
│       ├── Middleware/        # ExceptionHandlingMiddleware, CorrelationIdMiddleware
│       ├── Models/            # Domain entities (BaseEntity, User, Project, TaskItem, ProjectMember)
│       ├── Repositories/      # Generic IRepository<T> + domain-specific interfaces/implementations
│       ├── Services/          # IProjectService, ITaskService, TaskStatusTransitionRules
│       ├── Validators/        # FluentValidation validators per request DTO
│       └── Program.cs         # DI setup, middleware pipeline, seeder bootstrap
└── tests/
    ├── TaskBoard.UnitTests/
    │   ├── Services/          # TaskStatusTransitionRulesTests, TaskServiceTests
    │   └── Validators/        # CreateTaskRequestValidatorTests, UpdateTaskStatusRequestValidatorTests
    └── TaskBoard.IntegrationTests/
        ├── TaskBoardApiFactory.cs   # WebApplicationFactory with SQLite override
        ├── TasksApiTests.cs
        └── ProjectsApiTests.cs
```

### Request Pipeline Order

```
Incoming request
  → CorrelationIdMiddleware      (attach / echo X-Correlation-Id)
  → SerilogRequestLogging        (structured HTTP access log)
  → ExceptionHandlingMiddleware  (RFC 7807 Problem Details for all exceptions)
  → RoleAuthMiddleware           (parse X-User-Id / X-User-Role, enforce [RequireRole])
  → Swagger UI (Development only)
  → HTTPS Redirection
  → Authorization
  → Controller
```

---

## Tech Stack

| Layer | Technology | Version |
|---|---|---|
| Runtime | .NET / ASP.NET Core | 10.0 |
| ORM | Entity Framework Core + SQL Server | 10.0.5 |
| Validation | FluentValidation.AspNetCore | 11.3.1 |
| Logging | Serilog (Console + File sinks) | 10.0.0 |
| API Docs | Swashbuckle / OpenAPI | 10.1.7 / 2.4.1 |
| Unit Tests | xUnit + NSubstitute + FluentAssertions | — |
| Integration Tests | xUnit + WebApplicationFactory + SQLite | — |

---

## Prerequisites

### Running with Docker (recommended)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) 4.x+

### Running locally
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server 2019+ (or SQL Server Express / LocalDB / Azure SQL)
- (Optional) `dotnet-ef` global tool for database migrations

---

## Quick Start (Docker)

This is the fastest way to run the full stack — no local .NET SDK or SQL Server required.

```bash
# Clone the repo
git clone <repo-url>
cd "Task Board"

# Start API + SQL Server together
docker compose up --build
```

The API will be available at **http://localhost:8080**  
Swagger UI: **http://localhost:8080/swagger**

> The database is created automatically on first start. Demo data is seeded so you can make API calls immediately.

To stop:
```bash
docker compose down
# To also remove the database volume:
docker compose down -v
```

---

## Local Development Setup

### 1. Clone and restore

```bash
git clone <repo-url>
cd "Task Board"
dotnet restore TaskBoard.slnx
```

### 2. Configure the database connection

Edit `src/TaskBoard.Api/appsettings.Development.json` and set your SQL Server connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TaskBoardDb_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

For **LocalDB**:
```
Server=(localdb)\\mssqllocaldb;Database=TaskBoardDb_Dev;Trusted_Connection=True;
```

### 3. Apply database migrations

```bash
# Install EF CLI tool if not already present
dotnet tool install --global dotnet-ef

# From the solution root
dotnet ef database update --project src/TaskBoard.Api
```

### 4. (Optional) Seed demo data

Demo data is enabled by default in Development via `appsettings.Development.json`:
```json
{
  "SeedData": { "Enabled": true }
}
```

This seeds:
- **3 users**: `alice@taskboard.dev` (Admin), `bob@taskboard.dev` (Member), `carol@taskboard.dev` (Member)
- **2 projects**: one active, one archived
- **6 tasks**: one in each status (`Todo`, `InProgress`, `InReview`, `Done`, `Cancelled`) plus one archived task

### 5. Run the API

```bash
dotnet run --project src/TaskBoard.Api
# or
cd src/TaskBoard.Api && dotnet run
```

The API starts on:
- HTTP: `http://localhost:5173`
- HTTPS: `https://localhost:7002`

Swagger UI: `http://localhost:5173/swagger`

---

## Running the Tests

```bash
# All tests (unit + integration)
dotnet test TaskBoard.slnx

# Unit tests only
dotnet test tests/TaskBoard.UnitTests

# Integration tests only
dotnet test tests/TaskBoard.IntegrationTests

# With coverage report (requires coverlet)
dotnet test TaskBoard.slnx --collect:"XPlat Code Coverage"
```

> **Integration tests use SQLite in-memory** — no SQL Server needed. The `TaskBoardApiFactory` replaces the SQL Server `DbContext` registration at test startup.

Expected result: **99/99 tests passing**

---

## API Reference

> Swagger UI at `/swagger` provides interactive documentation. Auth headers can be set via the 🔓 **Authorize** button.

### Projects

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/projects?userId={id}` | — | List projects owned by or where user is a member |
| `GET` | `/api/projects/{id}` | — | Get a single project with task/member counts |
| `POST` | `/api/projects?ownerId={id}` | Member+ | Create a new project |
| `PUT` | `/api/projects/{id}` | Member+ | Update project name/description |
| `PATCH` | `/api/projects/{id}/archive` | Admin | Archive a project |
| `DELETE` | `/api/projects/{id}` | Admin | Delete a project |
| `GET` | `/api/projects/{id}/tasks` | — | Paged, filtered, sorted task list for a project |

#### Task query parameters (`GET /api/projects/{id}/tasks`)

| Param | Type | Description |
|-------|------|-------------|
| `status` | int (0-4) | Filter by status enum value |
| `priority` | int (0-3) | Filter by priority enum value |
| `assigneeId` | int | Filter by assigned user |
| `includeArchived` | bool | Include archived tasks (default: false) |
| `sortBy` | string | `createdAt` \| `dueDate` \| `priority` \| `status` |
| `sortDir` | string | `asc` \| `desc` |
| `page` | int | Page number (1-based, default: 1) |
| `pageSize` | int | Items per page (1-100, default: 20) |

### Tasks

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/tasks?projectId={id}` | — | List non-archived tasks for a project |
| `GET` | `/api/tasks/{id}` | — | Get a single task |
| `POST` | `/api/tasks` | Member+ | Create a task |
| `PUT` | `/api/tasks/{id}` | Member+ | Full update (enforces status transition rules) |
| `PATCH` | `/api/tasks/{id}/status` | — | Update status only (enforces transition rules) |
| `PATCH` | `/api/tasks/{id}/archive` | — | Archive a task |
| `DELETE` | `/api/tasks/{id}` | Admin | Delete a task |

### Common Response Shapes

**Success** — resource responses include all relevant fields plus `createdAt`, `updatedAt`, `isArchived`.

**Error** — RFC 7807 Problem Details:
```json
{
  "type": "https://taskboard.api/errors/not-found",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Task 42 not found.",
  "traceId": "00-abc123...",
  "instance": "/api/tasks/42"
}
```

**Paged list** (`GET /api/projects/{id}/tasks`):
```json
{
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3,
  "hasPreviousPage": false,
  "hasNextPage": true,
  "items": [ ... ]
}
```

---

## Authentication

This API uses **header-based mock authentication** intended for development and demo purposes only.

| Header | Type | Description |
|--------|------|-------------|
| `X-User-Id` | integer | The caller's user ID |
| `X-User-Role` | string | `Member` or `Admin` (defaults to `Member` if absent) |

**Behaviour:**
- Requests with no `X-User-Id` header are treated as anonymous and blocked on any `[RequireRole]` endpoint with **403 Forbidden**
- `Member` role can create/update resources
- `Admin` role is required to archive/delete resources

**Quick test with the seeded data:**
```
X-User-Id: 1
X-User-Role: Admin
```

---

## Status Transition Rules

Tasks follow an explicit workflow. Arrows show allowed transitions:

```
         ┌──────────────────────────┐
         │                          ↓
   Todo ──→ InProgress ──→ InReview ──→ Done
    ↑           │               │
    │    (back to Todo)    (back to InProgress)
    │           ↓               ↓
    └──── Cancelled ←───────────┘
          │
          └──→ (restore to Todo)
```

| From \ To | Todo | InProgress | InReview | Done | Cancelled |
|-----------|:----:|:----------:|:--------:|:----:|:---------:|
| **Todo** | — | ✅ | ❌ | ❌ | ✅ |
| **InProgress** | ✅ | — | ✅ | ❌ | ✅ |
| **InReview** | ❌ | ✅ | — | ✅ | ✅ |
| **Done** | ❌ | ✅ | ❌ | — | ❌ |
| **Cancelled** | ✅ | ❌ | ❌ | ❌ | — |

Violating these rules returns **422 Unprocessable Entity** with the allowed transitions listed in the error detail.

---

## Configuration Reference

### `appsettings.json` (production defaults)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=TaskBoardDb;..."
  },
  "SeedData": {
    "Enabled": false         // set true in Development to load demo data
  },
  "ApiSettings": {
    "Title": "Task Board API",
    "Version": "v1"
  },
  "Serilog": { ... }         // standard Serilog configuration
}
```

### Environment Variables

All `appsettings.json` values can be overridden via environment variables using the ASP.NET Core double-underscore convention:

```bash
ConnectionStrings__DefaultConnection="Server=..."
SeedData__Enabled=true
ASPNETCORE_ENVIRONMENT=Development
```

---

## Design Decisions

### Repository + Service layering
A generic `IRepository<T>` provides the standard data-access operations. Domain-specific repositories (`IProjectRepository`, `ITaskRepository`) extend it with query-specific methods (e.g. `GetByIdWithDetailsAsync` with eager-loaded navigation properties). Services own all business logic — repositories never throw business exceptions, services always do. This makes services easy to unit-test with mocks.

### Status transitions as a static rule table
`TaskStatusTransitionRules` is a static class holding an immutable dictionary of `From → Set<To>` transitions. Centralising the rules in one place makes them easy to audit, and the static structure means zero allocations at call time. Adding a new transition is a one-line change.

### Header-based mock RBAC
Full JWT authentication was explicitly out of scope for this exercise. The header approach (`X-User-Id`, `X-User-Role`) lets reviewers test RBAC immediately without setting up an identity provider or obtaining tokens. The middleware is isolated enough that swapping it for real JWT bearer auth would require changes only in `RoleAuthMiddleware` and `Program.cs`.

### FluentValidation over DataAnnotations
FluentValidation allows complex, testable validation rules (e.g. "DueDate must be in the future") with no reflection magic. Validators can be tested in isolation; `ShouldHaveValidationErrorFor` / `ShouldNotHaveValidationErrorFor` make intent clear.

### EF Core with explicit configurations
Entity configurations live in `IEntityTypeConfiguration<T>` classes rather than attributes on models. This keeps models clean and makes database schema decisions (max lengths, delete behaviours, default values) explicit and co-located.

### RFC 7807 Problem Details for errors
All exceptions are caught by `ExceptionHandlingMiddleware` and converted to Problem Details responses with consistent fields (`type`, `title`, `status`, `detail`, `traceId`, `instance`). Controllers never return inconsistently-shaped error bodies. Internal server errors in Production never expose raw exception messages.

### SQLite for integration tests
Integration tests use `WebApplicationFactory<Program>` with `ConfigureTestServices` to replace the SQL Server `DbContext` with an in-memory SQLite database. This means tests run fast with no external dependencies, while still exercising the full request pipeline — middleware, routing, validation, service, and repository layers.

### Correlation IDs
`CorrelationIdMiddleware` generates or propagates an `X-Correlation-Id` header and pushes it into Serilog's `LogContext`. Every log line and every error response carries the same correlation ID, making distributed debugging tractable.

---

## Assumptions & Trade-offs

| Decision | Assumption Made | Trade-off |
|----------|----------------|-----------|
| **No real auth** | Reviewer wants to test RBAC without token setup | Not production-ready; trivially bypassable |
| **SQL Server only** | Target environment has SQL Server | Changing to PostgreSQL requires only a package swap and new provider in `Program.cs` |
| **Integer IDs** | Simple, sequential identity is sufficient | GUIDs are better for distributed systems or URL-safe IDs |
| **No pagination on project list** | Projects per user are expected to be small | Should be paginated if users can own thousands of projects |
| **No soft-delete for users** | User deletion not in scope | Deleting a user with owned projects will fail at the FK constraint |
| **Manual mapping over AutoMapper** | Explicit `MappingExtensions` are more discoverable | AutoMapper is installed but unused; either complete the adoption or remove the package |
| **EF change tracking on save** | All saves go through the same `DbContext` scope | Long-running scopes could cause stale reads; would need `AsNoTracking()` queries added for read-heavy paths |
| **.NET 10** | .NET 10 SDK was the only runtime available at build time | The project targets `net10.0`; `.NET 8 LTS` would be more typical for a submission |

---

## Limitations & Future Work

### Known limitations
- **No real authentication** — the header-based RBAC is a demo pattern only
- **No pagination on `GET /api/projects`** — full list is returned
- **No user management API** — users are seeded but there are no `POST /api/users` or `GET /api/users` endpoints
- **No project membership API** — members are seeded but cannot be added/removed via the API
- **HTTPS redirect may warn** in the test host as it cannot determine the HTTPS port — this is a test infrastructure issue, not application behaviour

### Future improvements

**Security**
- Replace mock headers with JWT Bearer authentication (ASP.NET Core `AddAuthentication` + `AddJwtBearer`)
- Add refresh token support
- Rate limiting (`AddRateLimiter`)
- Input sanitisation for description fields

**Features**
- `POST /api/users` — user registration
- `POST /api/projects/{id}/members` — add/remove project members
- Task comments / activity log
- File attachments on tasks
- Due date reminders / notifications (background job with `IHostedService`)
- Webhook support for state changes

**Infrastructure**
- PostgreSQL support (replace `UseSqlServer` with `UseNpgsql`)
- Redis cache for read-heavy project/task queries
- Health check endpoint (`/health`) with database connectivity probe
- Structured output beyond Serilog File — ship to OpenTelemetry / Seq / Datadog
- `dotnet publish` container image to Docker Hub / GitHub Container Registry via CI workflow

**Code quality**
- Complete AutoMapper adoption or remove the package
- Add `AsNoTracking()` to all read-only repository queries
- Add `CancellationToken` propagation into database operations already in place — verify controller timeout behaviour
- Increase integration test coverage for filtering/sorting combinations
- Mutation testing (`Stryker.NET`)

**Documentation**
- Annotate all controller actions with XML doc comments (`/// <summary>`) so Swagger shows per-endpoint descriptions
</content>
</invoke>