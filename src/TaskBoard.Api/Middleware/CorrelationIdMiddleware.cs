using Serilog.Context;

namespace TaskBoard.Api.Middleware;

/// <summary>
/// Ensures every request has a correlation ID:
///   - Reads "X-Correlation-Id" from incoming request headers (client-supplied).
///   - Generates a new short GUID if the header is absent.
///   - Pushes the ID into Serilog's LogContext so it appears in every log line.
///   - Writes the ID back as "X-Correlation-Id" on the response.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming)
            ? incoming.ToString()
            : Guid.NewGuid().ToString("N")[..12];   // short 12-char prefix for readability

        // Add to response so callers can correlate logs
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(HeaderName, correlationId);
            return Task.CompletedTask;
        });

        // Push into Serilog's log context for the duration of the request
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
