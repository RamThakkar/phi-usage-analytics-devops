namespace PhiUsageAnalytics.Domain.Entities;

/// <summary>
/// Represents a single content usage record.
/// Maps to [dbo].[PanelUsageDatas] table in PhiSyllabusDb.
/// ContentTypeId: 2 = Video, 3 = Simulation
/// </summary>
public class PanelUsageData
{
    public int Id { get; set; }
    public int LanguageId { get; set; }
    public string? BoardId { get; set; }
    public string? GradeId { get; set; }
    public string? SubjectId { get; set; }
    public string? ChapterId { get; set; }
    public int TopicId { get; set; }
    public string? LicenseKey { get; set; }
    public int ContentTypeId { get; set; }
    public long UnixStartTime { get; set; }
    public long UnixEndTime { get; set; }
    public int UsageTime { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
}
