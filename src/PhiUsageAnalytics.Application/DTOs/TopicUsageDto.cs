namespace PhiUsageAnalytics.Application.DTOs;

/// <summary>
/// Usage report for a single topic showing Video and Simulation status.
/// </summary>
public class TopicUsageDto
{
    public int TopicId { get; set; }
    public string? TopicName { get; set; }
    public bool VideoWatched { get; set; }
    public int VideoUsageSeconds { get; set; }
    public bool SimulationUsed { get; set; }
    public int SimulationUsageSeconds { get; set; }
    public int TotalUsageSeconds { get; set; }
    public DateTime? LastUsageDate { get; set; }
    public string VideoTime => VideoWatched ? FormatTime(VideoUsageSeconds) : "—";
    public string SimulationTime => SimulationUsed ? FormatTime(SimulationUsageSeconds) : "—";
    public string TotalTime => FormatTime(TotalUsageSeconds);

    private static string FormatTime(int totalSeconds)
    {
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        return minutes > 0 ? $"{minutes}m {seconds:D2}s" : $"{seconds}s";
    }
}
