namespace PhiUsageAnalytics.Application.DTOs;

/// <summary>
/// Usage report grouped by subject within a grade.
/// </summary>
public class SubjectUsageDto
{
    public string SubjectId { get; set; } = string.Empty;
    public string? SubjectName { get; set; }
    public int TotalSessions { get; set; }
    public int TotalUsageSeconds { get; set; }
    public int VideoUsageSeconds { get; set; }
    public int SimulationUsageSeconds { get; set; }
    public int ChapterCount { get; set; }
    public DateTime? LastUsageDate { get; set; }
    public string TotalTime => FormatTime(TotalUsageSeconds);
    public string VideoTime => FormatTime(VideoUsageSeconds);
    public string SimulationTime => FormatTime(SimulationUsageSeconds);

    private static string FormatTime(int totalSeconds)
    {
        var hours = totalSeconds / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        return hours > 0 ? $"{hours}h {minutes:D2}m" : $"{minutes}m";
    }
}
