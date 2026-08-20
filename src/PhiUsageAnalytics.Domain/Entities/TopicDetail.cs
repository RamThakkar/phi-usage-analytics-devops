namespace PhiUsageAnalytics.Domain.Entities;

/// <summary>
/// Stores topic names per language.
/// Maps to [PhiSyllabusDb].[dbo].[TopicDetails] table.
/// Composite PK: (TopicId, LanguageId)
/// </summary>
public class TopicDetail
{
    public int TopicId { get; set; }
    public int LanguageId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? SearchTag { get; set; }
    public bool IsQuiz { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? Status { get; set; }
}
