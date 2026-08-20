using Microsoft.AspNetCore.Mvc;
using PhiUsageAnalytics.Api.Services;
using PhiUsageAnalytics.Application.DTOs;
using PhiUsageAnalytics.Application.Interfaces;
using PhiUsageAnalytics.Application.Services;

namespace PhiUsageAnalytics.Api.Controllers;

/// <summary>
/// API endpoints for the usage analytics dashboard.
/// All endpoints are read-only (GET) except login (POST).
/// Protected by token-based auth (except login endpoint).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly AnalyticsService _service;
    private readonly SessionService _sessionService;
    private readonly IConfiguration _configuration;
    private readonly VisitorLogger _visitorLogger;

    public AnalyticsController(AnalyticsService service, SessionService sessionService, IConfiguration configuration, VisitorLogger visitorLogger)
    {
        _service = service;
        _sessionService = sessionService;
        _configuration = configuration;
        _visitorLogger = visitorLogger;
    }

    /// <summary>
    /// Validate login credentials from appsettings mapping.
    /// Returns a session token on success.
    /// Protected by login lockout (5 failed attempts = 15 min block).
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Check lockout
        if (_sessionService.IsLockedOut(ipAddress))
        {
            return StatusCode(429, new LoginResponseDto
            {
                Success = false,
                Message = "Too many failed attempts. Please try again after 15 minutes."
            });
        }

        // Validate against appsettings
        var users = _configuration.GetSection("AnalyticsUsers").Get<List<AnalyticsUserConfig>>();
        var user = users?.FirstOrDefault(u =>
            u.Username.Equals(request.Username.Trim(), StringComparison.OrdinalIgnoreCase) &&
            u.Password == request.Password.Trim());

        if (user == null)
        {
            var locked = _sessionService.RecordFailedAttempt(ipAddress);
            var message = locked
                ? "Account locked for 15 minutes due to too many failed attempts."
                : "Invalid username or password.";
            _visitorLogger.LogFailedLogin(request.Username.Trim(), ipAddress);
            return Unauthorized(new LoginResponseDto { Success = false, Message = message });
        }

        // Get org name from config (no DB call needed)
        var orgName = user.OrganizationName;

        // Success — create session token
        _sessionService.ClearFailedAttempts(ipAddress);
        var token = _sessionService.CreateSession(user.OrganizationId, orgName);
        _visitorLogger.LogLogin(user.Username, orgName, ipAddress);

        return Ok(new
        {
            Success = true,
            OrganizationId = user.OrganizationId,
            OrganizationName = orgName,
            Token = token
        });
    }

    /// <summary>
    /// Logout — invalidate session token.
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            var session = _sessionService.ValidateToken(token);
            if (session != null)
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                _visitorLogger.LogLogout(session.OrganizationName, session.OrganizationName, ip);
            }
            _sessionService.RemoveSession(token);
        }
        return Ok(new { message = "Logged out successfully." });
    }

    /// <summary>
    /// Clear Redis cache for an organization (force refresh from DB).
    /// </summary>
    [HttpPost("organization/{organizationId}/refresh-cache")]
    public async Task<IActionResult> RefreshCache(string organizationId, [FromServices] ICacheService cache)
    {
        await cache.RemoveByPrefixAsync($"org:{organizationId}:");
        await cache.RemoveByPrefixAsync($"license:");
        return Ok(new { message = "Cache cleared. Next request will fetch fresh data." });
    }

    /// <summary>
    /// Get organization-level summary (total licenses, sessions, video/sim time).
    /// </summary>
    [HttpGet("organization/{organizationId}/summary")]
    public async Task<IActionResult> GetOrganizationSummary(
        string organizationId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _service.GetOrganizationSummaryAsync(organizationId, fromDate, toDate);
        if (result == null)
            return NotFound(new { message = "Organization not found." });

        return Ok(result);
    }

    /// <summary>
    /// Get license-wise usage breakdown for an organization (paginated + searchable + filterable + sortable).
    /// </summary>
    [HttpGet("organization/{organizationId}/licenses")]
    public async Task<IActionResult> GetLicenseUsage(
        string organizationId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetLicenseUsageAsync(organizationId, fromDate, toDate, search, status, sortBy, sortDir, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get grade-wise usage for a specific license key.
    /// </summary>
    [HttpGet("license/{licenseKey}/grades")]
    public async Task<IActionResult> GetGradeUsage(
        string licenseKey,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _service.GetGradeUsageAsync(licenseKey, fromDate, toDate);
        return Ok(result);
    }

    /// <summary>
    /// Get subject-wise usage for a license within a specific grade.
    /// </summary>
    [HttpGet("license/{licenseKey}/grades/{gradeId}/subjects")]
    public async Task<IActionResult> GetSubjectUsage(
        string licenseKey,
        string gradeId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _service.GetSubjectUsageAsync(licenseKey, gradeId, fromDate, toDate);
        return Ok(result);
    }

    /// <summary>
    /// Get chapter-wise usage for a license within a grade and subject.
    /// </summary>
    [HttpGet("license/{licenseKey}/grades/{gradeId}/subjects/{subjectId}/chapters")]
    public async Task<IActionResult> GetChapterUsage(
        string licenseKey,
        string gradeId,
        string subjectId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _service.GetChapterUsageAsync(licenseKey, gradeId, subjectId, fromDate, toDate);
        return Ok(result);
    }

    /// <summary>
    /// Get topic-wise usage for a license within a chapter.
    /// </summary>
    [HttpGet("license/{licenseKey}/chapters/{chapterId}/topics")]
    public async Task<IActionResult> GetTopicUsage(
        string licenseKey,
        string chapterId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _service.GetTopicUsageAsync(licenseKey, chapterId, fromDate, toDate);
        return Ok(result);
    }

    /// <summary>
    /// Get daily usage trend for an organization (for line chart).
    /// </summary>
    [HttpGet("organization/{organizationId}/trend")]
    public async Task<IActionResult> GetDailyTrend(
        string organizationId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _service.GetDailyTrendAsync(organizationId, fromDate, toDate);
        return Ok(result);
    }

    /// <summary>
    /// Export license usage data as CSV (for Excel).
    /// </summary>
    [HttpGet("license/{licenseKey}/export")]
    public async Task<IActionResult> ExportLicenseUsage(
        string licenseKey,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var grades = await _service.GetGradeUsageAsync(licenseKey, fromDate, toDate);

        var csv = "Grade,Sessions,Total Time (sec),Video Time (sec),Simulation Time (sec),Subjects,Last Usage\n";
        foreach (var g in grades)
        {
            csv += $"{g.GradeName},{g.TotalSessions},{g.TotalUsageSeconds},{g.VideoUsageSeconds},{g.SimulationUsageSeconds},{g.SubjectCount},{g.LastUsageDate:yyyy-MM-dd}\n";
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", $"usage-report-{licenseKey}.csv");
    }

    /// <summary>
    /// Get inactive licenses (not used in last N days).
    /// </summary>
    [HttpGet("organization/{organizationId}/inactive-licenses")]
    public async Task<IActionResult> GetInactiveLicenses(
        string organizationId,
        [FromQuery] int days = 7)
    {
        var licenses = await _service.GetLicenseUsageAsync(organizationId, null, null, null, null, null, null, 1, 1000);
        var cutoffDate = DateTime.Now.AddDays(-days);
        var inactive = licenses.Items
            .Where(l => l.HasUsageData && (l.LastUsageDate == null || l.LastUsageDate < cutoffDate))
            .Select(l => new { l.LicenseKey, l.ConsumerName, l.Platform, l.LastUsageDate, DaysInactive = l.LastUsageDate.HasValue ? (int)(DateTime.Now - l.LastUsageDate.Value).TotalDays : 999 })
            .OrderByDescending(l => l.DaysInactive)
            .ToList();

        return Ok(new { days, count = inactive.Count, licenses = inactive });
    }
    /// Get comparative analysis (this period vs previous period).
    /// </summary>
    [HttpGet("organization/{organizationId}/comparison")]
    public async Task<IActionResult> GetComparison(
        string organizationId,
        [FromQuery] int days = 30,
        [FromServices] ICacheService cache = null!)
    {
        try
        {
            // Check Redis cache first
            var cacheKey = $"org:{organizationId}:comparison:{days}";
            var cached = await cache.GetAsync<object>(cacheKey);
            if (cached != null) return Ok(cached);

            var now = DateTime.UtcNow;
            var currentStart = now.AddDays(-days);
            var previousStart = currentStart.AddDays(-days);

            var current = await _service.GetOrganizationSummaryAsync(organizationId, currentStart, now);
            var previous = await _service.GetOrganizationSummaryAsync(organizationId, previousStart, currentStart);

            double sessionsChange = 0, timeChange = 0, videoChange = 0, simChange = 0;
            if (current != null && previous != null)
            {
                sessionsChange = CalculateChange(current.TotalSessions, previous.TotalSessions);
                timeChange = CalculateChange(current.TotalUsageSeconds, previous.TotalUsageSeconds);
                videoChange = CalculateChange(current.VideoUsageSeconds, previous.VideoUsageSeconds);
                simChange = CalculateChange(current.SimulationUsageSeconds, previous.SimulationUsageSeconds);
            }

            var result = new
            {
                period = $"Last {days} days vs Previous {days} days",
                current = new { TotalSessions = current?.TotalSessions ?? 0, TotalUsageSeconds = current?.TotalUsageSeconds ?? 0, VideoUsageSeconds = current?.VideoUsageSeconds ?? 0, SimulationUsageSeconds = current?.SimulationUsageSeconds ?? 0 },
                previous = new { TotalSessions = previous?.TotalSessions ?? 0, TotalUsageSeconds = previous?.TotalUsageSeconds ?? 0, VideoUsageSeconds = previous?.VideoUsageSeconds ?? 0, SimulationUsageSeconds = previous?.SimulationUsageSeconds ?? 0 },
                change = new { sessions = sessionsChange, totalTime = timeChange, video = videoChange, simulation = simChange }
            };

            await cache.SetAsync(cacheKey, result);
            return Ok(result);
        }
        catch
        {
            return Ok(new { period = "", current = new { TotalSessions = 0, TotalUsageSeconds = 0, VideoUsageSeconds = 0, SimulationUsageSeconds = 0 }, previous = new { TotalSessions = 0, TotalUsageSeconds = 0, VideoUsageSeconds = 0, SimulationUsageSeconds = 0 }, change = new { sessions = 0.0, totalTime = 0.0, video = 0.0, simulation = 0.0 } });
        }
    }

    /// <summary>
    /// Get top grades by usage.
    /// </summary>
    [HttpGet("organization/{organizationId}/top-bottom")]
    public async Task<IActionResult> GetTopBottom(string organizationId, [FromServices] ICacheService cache)
    {
        try
        {
            var cacheKey = $"org:{organizationId}:top-bottom";
            var cached = await cache.GetAsync<object>(cacheKey);
            if (cached != null) return Ok(cached);

            var licenses = await _service.GetLicenseUsageAsync(organizationId, null, null, null, "with_usage", null, null, 1, 1000);
            var allGrades = new List<Application.DTOs.GradeUsageDto>();

            var topLicenses = licenses.Items.Take(5).ToList();
            foreach (var lic in topLicenses)
            {
                var grades = await _service.GetGradeUsageAsync(lic.LicenseKey, null, null);
                allGrades.AddRange(grades);
            }

            var gradeRanking = allGrades
                .GroupBy(g => g.GradeName ?? g.GradeId)
                .Select(g => new { Grade = g.Key, TotalSeconds = g.Sum(x => x.TotalUsageSeconds), Sessions = g.Sum(x => x.TotalSessions) })
                .OrderByDescending(g => g.TotalSeconds)
                .ToList();

            var result = new
            {
                topGrades = gradeRanking.Take(5),
                bottomGrades = gradeRanking.TakeLast(5).Reverse()
            };

            await cache.SetAsync(cacheKey, result);
            return Ok(result);
        }
        catch
        {
            return Ok(new { topGrades = new object[0], bottomGrades = new object[0] });
        }
    }

    /// <summary>
    /// Get usage heatmap data (hour of day × day of week).
    /// </summary>
    [HttpGet("organization/{organizationId}/heatmap")]
    public async Task<IActionResult> GetHeatmap(string organizationId, [FromQuery] int days = 30)
    {
        var result = await _service.GetHeatmapAsync(organizationId, days);
        return Ok(result);
    }

    /// <summary>
    /// Get most popular topics across all licenses (by total usage time).
    /// </summary>
    [HttpGet("organization/{organizationId}/popular-topics")]
    public async Task<IActionResult> GetPopularTopics(string organizationId)
    {
        var result = await _service.GetPopularTopicsAsync(organizationId);
        return Ok(result);
    }

    /// <summary>
    /// Get least engaged topics (opened 3+ times but always less than 60 seconds).
    /// </summary>
    [HttpGet("organization/{organizationId}/least-engaged-topics")]
    public async Task<IActionResult> GetLeastEngagedTopics(string organizationId)
    {
        var result = await _service.GetLeastEngagedTopicsAsync(organizationId);
        return Ok(result);
    }

    /// <summary>
    /// Calculates percentage change correctly. Returns 0 if both are 0 or data is meaningless.
    /// </summary>
    private static double CalculateChange(int current, int previous)
    {
        if (current == 0 && previous == 0) return 0;
        if (previous == 0 && current > 0) return 100; // New activity (show as 100% new)
        if (previous == 0) return 0;
        return Math.Round(((double)current - previous) / previous * 100, 1);
    }
}


/// <summary>
/// Config model for analytics users defined in appsettings.
/// </summary>
public class AnalyticsUserConfig
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
}
