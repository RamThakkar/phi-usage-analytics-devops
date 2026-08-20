using PhiUsageAnalytics.Api.Services;

namespace PhiUsageAnalytics.Api.Middleware;

/// <summary>
/// Validates session token on all API requests except login.
/// Returns 401 Unauthorized if no valid token.
/// </summary>
public class AuthMiddleware
{
    private readonly RequestDelegate _next;

    public AuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, SessionService sessionService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Skip auth for:
        // - Static files (index.html, favicon, css, js)
        // - Login endpoint
        if (!path.StartsWith("/api/") || path.EndsWith("/login"))
        {
            await _next(context);
            return;
        }

        // Extract token from Authorization header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "Unauthorized. Please login." });
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        var session = sessionService.ValidateToken(token);

        if (session == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "Session expired. Please login again." });
            return;
        }

        // Store session info in HttpContext for controllers to use if needed
        context.Items["Session"] = session;
        context.Items["OrganizationId"] = session.OrganizationId;

        await _next(context);
    }
}
