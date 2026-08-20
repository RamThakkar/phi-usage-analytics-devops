namespace PhiUsageAnalytics.Application.DTOs;

/// <summary>
/// Daily usage data point for trend chart.
/// </summary>
public class DailyUsageTrendDto
{
    public DateTime Date { get; set; }
    public int VideoSeconds { get; set; }
    public int SimulationSeconds { get; set; }
    public int TotalSessions { get; set; }
}
