using Microsoft.EntityFrameworkCore;
using TaskBoard.Api.Enums;
using TaskBoard.Api.Models;
using TaskStatus = TaskBoard.Api.Enums.TaskStatus;

namespace TaskBoard.Api.Data;

/// <summary>
/// Seeds demo data so a reviewer can call the API immediately.
/// Runs only when the database is reachable and the Users table is empty.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // Apply any pending migrations (no-op if already up to date)
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database migration could not run (no SQL Server?). Skipping seed.");
            return;
        }

        if (await db.Users.AnyAsync())
        {
            logger.LogInformation("Database already seeded — skipping.");
            return;
        }

        logger.LogInformation("Seeding demo data…");

        // ── Users ──────────────────────────────────────────────────────────────
        var alice = new User
        {
            Name         = "Alice Admin",
            Email        = "alice@taskboard.dev",
            PasswordHash = "demo-hash-alice",   // not a real hash — demo only
            Role         = UserRole.Admin,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };
        var bob = new User
        {
            Name         = "Bob Member",
            Email        = "bob@taskboard.dev",
            PasswordHash = "demo-hash-bob",
            Role         = UserRole.Member,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };
        var carol = new User
        {
            Name         = "Carol Member",
            Email        = "carol@taskboard.dev",
            PasswordHash = "demo-hash-carol",
            Role         = UserRole.Member,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };

        db.Users.AddRange(alice, bob, carol);
        await db.SaveChangesAsync();

        // ── Project ────────────────────────────────────────────────────────────
        var project = new Project
        {
            Name        = "Task Board Demo",
            Description = "A sample project pre-loaded for reviewer testing.",
            OwnerId     = alice.Id,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow
        };

        var secondProject = new Project
        {
            Name        = "Mobile App Redesign",
            Description = "Redesign the mobile application UI — archived for reference.",
            OwnerId     = alice.Id,
            IsArchived  = true,
            CreatedAt   = DateTime.UtcNow.AddMonths(-3),
            UpdatedAt   = DateTime.UtcNow.AddMonths(-1)
        };

        db.Projects.AddRange(project, secondProject);
        await db.SaveChangesAsync();

        // ── Project members ────────────────────────────────────────────────────
        db.ProjectMembers.AddRange(
            new ProjectMember { ProjectId = project.Id, UserId = bob.Id,   Role = UserRole.Member, JoinedAt = DateTime.UtcNow },
            new ProjectMember { ProjectId = project.Id, UserId = carol.Id, Role = UserRole.Member, JoinedAt = DateTime.UtcNow }
        );

        // ── Tasks (one per status + an archived one) ────────────────────────────
        var now = DateTime.UtcNow;
        db.Tasks.AddRange(
            new TaskItem
            {
                Title       = "Set up CI/CD pipeline",
                Description = "Configure GitHub Actions for build, test, and deploy.",
                Status      = TaskStatus.Todo,
                Priority    = TaskPriority.High,
                ProjectId   = project.Id,
                AssigneeId  = bob.Id,
                DueDate     = now.AddDays(14),
                CreatedAt   = now,
                UpdatedAt   = now
            },
            new TaskItem
            {
                Title       = "Implement user authentication",
                Description = "Add JWT-based auth with refresh tokens.",
                Status      = TaskStatus.InProgress,
                Priority    = TaskPriority.Critical,
                ProjectId   = project.Id,
                AssigneeId  = alice.Id,
                DueDate     = now.AddDays(7),
                CreatedAt   = now.AddDays(-3),
                UpdatedAt   = now.AddDays(-1)
            },
            new TaskItem
            {
                Title       = "Design database schema",
                Description = "Define all entity relationships and indexes.",
                Status      = TaskStatus.InReview,
                Priority    = TaskPriority.High,
                ProjectId   = project.Id,
                AssigneeId  = carol.Id,
                DueDate     = now.AddDays(2),
                CreatedAt   = now.AddDays(-10),
                UpdatedAt   = now.AddDays(-2)
            },
            new TaskItem
            {
                Title       = "Write API documentation",
                Description = "Document all endpoints in Swagger/OpenAPI.",
                Status      = TaskStatus.Done,
                Priority    = TaskPriority.Medium,
                ProjectId   = project.Id,
                AssigneeId  = bob.Id,
                DueDate     = now.AddDays(-1),
                CreatedAt   = now.AddDays(-14),
                UpdatedAt   = now.AddDays(-1)
            },
            new TaskItem
            {
                Title       = "Migrate legacy data",
                Description = "One-time migration from the old CSV export — no longer needed.",
                Status      = TaskStatus.Cancelled,
                Priority    = TaskPriority.Low,
                ProjectId   = project.Id,
                DueDate     = now.AddDays(-5),
                CreatedAt   = now.AddDays(-20),
                UpdatedAt   = now.AddDays(-5)
            },
            new TaskItem
            {
                Title       = "Performance benchmarking",
                Description = "Benchmark key endpoints under load — archived after discussion.",
                Status      = TaskStatus.Todo,
                Priority    = TaskPriority.Medium,
                ProjectId   = project.Id,
                AssigneeId  = carol.Id,
                IsArchived  = true,
                CreatedAt   = now.AddDays(-30),
                UpdatedAt   = now.AddDays(-15)
            }
        );

        await db.SaveChangesAsync();

        logger.LogInformation(
            "Seed complete. Users: alice (id={AliceId}), bob (id={BobId}), carol (id={CarolId}). " +
            "Project id={ProjectId}.",
            alice.Id, bob.Id, carol.Id, project.Id);
    }
}
