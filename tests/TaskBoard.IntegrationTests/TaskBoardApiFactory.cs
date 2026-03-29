using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Api.Data;

namespace TaskBoard.IntegrationTests;

/// <summary>
/// Spins up the real ASP.NET Core pipeline but replaces SQL Server with an
/// in-memory SQLite database so tests run without any external infrastructure.
/// A single SqliteConnection is shared across the test run so the schema is
/// only created once and each test class can access data written by others.
/// </summary>
public class TaskBoardApiFactory : WebApplicationFactory<Program>
{
    // Keep the connection open for the lifetime of the factory so the
    // in-memory SQLite database outlives individual DbContext instances.
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public TaskBoardApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // EF Core's AddDbContext registers TWO descriptors for TContext:
            //   DbContextOptions<TContext>              – the options factory
            //   IDbContextOptionsConfiguration<TContext> – the per-startup options lambda
            // Both must be removed before replacing with the SQLite variant;
            // otherwise the SQL Server lambda still runs inside the new options factory
            // and EF Core sees two conflicting providers.
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                         || d.ServiceType == typeof(ApplicationDbContext)
                         || d.ServiceType == typeof(IDbContextOptionsConfiguration<ApplicationDbContext>))
                .ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            // Register a clean SQLite-backed context using the shared connection
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(_connection));
        });

        // Silence Serilog startup noise and disable data seeding in tests
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
        builder.UseSetting("SeedData:Enabled", "false");
    }

    /// <summary>
    /// Creates (or re-creates) the database schema and returns a scoped DbContext.
    /// Call this from test class constructors to get a fresh-schema database.
    /// </summary>
    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
