using System.Collections.Concurrent;

namespace PhiUsageAnalytics.Api.Middleware;

/// <summary>
/// Simple in-memory rate limiting.
/// Max 100 requests per minute per IP address.
/// Returns 429 Too Many Requests if exceeded.
/// Bypassed when X-Warmup-Key header matches configured key.
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _clients = new();

    private const int MaxRequestsPerMinute = 100;
    private const string WarmupHeaderName = "X-Warmup-Key";
    private const string WarmupSecretKey = "phi-cache-warmup-2026-secure";

    public RateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Only rate-limit API calls
        if (!path.StartsWith("/api/"))
        {
            await _next(context);
            return;
        }

        // Bypass rate limiting for warm-up requests with valid key
        if (context.Request.Headers.TryGetValue(WarmupHeaderName, out var warmupKey)
            && warmupKey == WarmupSecretKey)
        {
            await _next(context);
            return;
        }

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;

        var clientInfo = _clients.GetOrAdd(ipAddress, _ => new RateLimitInfo { WindowStart = now });

        // Reset window if a minute has passed
        if ((now - clientInfo.WindowStart).TotalMinutes >= 1)
        {
            clientInfo.RequestCount = 0;
            clientInfo.WindowStart = now;
        }

        clientInfo.RequestCount++;

        if (clientInfo.RequestCount > MaxRequestsPerMinute)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers.Add("Retry-After", "60");
            await context.Response.WriteAsJsonAsync(new { message = "Too many requests. Please wait a minute." });
            return;
        }

        await _next(context);
    }
}

public class RateLimitInfo
{
    public int RequestCount { get; set; }
    public DateTime WindowStart { get; set; }
}
