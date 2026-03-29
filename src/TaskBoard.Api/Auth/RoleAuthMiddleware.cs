using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Controllers;
using TaskBoard.Api.Enums;

namespace TaskBoard.Api.Auth;

/// <summary>
/// Reads X-User-Id and X-User-Role headers to build a CurrentUser context,
/// then enforces [RequireRole] on controllers/actions.
///
/// Header contract (documented in Swagger):
///   X-User-Id   : integer   (required on all protected endpoints)
///   X-User-Role : Member | Admin  (defaults to Member when absent)
/// </summary>
public class RoleAuthMiddleware
{
    private readonly RequestDelegate _next;

    public RoleAuthMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Parse caller identity from headers
        if (!context.Request.Headers.TryGetValue("X-User-Id", out var userIdRaw) ||
            !int.TryParse(userIdRaw, out var userId))
        {
            // No identity supplied — check whether the endpoint requires a role.
            // If it does, reject the anonymous request with 403.
            var endpointMeta = context.GetEndpoint();
            if (endpointMeta?.Metadata.GetMetadata<RequireRoleAttribute>() is not null)
            {
                context.Response.StatusCode  = (int)HttpStatusCode.Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "Authentication required. Provide an X-User-Id header."
                }));
                return;
            }

            await _next(context);
            return;
        }

        var roleStr = context.Request.Headers.TryGetValue("X-User-Role", out var roleRaw)
            ? roleRaw.ToString()
            : "Member";

        if (!Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var role))
            role = UserRole.Member;

        var currentUser = new CurrentUser { Id = userId, Role = role };
        context.Items["CurrentUser"] = currentUser;

        // Check [RequireRole] on the matched endpoint
        var endpoint  = context.GetEndpoint();
        var attribute = endpoint?.Metadata.GetMetadata<RequireRoleAttribute>();

        if (attribute is not null)
        {
            if (role < attribute.MinimumRole)
            {
                context.Response.StatusCode  = (int)HttpStatusCode.Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = $"Role '{role}' is not permitted. Required: '{attribute.MinimumRole}' or higher."
                }));
                return;
            }
        }

        await _next(context);
    }
}
