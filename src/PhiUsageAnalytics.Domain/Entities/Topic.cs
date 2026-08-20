namespace PhiUsageAnalytics.Domain.Entities;

/// <summary>
/// Represents a topic in the syllabus.
/// Maps to [PhiSyllabusDb].[dbo].[Topics] table.
/// Name is stored in TopicDetails table (multi-language support).
/// </summary>
public class Topic
{
    public int Id { get; set; }
    public string? ThumbnailPath { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
