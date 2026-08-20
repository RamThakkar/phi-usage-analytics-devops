namespace PhiUsageAnalytics.Application.DTOs;

/// <summary>
/// Usage heatmap data point (hour × day of week).
/// </summary>
public class HeatmapDto
{
    /// <summary>Day of week: 0=Sunday, 1=Monday, ... 6=Saturday</summary>
    public int DayOfWeek { get; set; }

    /// <summary>Hour of day: 0-23</summary>
    public int Hour { get; set; }

    /// <summary>Total usage minutes in this slot</summary>
    public int Minutes { get; set; }

    /// <summary>Number of sessions in this slot</summary>
    public int Sessions { get; set; }
}
