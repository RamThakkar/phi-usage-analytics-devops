using PhiUsageAnalytics.Application.DTOs;

namespace PhiUsageAnalytics.Application.Interfaces;

/// <summary>
/// Repository interface for all analytics data queries.
/// Single repository — keeps it simple for a read-only reporting project.
/// </summary>
public interface IAnalyticsRepository
{
    /// <summary>Get organization summary (total sessions, video/sim split).</summary>
    Task<OrganizationSummaryDto?> GetOrganizationSummaryAsync(string organizationId, DateTime? fromDate, DateTime? toDate);

    /// <summary>Get license-wise usage for an organization (paginated + searchable + filterable + sortable).</summary>
    Task<PagedResultDto<LicenseUsageDto>> GetLicenseUsageAsync(string organizationId, DateTime? fromDate, DateTime? toDate, string? searchKey, string? status, string? sortBy, string? sortDir, int page, int pageSize);

    /// <summary>Get grade-wise usage for a specific license.</summary>
    Task<List<GradeUsageDto>> GetGradeUsageAsync(string licenseKey, DateTime? fromDate, DateTime? toDate);

    /// <summary>Get subject-wise usage for a license within a grade.</summary>
    Task<List<SubjectUsageDto>> GetSubjectUsageAsync(string licenseKey, string gradeId, DateTime? fromDate, DateTime? toDate);

    /// <summary>Get chapter-wise usage for a license within a subject.</summary>
    Task<List<ChapterUsageDto>> GetChapterUsageAsync(string licenseKey, string gradeId, string subjectId, DateTime? fromDate, DateTime? toDate);

    /// <summary>Get topic-wise usage for a license within a chapter.</summary>
    Task<List<TopicUsageDto>> GetTopicUsageAsync(string licenseKey, string chapterId, DateTime? fromDate, DateTime? toDate);

    /// <summary>Get daily usage trend for an organization.</summary>
    Task<List<DailyUsageTrendDto>> GetDailyTrendAsync(string organizationId, DateTime? fromDate, DateTime? toDate);

    /// <summary>Get usage heatmap (hour × day of week) for an organization.</summary>
    Task<List<HeatmapDto>> GetHeatmapAsync(string organizationId, int days);

    /// <summary>Get most popular topics across all licenses.</summary>
    Task<List<PopularTopicDto>> GetPopularTopicsAsync(string organizationId);

    /// <summary>Get least engaged topics (opened 3+ times, always &lt; 60s).</summary>
    Task<List<PopularTopicDto>> GetLeastEngagedTopicsAsync(string organizationId);
}
