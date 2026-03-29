using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskBoard.Api.Middleware;

/// <summary>
/// Catches all unhandled exceptions and returns structured Problem Details responses (RFC 7807).
/// No raw stack traces are ever sent to the client.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy    = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition  = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next   = next;
        _logger = logger;
        _env    = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected — log at a low level and do nothing
            _logger.LogDebug("Request cancelled by client: {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (status, title, type) = ex switch
        {
            KeyNotFoundException      => (HttpStatusCode.NotFound,                  "Resource Not Found",          "not-found"),
            InvalidOperationException => (HttpStatusCode.UnprocessableEntity,        "Business Rule Violation",     "unprocessable"),
            ArgumentNullException     => (HttpStatusCode.BadRequest,                "Bad Request",                  "bad-request"),
            ArgumentException         => (HttpStatusCode.BadRequest,                "Bad Request",                  "bad-request"),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden,               "Forbidden",                   "forbidden"),
            NotSupportedException     => (HttpStatusCode.BadRequest,                "Operation Not Supported",     "not-supported"),
            _                         => (HttpStatusCode.InternalServerError,        "Internal Server Error",       "internal-error")
        };

        var statusCode = (int)status;
        var traceId    = Activity.Current?.Id ?? context.TraceIdentifier;

        // Only expose detail message for known, deliberate exceptions; never expose internals
        var detail = status == HttpStatusCode.InternalServerError && !_env.IsDevelopment()
            ? "An unexpected error occurred. Please try again later."
            : ex.Message;

        var problem = new ProblemDetail
        {
            Type     = $"https://taskboard.api/errors/{type}",
            Title    = title,
            Status   = statusCode,
            Detail   = detail,
            TraceId  = traceId,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode  = statusCode;

        return context.Response.WriteAsync(JsonSerializer.Serialize(problem, _jsonOptions));
    }

    private sealed class ProblemDetail
    {
        public string Type     { get; init; } = string.Empty;
        public string Title    { get; init; } = string.Empty;
        public int    Status   { get; init; }
        public string Detail   { get; init; } = string.Empty;
        public string TraceId  { get; init; } = string.Empty;
        public string Instance { get; init; } = string.Empty;
    }
}
