using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;
using TaskBoard.Api.Auth;
using TaskBoard.Api.Data;
using TaskBoard.Api.Middleware;
using TaskBoard.Api.Repositories;
using TaskBoard.Api.Services;

// Configure Serilog from appsettings before the host is built
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ── Database ──────────────────────────────────────────────────────────────
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // ── Repositories ──────────────────────────────────────────────────────────
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
    builder.Services.AddScoped<ITaskRepository, TaskRepository>();

    // ── Services ──────────────────────────────────────────────────────────────
    builder.Services.AddScoped<IProjectService, ProjectService>();
    builder.Services.AddScoped<ITaskService, TaskService>();

    // ── Controllers + Validation ──────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // ── OpenAPI / Swagger ─────────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        var settings = builder.Configuration.GetSection("ApiSettings");
        var version  = settings["Version"] ?? "v1";

        c.SwaggerDoc(version, new OpenApiInfo
        {
            Title       = settings["Title"] ?? "Task Board API",
            Version     = version,
            Description = "A RESTful API for managing task boards, projects, and team members.\n\n" +
                          "## Authentication\n" +
                          "This API uses simple header-based identity for development/demo purposes.\n\n" +
                          "Supply these headers on requests:\n" +
                          "- **X-User-Id** (integer) — the caller's user ID\n" +
                          "- **X-User-Role** — `Member` (default) or `Admin`\n\n" +
                          "Endpoints marked **[Admin]** require `X-User-Role: Admin`."
        });

        c.AddSecurityDefinition("UserIdHeader", new OpenApiSecurityScheme
        {
            Name        = "X-User-Id",
            In          = ParameterLocation.Header,
            Type        = SecuritySchemeType.ApiKey,
            Description = "Caller user ID (integer)"
        });
        c.AddSecurityDefinition("UserRoleHeader", new OpenApiSecurityScheme
        {
            Name        = "X-User-Role",
            In          = ParameterLocation.Header,
            Type        = SecuritySchemeType.ApiKey,
            Description = "Caller role: Member (default) or Admin"
        });
        c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
        {
            { new OpenApiSecuritySchemeReference("UserIdHeader",   doc), [] },
            { new OpenApiSecuritySchemeReference("UserRoleHeader", doc), [] }
        });

        // Include XML doc comments if present
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
    });

    var app = builder.Build();

    // ── Middleware pipeline ───────────────────────────────────────────────────
    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<RoleAuthMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Board API v1"));
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
