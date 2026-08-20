using PhiUsageAnalytics.Api.Services;

namespace PhiUsageAnalytics.Api.Middleware;

/// <summary>
/// Global exception handler.
/// Catches all unhandled exceptions, logs them to file, and returns a clean 500 response.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ErrorLogger errorLogger)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var endpoint = $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}";
            errorLogger.LogError(endpoint, ex);

            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Something went wrong. Please try again.",
                error = ex.GetType().Name
            });
        }
    }
}
