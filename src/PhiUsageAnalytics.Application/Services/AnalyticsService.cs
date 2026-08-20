using PhiUsageAnalytics.Application.DTOs;
using PhiUsageAnalytics.Application.Interfaces;

namespace PhiUsageAnalytics.Application.Services;

/// <summary>
/// Service layer for analytics operations.
/// Thin layer — delegates to repository. Exists for future business logic if needed.
/// </summary>
public class AnalyticsService
{
    private readonly IAnalyticsRepository _repository;

    public AnalyticsService(IAnalyticsRepository repository)
    {
        _repository = repository;
    }

    public Task<OrganizationSummaryDto?> GetOrganizationSummaryAsync(string organizationId, DateTime? fromDate, DateTime? toDate)
    {
        return _repository.GetOrganizationSummaryAsync(organizationId, fromDate, toDate);
    }

    public Task<PagedResultDto<LicenseUsageDto>> GetLicenseUsageAsync(string organizationId, DateTime? fromDate, DateTime? toDate, string? searchKey, string? status, string? sortBy, string? sortDir, int page, int pageSize)
    {
        return _repository.GetLicenseUsageAsync(organizationId, fromDate, toDate, searchKey, status, sortBy, sortDir, page, pageSize);
    }

    public Task<List<GradeUsageDto>> GetGradeUsageAsync(string licenseKey, DateTime? fromDate, DateTime? toDate)
    {
        return _repository.GetGradeUsageAsync(licenseKey, fromDate, toDate);
    }

    public Task<List<SubjectUsageDto>> GetSubjectUsageAsync(string licenseKey, string gradeId, DateTime? fromDate, DateTime? toDate)
    {
        return _repository.GetSubjectUsageAsync(licenseKey, gradeId, fromDate, toDate);
    }

    public Task<List<ChapterUsageDto>> GetChapterUsageAsync(string licenseKey, string gradeId, string subjectId, DateTime? fromDate, DateTime? toDate)
    {
        return _repository.GetChapterUsageAsync(licenseKey, gradeId, subjectId, fromDate, toDate);
    }

    public Task<List<TopicUsageDto>> GetTopicUsageAsync(string licenseKey, string chapterId, DateTime? fromDate, DateTime? toDate)
    {
        return _repository.GetTopicUsageAsync(licenseKey, chapterId, fromDate, toDate);
    }

    public Task<List<DailyUsageTrendDto>> GetDailyTrendAsync(string organizationId, DateTime? fromDate, DateTime? toDate)
    {
        return _repository.GetDailyTrendAsync(organizationId, fromDate, toDate);
    }

    public Task<List<HeatmapDto>> GetHeatmapAsync(string organizationId, int days)
    {
        return _repository.GetHeatmapAsync(organizationId, days);
    }

    public Task<List<PopularTopicDto>> GetPopularTopicsAsync(string organizationId)
    {
        return _repository.GetPopularTopicsAsync(organizationId);
    }

    public Task<List<PopularTopicDto>> GetLeastEngagedTopicsAsync(string organizationId)
    {
        return _repository.GetLeastEngagedTopicsAsync(organizationId);
    }
}
