namespace PhiUsageAnalytics.Application.DTOs;

/// <summary>
/// Represents a topic with its full hierarchy path and usage stats.
/// Used for Most Popular and Least Engaged reports.
/// </summary>
public class PopularTopicDto
{
    public int TopicId { get; set; }
    public string? TopicName { get; set; }
    public string? GradeName { get; set; }
    public string? SubjectName { get; set; }
    public string? ChapterName { get; set; }
    public int TotalSessions { get; set; }
    public int TotalUsageSeconds { get; set; }
    public int AvgUsageSeconds { get; set; }
    public string Path => $"{GradeName} → {SubjectName} → {ChapterName}";
}
