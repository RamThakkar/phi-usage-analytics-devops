namespace PhiUsageAnalytics.Application.DTOs;

/// <summary>
/// Top-level summary for an organization.
/// </summary>
public class OrganizationSummaryDto
{
    public string OrganizationId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public int TotalLicenses { get; set; }
    public int TotalSessions { get; set; }
    public int TotalUsageSeconds { get; set; }
    public int VideoUsageSeconds { get; set; }
    public int SimulationUsageSeconds { get; set; }
    public string TotalTime => FormatTime(TotalUsageSeconds);
    public string VideoTime => FormatTime(VideoUsageSeconds);
    public string SimulationTime => FormatTime(SimulationUsageSeconds);
    public double VideoPercentage => TotalUsageSeconds > 0 ? Math.Round((double)VideoUsageSeconds / TotalUsageSeconds * 100, 1) : 0;
    public double SimulationPercentage => TotalUsageSeconds > 0 ? Math.Round((double)SimulationUsageSeconds / TotalUsageSeconds * 100, 1) : 0;

    private static string FormatTime(int totalSeconds)
    {
        var hours = totalSeconds / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        return hours > 0 ? $"{hours}h {minutes:D2}m" : $"{minutes}m";
    }
}
