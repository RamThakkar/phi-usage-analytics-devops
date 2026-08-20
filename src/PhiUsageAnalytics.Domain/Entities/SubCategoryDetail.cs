namespace PhiUsageAnalytics.Domain.Entities;

/// <summary>
/// Stores subcategory (grade/subject/chapter) names per language.
/// Maps to [PhiSyllabusDb].[dbo].[SubCategoryDetails] table.
/// Composite PK: (SubCategoryId, LanguageId)
/// </summary>
public class SubCategoryDetail
{
    public string SubCategoryId { get; set; } = string.Empty;
    public int LanguageId { get; set; }
    public string? Name { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsActive { get; set; }
}
