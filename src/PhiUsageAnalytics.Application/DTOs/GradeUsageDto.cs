namespace PhiUsageAnalytics.Application.DTOs;

/// <summary>
/// Usage report grouped by grade.
/// </summary>
public class GradeUsageDto
{
    public string GradeId { get; set; } = string.Empty;
    public string? GradeName { get; set; }
    public int TotalSessions { get; set; }
    public int TotalUsageSeconds { get; set; }
    public int VideoUsageSeconds { get; set; }
    public int SimulationUsageSeconds { get; set; }
    public int SubjectCount { get; set; }
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
