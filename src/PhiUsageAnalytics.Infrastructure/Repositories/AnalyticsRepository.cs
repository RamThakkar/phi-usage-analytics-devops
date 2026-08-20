using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PhiUsageAnalytics.Application.DTOs;
using PhiUsageAnalytics.Application.Interfaces;
using PhiUsageAnalytics.Infrastructure.Data;

namespace PhiUsageAnalytics.Infrastructure.Repositories;

/// <summary>
/// Analytics queries with Redis caching.
/// All data cached until midnight. First request of the day hits DB, rest served from Redis.
/// Platform-aware ContentTypeId mapping for Android licenses activated before cutoff date.
/// </summary>
public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly SyllabusDbContext _context;
    private readonly ICacheService _cache;
    private readonly IConfiguration _configuration;

    private const int DefaultLanguageId = 1;

    public AnalyticsRepository(SyllabusDbContext context, ICacheService cache, IConfiguration configuration)
    {
        _context = context;
        _cache = cache;
        _configuration = configuration;
    }

    public async Task<OrganizationSummaryDto?> GetOrganizationSummaryAsync(string organizationId, DateTime? fromDate, DateTime? toDate)
    {
        // Cache only when no date filter
        var cacheKey = $"org:{organizationId}:summary";
        if (fromDate == null && toDate == null)
        {
            var cached = await _cache.GetAsync<OrganizationSummaryDto>(cacheKey);
            if (cached != null) return cached;
        }

        // Get license keys + activation info
        var licenseData = await GetLicenseActivationData(organizationId);
        var licenseKeys = licenseData.Select(l => l.Key).ToList();

        if (!licenseKeys.Any())
        {
            return new OrganizationSummaryDto { OrganizationId = organizationId, TotalLicenses = 0 };
        }

        var cutoffDate = GetCutoffDate();

        // Get raw usage grouped by license + contentType
        var query = _context.PanelUsageDatas.AsNoTracking()
            .Where(p => licenseKeys.Contains(p.LicenseKey!));
        query = ApplyDateFilter(query, fromDate, toDate);

        var rawData = await query
            .GroupBy(p => new { p.LicenseKey, p.ContentTypeId })
            .Select(g => new { LicenseKey = g.Key.LicenseKey!, ContentTypeId = g.Key.ContentTypeId, Seconds = g.Sum(p => p.UsageTime), Count = g.Count() })
            .ToListAsync();

        // Apply platform-aware mapping
        int totalSessions = 0, totalVideo = 0, totalSim = 0;
        foreach (var license in licenseData)
        {
            var records = rawData.Where(r => r.LicenseKey == license.Key).ToList();
            var (videoId, simId) = GetContentTypeIds(license.Platform, license.ActivatedDate, cutoffDate);
            totalSessions += records.Sum(r => r.Count);
            totalVideo += records.Where(r => r.ContentTypeId == videoId).Sum(r => r.Seconds);
            totalSim += records.Where(r => r.ContentTypeId == simId).Sum(r => r.Seconds);
        }

        var result = new OrganizationSummaryDto
        {
            OrganizationId = organizationId,
            OrganizationName = "",
            TotalLicenses = licenseKeys.Count,
            TotalSessions = totalSessions,
            TotalUsageSeconds = totalVideo + totalSim,
            VideoUsageSeconds = totalVideo,
            SimulationUsageSeconds = totalSim
        };

        if (fromDate == null && toDate == null)
            await _cache.SetAsync(cacheKey, result);

        return result;
    }

    public async Task<PagedResultDto<LicenseUsageDto>> GetLicenseUsageAsync(string organizationId, DateTime? fromDate, DateTime? toDate, string? searchKey, string? status, string? sortBy, string? sortDir, int page, int pageSize)
    {
        // Cache the full license list (no date filter, no search, no status filter)
        var cacheKey = $"org:{organizationId}:licenses:all";
        List<LicenseUsageDto>? allItems = null;

        if (fromDate == null && toDate == null && string.IsNullOrWhiteSpace(searchKey) && string.IsNullOrWhiteSpace(status))
        {
            allItems = await _cache.GetAsync<List<LicenseUsageDto>>(cacheKey);
        }

        if (allItems == null)
        {
            // Build from DB
            allItems = await BuildLicenseUsageList(organizationId, fromDate, toDate);

            // Cache if no date filter
            if (fromDate == null && toDate == null)
                await _cache.SetAsync(cacheKey, allItems);
        }

        // Apply search filter
        var filtered = allItems.ToList();
        if (!string.IsNullOrWhiteSpace(searchKey))
            filtered = filtered.Where(i => i.LicenseKey.Contains(searchKey)).ToList();

        // Apply status filter
        if (status == "with_usage")
            filtered = filtered.Where(i => i.HasUsageData).ToList();
        else if (status == "without_usage")
            filtered = filtered.Where(i => !i.HasUsageData).ToList();

        // Apply sorting
        filtered = ApplyLicenseSort(filtered, sortBy, sortDir);

        // Paginate
        var totalCount = filtered.Count;
        var pagedItems = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResultDto<LicenseUsageDto>
        {
            Items = pagedItems,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<GradeUsageDto>> GetGradeUsageAsync(string licenseKey, DateTime? fromDate, DateTime? toDate)
    {
        var cacheKey = $"license:{licenseKey}:grades";
        if (fromDate == null && toDate == null)
        {
            var cached = await _cache.GetAsync<List<GradeUsageDto>>(cacheKey);
            if (cached != null) return cached;
        }

        var (videoId, simId) = await GetContentTypeIdsForLicense(licenseKey);

        var query = _context.PanelUsageDatas.AsNoTracking().Where(p => p.LicenseKey == licenseKey);
        query = ApplyDateFilter(query, fromDate, toDate);

        var result = await query
            .GroupBy(p => p.GradeId)
            .Select(g => new GradeUsageDto
            {
                GradeId = g.Key!,
                TotalSessions = g.Count(),
                TotalUsageSeconds = g.Sum(p => p.UsageTime),
                VideoUsageSeconds = g.Where(p => p.ContentTypeId == videoId).Sum(p => p.UsageTime),
                SimulationUsageSeconds = g.Where(p => p.ContentTypeId == simId).Sum(p => p.UsageTime),
                SubjectCount = g.Select(p => p.SubjectId).Distinct().Count(),
                LastUsageDate = g.Max(p => p.CreatedDate)
            })
            .OrderByDescending(g => g.TotalUsageSeconds)
            .ToListAsync();

        var gradeIds = result.Select(r => r.GradeId).ToList();
        var gradeNames = await GetSubCategoryNames(gradeIds);
        foreach (var grade in result)
            grade.GradeName = gradeNames.GetValueOrDefault(grade.GradeId) ?? grade.GradeId;

        if (fromDate == null && toDate == null)
            await _cache.SetAsync(cacheKey, result);

        return result;
    }

    public async Task<List<SubjectUsageDto>> GetSubjectUsageAsync(string licenseKey, string gradeId, DateTime? fromDate, DateTime? toDate)
    {
        var cacheKey = $"license:{licenseKey}:grade:{gradeId}:subjects";
        if (fromDate == null && toDate == null)
        {
            var cached = await _cache.GetAsync<List<SubjectUsageDto>>(cacheKey);
            if (cached != null) return cached;
        }

        var (videoId, simId) = await GetContentTypeIdsForLicense(licenseKey);

        var query = _context.PanelUsageDatas.AsNoTracking()
            .Where(p => p.LicenseKey == licenseKey && p.GradeId == gradeId);
        query = ApplyDateFilter(query, fromDate, toDate);

        var result = await query
            .GroupBy(p => p.SubjectId)
            .Select(g => new SubjectUsageDto
            {
                SubjectId = g.Key!,
                TotalSessions = g.Count(),
                TotalUsageSeconds = g.Sum(p => p.UsageTime),
                VideoUsageSeconds = g.Where(p => p.ContentTypeId == videoId).Sum(p => p.UsageTime),
                SimulationUsageSeconds = g.Where(p => p.ContentTypeId == simId).Sum(p => p.UsageTime),
                ChapterCount = g.Select(p => p.ChapterId).Distinct().Count(),
                LastUsageDate = g.Max(p => p.CreatedDate)
            })
            .OrderByDescending(s => s.TotalUsageSeconds)
            .ToListAsync();

        var subjectIds = result.Select(r => r.SubjectId).ToList();
        var subjectNames = await GetSubCategoryNames(subjectIds);
        foreach (var subject in result)
            subject.SubjectName = subjectNames.GetValueOrDefault(subject.SubjectId) ?? subject.SubjectId;

        if (fromDate == null && toDate == null)
            await _cache.SetAsync(cacheKey, result);

        return result;
    }

    public async Task<List<ChapterUsageDto>> GetChapterUsageAsync(string licenseKey, string gradeId, string subjectId, DateTime? fromDate, DateTime? toDate)
    {
        var cacheKey = $"license:{licenseKey}:grade:{gradeId}:subject:{subjectId}:chapters";
        if (fromDate == null && toDate == null)
        {
            var cached = await _cache.GetAsync<List<ChapterUsageDto>>(cacheKey);
            if (cached != null) return cached;
        }

        var (videoId, simId) = await GetContentTypeIdsForLicense(licenseKey);

        var query = _context.PanelUsageDatas.AsNoTracking()
            .Where(p => p.LicenseKey == licenseKey && p.GradeId == gradeId && p.SubjectId == subjectId);
        query = ApplyDateFilter(query, fromDate, toDate);

        var result = await query
            .GroupBy(p => p.ChapterId)
            .Select(g => new ChapterUsageDto
            {
                ChapterId = g.Key!,
                TotalSessions = g.Count(),
                TotalUsageSeconds = g.Sum(p => p.UsageTime),
                VideoUsageSeconds = g.Where(p => p.ContentTypeId == videoId).Sum(p => p.UsageTime),
                SimulationUsageSeconds = g.Where(p => p.ContentTypeId == simId).Sum(p => p.UsageTime),
                TopicCount = g.Select(p => p.TopicId).Distinct().Count(),
                LastUsageDate = g.Max(p => p.CreatedDate)
            })
            .OrderByDescending(c => c.TotalUsageSeconds)
            .ToListAsync();

        var chapterIds = result.Select(r => r.ChapterId).ToList();
        var chapterNames = await GetSubCategoryNames(chapterIds);
        foreach (var chapter in result)
            chapter.ChapterName = chapterNames.GetValueOrDefault(chapter.ChapterId) ?? chapter.ChapterId;

        if (fromDate == null && toDate == null)
            await _cache.SetAsync(cacheKey, result);

        return result;
    }

    public async Task<List<TopicUsageDto>> GetTopicUsageAsync(string licenseKey, string chapterId, DateTime? fromDate, DateTime? toDate)
    {
        var cacheKey = $"license:{licenseKey}:chapter:{chapterId}:topics";
        if (fromDate == null && toDate == null)
        {
            var cached = await _cache.GetAsync<List<TopicUsageDto>>(cacheKey);
            if (cached != null) return cached;
        }

        var (videoId, simId) = await GetContentTypeIdsForLicense(licenseKey);

        var query = _context.PanelUsageDatas.AsNoTracking()
            .Where(p => p.LicenseKey == licenseKey && p.ChapterId == chapterId);
        query = ApplyDateFilter(query, fromDate, toDate);

        var rawData = await query
            .GroupBy(p => p.TopicId)
            .Select(g => new
            {
                TopicId = g.Key,
                VideoSeconds = g.Where(p => p.ContentTypeId == videoId).Sum(p => p.UsageTime),
                SimSeconds = g.Where(p => p.ContentTypeId == simId).Sum(p => p.UsageTime),
                HasVideo = g.Any(p => p.ContentTypeId == videoId),
                HasSim = g.Any(p => p.ContentTypeId == simId),
                LastUsage = g.Max(p => p.CreatedDate)
            })
            .OrderByDescending(t => t.VideoSeconds + t.SimSeconds)
            .ToListAsync();

        var topicIds = rawData.Select(r => r.TopicId).ToList();
        var topicNames = await _context.TopicDetails.AsNoTracking()
            .Where(td => topicIds.Contains(td.TopicId) && td.LanguageId == DefaultLanguageId)
            .ToDictionaryAsync(td => td.TopicId, td => td.Name);

        var result = rawData.Select(r => new TopicUsageDto
        {
            TopicId = r.TopicId,
            TopicName = topicNames.GetValueOrDefault(r.TopicId) ?? $"Topic {r.TopicId}",
            VideoWatched = r.HasVideo,
            VideoUsageSeconds = r.VideoSeconds,
            SimulationUsed = r.HasSim,
            SimulationUsageSeconds = r.SimSeconds,
            TotalUsageSeconds = r.VideoSeconds + r.SimSeconds,
            LastUsageDate = r.LastUsage
        }).ToList();

        if (fromDate == null && toDate == null)
            await _cache.SetAsync(cacheKey, result);

        return result;
    }

    public async Task<List<DailyUsageTrendDto>> GetDailyTrendAsync(string organizationId, DateTime? fromDate, DateTime? toDate)
    {
        var cacheKey = $"org:{organizationId}:trend";
        if (fromDate == null && toDate == null)
        {
            var cached = await _cache.GetAsync<List<DailyUsageTrendDto>>(cacheKey);
            if (cached != null) return cached;
        }

        // Get license data with platform info for correct ContentTypeId mapping
        var licenseData = await GetLicenseActivationData(organizationId);
        var licenseKeys = licenseData.Select(l => l.Key).ToList();
        if (!licenseKeys.Any()) return new List<DailyUsageTrendDto>();

        var cutoffDate = GetCutoffDate();

        // Build a map of licenseKey → (videoId, simId)
        var licenseMapping = licenseData.ToDictionary(
            l => l.Key,
            l => GetContentTypeIds(l.Platform, l.ActivatedDate, cutoffDate)
        );

        // Get raw data grouped by date + license + contentType
        var query = _context.PanelUsageDatas.AsNoTracking()
            .Where(p => licenseKeys.Contains(p.LicenseKey!));
        query = ApplyDateFilter(query, fromDate, toDate);

        var rawData = await query
            .GroupBy(p => new { Date = p.CreatedDate.Date, p.LicenseKey, p.ContentTypeId })
            .Select(g => new { g.Key.Date, LicenseKey = g.Key.LicenseKey!, g.Key.ContentTypeId, Seconds = g.Sum(p => p.UsageTime), Count = g.Count() })
            .ToListAsync();

        // Aggregate per day with platform-aware mapping
        var result = rawData
            .GroupBy(r => r.Date)
            .Select(dayGroup =>
            {
                int videoSec = 0, simSec = 0, sessions = 0;
                foreach (var record in dayGroup)
                {
                    sessions += record.Count;
                    if (licenseMapping.TryGetValue(record.LicenseKey, out var mapping))
                    {
                        if (record.ContentTypeId == mapping.videoId) videoSec += record.Seconds;
                        else if (record.ContentTypeId == mapping.simId) simSec += record.Seconds;
                    }
                }
                return new DailyUsageTrendDto
                {
                    Date = dayGroup.Key,
                    VideoSeconds = videoSec,
                    SimulationSeconds = simSec,
                    TotalSessions = sessions
                };
            })
            .OrderBy(d => d.Date)
            .ToList();

        if (fromDate == null && toDate == null)
            await _cache.SetAsync(cacheKey, result);

        return result;
    }

    public async Task<List<HeatmapDto>> GetHeatmapAsync(string organizationId, int days)
    {
        var cacheKey = $"org:{organizationId}:heatmap:{days}";
        var cached = await _cache.GetAsync<List<HeatmapDto>>(cacheKey);
        if (cached != null) return cached;

        var licenseKeys = await GetLicenseKeysForOrg(organizationId);
        if (!licenseKeys.Any()) return new List<HeatmapDto>();

        // Filter by date range
        var fromDate = DateTime.UtcNow.AddDays(-days);

        // Get usage with CreatedDate for hour/day extraction
        var rawData = await _context.PanelUsageDatas.AsNoTracking()
            .Where(p => licenseKeys.Contains(p.LicenseKey!) && p.CreatedDate >= fromDate)
            .Select(p => new { p.CreatedDate, p.UsageTime })
            .ToListAsync();

        // Convert UTC to IST (+5:30) then group by DayOfWeek + Hour
        var istOffset = TimeSpan.FromHours(5.5);
        var result = rawData
            .Select(p => new { IstDate = p.CreatedDate.Add(istOffset), p.UsageTime })
            .GroupBy(p => new { DayOfWeek = (int)p.IstDate.DayOfWeek, Hour = p.IstDate.Hour })
            .Select(g => new HeatmapDto
            {
                DayOfWeek = g.Key.DayOfWeek,
                Hour = g.Key.Hour,
                Minutes = g.Sum(p => p.UsageTime) / 60,
                Sessions = g.Count()
            })
            .OrderBy(h => h.DayOfWeek)
            .ThenBy(h => h.Hour)
            .ToList();

        await _cache.SetAsync(cacheKey, result);
        return result;
    }

    // ===== HELPERS =====

    private async Task<List<PopularTopicDto>> GetTopicsWithPath(string organizationId, bool mostPopular)
    {
        var cacheKey = $"org:{organizationId}:{(mostPopular ? "popular" : "least-engaged")}-topics";
        var cached = await _cache.GetAsync<List<PopularTopicDto>>(cacheKey);
        if (cached != null) return cached;

        var licenseKeys = await GetLicenseKeysForOrg(organizationId);
        if (!licenseKeys.Any()) return new List<PopularTopicDto>();

        var query = _context.PanelUsageDatas.AsNoTracking()
            .Where(p => licenseKeys.Contains(p.LicenseKey!));

        List<PopularTopicDto> result;

        if (mostPopular)
        {
            // Top 10 topics by total usage time
            var topTopics = await query
                .GroupBy(p => new { p.TopicId, p.GradeId, p.SubjectId, p.ChapterId })
                .Select(g => new
                {
                    g.Key.TopicId,
                    g.Key.GradeId,
                    g.Key.SubjectId,
                    g.Key.ChapterId,
                    TotalSessions = g.Count(),
                    TotalSeconds = g.Sum(p => p.UsageTime),
                    AvgSeconds = (int)g.Average(p => p.UsageTime)
                })
                .OrderByDescending(t => t.TotalSeconds)
                .Take(10)
                .ToListAsync();

            // Resolve names
            var topicIds = topTopics.Select(t => t.TopicId).Distinct().ToList();
            var topicNames = await _context.TopicDetails.AsNoTracking()
                .Where(td => topicIds.Contains(td.TopicId) && td.LanguageId == 1)
                .ToDictionaryAsync(td => td.TopicId, td => td.Name);

            var subCatIds = topTopics.SelectMany(t => new[] { t.GradeId, t.SubjectId, t.ChapterId }).Where(x => x != null).Distinct().ToList()!;
            var subCatNames = await GetSubCategoryNames(subCatIds!);

            result = topTopics.Select(t => new PopularTopicDto
            {
                TopicId = t.TopicId,
                TopicName = topicNames.GetValueOrDefault(t.TopicId) ?? $"Topic {t.TopicId}",
                GradeName = subCatNames.GetValueOrDefault(t.GradeId ?? "") ?? t.GradeId,
                SubjectName = subCatNames.GetValueOrDefault(t.SubjectId ?? "") ?? t.SubjectId,
                ChapterName = subCatNames.GetValueOrDefault(t.ChapterId ?? "") ?? t.ChapterId,
                TotalSessions = t.TotalSessions,
                TotalUsageSeconds = t.TotalSeconds,
                AvgUsageSeconds = t.AvgSeconds
            }).ToList();
        }
        else
        {
            // Least engaged: opened 3+ times, average < 60 seconds
            var leastEngaged = await query
                .GroupBy(p => new { p.TopicId, p.GradeId, p.SubjectId, p.ChapterId })
                .Where(g => g.Count() >= 3 && g.Average(p => p.UsageTime) < 60)
                .Select(g => new
                {
                    g.Key.TopicId,
                    g.Key.GradeId,
                    g.Key.SubjectId,
                    g.Key.ChapterId,
                    TotalSessions = g.Count(),
                    TotalSeconds = g.Sum(p => p.UsageTime),
                    AvgSeconds = (int)g.Average(p => p.UsageTime)
                })
                .OrderBy(t => t.AvgSeconds)
                .Take(10)
                .ToListAsync();

            var topicIds = leastEngaged.Select(t => t.TopicId).Distinct().ToList();
            var topicNames = await _context.TopicDetails.AsNoTracking()
                .Where(td => topicIds.Contains(td.TopicId) && td.LanguageId == 1)
                .ToDictionaryAsync(td => td.TopicId, td => td.Name);

            var subCatIds = leastEngaged.SelectMany(t => new[] { t.GradeId, t.SubjectId, t.ChapterId }).Where(x => x != null).Distinct().ToList()!;
            var subCatNames = await GetSubCategoryNames(subCatIds!);

            result = leastEngaged.Select(t => new PopularTopicDto
            {
                TopicId = t.TopicId,
                TopicName = topicNames.GetValueOrDefault(t.TopicId) ?? $"Topic {t.TopicId}",
                GradeName = subCatNames.GetValueOrDefault(t.GradeId ?? "") ?? t.GradeId,
                SubjectName = subCatNames.GetValueOrDefault(t.SubjectId ?? "") ?? t.SubjectId,
                ChapterName = subCatNames.GetValueOrDefault(t.ChapterId ?? "") ?? t.ChapterId,
                TotalSessions = t.TotalSessions,
                TotalUsageSeconds = t.TotalSeconds,
                AvgUsageSeconds = t.AvgSeconds
            }).ToList();
        }

        await _cache.SetAsync(cacheKey, result);
        return result;
    }

    public Task<List<PopularTopicDto>> GetPopularTopicsAsync(string organizationId)
        => GetTopicsWithPath(organizationId, true);

    public Task<List<PopularTopicDto>> GetLeastEngagedTopicsAsync(string organizationId)
        => GetTopicsWithPath(organizationId, false);

    private async Task<List<string>> GetLicenseKeysForOrg(string organizationId)
    {
        return await _context.Licenses.AsNoTracking()
            .Where(l => l.OrganizationId == organizationId && l.IsActive)
            .Select(l => l.Key)
            .ToListAsync();
    }

    private async Task<List<LicenseInfo>> GetLicenseActivationData(string organizationId)
    {
        var licenseKeys = await GetLicenseKeysForOrg(organizationId);
        if (!licenseKeys.Any()) return new List<LicenseInfo>();

        var activations = await _context.LicenseActivations.AsNoTracking()
            .Where(la => licenseKeys.Contains(la.LicenseKey!))
            .Select(la => new { la.LicenseKey, la.Platform, la.ConsumerName, la.ActivatedDate })
            .ToListAsync();

        var activationMap = activations
            .GroupBy(a => a.LicenseKey)
            .ToDictionary(g => g.Key!, g => g.OrderByDescending(a => a.ActivatedDate).First());

        return licenseKeys.Select(key =>
        {
            activationMap.TryGetValue(key, out var act);
            return new LicenseInfo
            {
                Key = key,
                Platform = act?.Platform,
                ConsumerName = act?.ConsumerName,
                ActivatedDate = act?.ActivatedDate
            };
        }).ToList();
    }

    private async Task<List<LicenseUsageDto>> BuildLicenseUsageList(string organizationId, DateTime? fromDate, DateTime? toDate)
    {
        var licenseData = await GetLicenseActivationData(organizationId);
        var licenseKeys = licenseData.Select(l => l.Key).ToList();

        if (!licenseKeys.Any()) return new List<LicenseUsageDto>();

        var cutoffDate = GetCutoffDate();

        var query = _context.PanelUsageDatas.AsNoTracking()
            .Where(p => licenseKeys.Contains(p.LicenseKey!));
        query = ApplyDateFilter(query, fromDate, toDate);

        var rawData = await query
            .GroupBy(p => new { p.LicenseKey, p.ContentTypeId })
            .Select(g => new { LicenseKey = g.Key.LicenseKey!, ContentTypeId = g.Key.ContentTypeId, Sessions = g.Count(), Seconds = g.Sum(p => p.UsageTime), GradeCount = g.Select(p => p.GradeId).Distinct().Count(), LastDate = g.Max(p => p.CreatedDate) })
            .ToListAsync();

        return licenseData.Select(license =>
        {
            var records = rawData.Where(r => r.LicenseKey == license.Key).ToList();
            var (videoId, simId) = GetContentTypeIds(license.Platform, license.ActivatedDate, cutoffDate);

            var videoSec = records.Where(r => r.ContentTypeId == videoId).Sum(r => r.Seconds);
            var simSec = records.Where(r => r.ContentTypeId == simId).Sum(r => r.Seconds);
            var totalSessions = records.Sum(r => r.Sessions);
            var gradeCount = records.Any() ? records.Max(r => r.GradeCount) : 0;
            var lastDate = records.Any() ? records.Max(r => r.LastDate) : (DateTime?)null;

            return new LicenseUsageDto
            {
                LicenseKey = license.Key,
                IsActive = true,
                HasUsageData = totalSessions > 0,
                Platform = license.Platform,
                ConsumerName = license.ConsumerName,
                TotalSessions = totalSessions,
                TotalUsageSeconds = videoSec + simSec,
                VideoUsageSeconds = videoSec,
                SimulationUsageSeconds = simSec,
                GradeCount = gradeCount,
                LastUsageDate = lastDate
            };
        }).ToList();
    }

    private async Task<(int videoId, int simId)> GetContentTypeIdsForLicense(string licenseKey)
    {
        var activation = await _context.LicenseActivations.AsNoTracking()
            .Where(la => la.LicenseKey == licenseKey)
            .OrderByDescending(la => la.ActivatedDate)
            .Select(la => new { la.Platform, la.ActivatedDate })
            .FirstOrDefaultAsync();

        var cutoffDate = GetCutoffDate();
        return GetContentTypeIds(activation?.Platform, activation?.ActivatedDate, cutoffDate);
    }

    private static (int videoId, int simId) GetContentTypeIds(string? platform, DateTime? activatedDate, DateTime cutoffDate)
    {
        if (platform?.ToLower() == "android" && activatedDate < cutoffDate)
            return (videoId: 1, simId: 2);

        return (videoId: 2, simId: 3);
    }

    private DateTime GetCutoffDate()
    {
        var str = _configuration["AndroidContentTypeCutoffDate"];
        return DateTime.TryParse(str, out var d) ? d : new DateTime(2026, 8, 15);
    }

    private async Task<Dictionary<string, string?>> GetSubCategoryNames(List<string> ids)
    {
        if (!ids.Any()) return new Dictionary<string, string?>();
        return await _context.SubCategoryDetails.AsNoTracking()
            .Where(sd => ids.Contains(sd.SubCategoryId) && sd.LanguageId == DefaultLanguageId)
            .ToDictionaryAsync(sd => sd.SubCategoryId, sd => sd.Name);
    }

    private static IQueryable<Domain.Entities.PanelUsageData> ApplyDateFilter(
        IQueryable<Domain.Entities.PanelUsageData> query, DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate.HasValue) query = query.Where(p => p.CreatedDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(p => p.CreatedDate <= toDate.Value.Date.AddDays(1));
        return query;
    }

    private static List<LicenseUsageDto> ApplyLicenseSort(List<LicenseUsageDto> items, string? sortBy, string? sortDir)
    {
        var isAsc = sortDir?.ToLower() == "asc";
        return sortBy?.ToLower() switch
        {
            "totalsessions" => isAsc ? items.OrderBy(i => i.TotalSessions).ToList() : items.OrderByDescending(i => i.TotalSessions).ToList(),
            "totalusageseconds" => isAsc ? items.OrderBy(i => i.TotalUsageSeconds).ToList() : items.OrderByDescending(i => i.TotalUsageSeconds).ToList(),
            "videousageseconds" => isAsc ? items.OrderBy(i => i.VideoUsageSeconds).ToList() : items.OrderByDescending(i => i.VideoUsageSeconds).ToList(),
            "simulationusageseconds" => isAsc ? items.OrderBy(i => i.SimulationUsageSeconds).ToList() : items.OrderByDescending(i => i.SimulationUsageSeconds).ToList(),
            "lastusagedate" => isAsc ? items.OrderBy(i => i.LastUsageDate ?? DateTime.MinValue).ToList() : items.OrderByDescending(i => i.LastUsageDate ?? DateTime.MinValue).ToList(),
            _ => items.OrderByDescending(i => i.TotalUsageSeconds).ToList()
        };
    }

    private class LicenseInfo
    {
        public string Key { get; set; } = string.Empty;
        public string? Platform { get; set; }
        public string? ConsumerName { get; set; }
        public DateTime? ActivatedDate { get; set; }
    }
}
