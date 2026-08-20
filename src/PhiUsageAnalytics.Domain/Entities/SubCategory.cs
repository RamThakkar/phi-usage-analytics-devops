namespace PhiUsageAnalytics.Domain.Entities;

/// <summary>
/// Represents boards, grades, subjects, chapters in the hierarchy.
/// Maps to [PhiSyllabusDb].[dbo].[SubCategories] table.
/// Note: The Id itself is the readable name (e.g., "CBSE-26-GR-6-MA").
/// </summary>
public class SubCategory
{
    public string Id { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public int CategoryId { get; set; }
    public int OrderIndex { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
