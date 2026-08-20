namespace PhiUsageAnalytics.Domain.Entities;

/// <summary>
/// Represents a license key assigned to an organization.
/// Maps to [PhiSyllabusDb].[dbo].[Licenses] table.
/// OrganizationId references Organizations in PhiLMSDb (cross-db, no FK in EF).
/// </summary>
public class License
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string? DistributorId { get; set; }
    public int Duration { get; set; }
    public bool IsActive { get; set; }
    public bool Renewal { get; set; }
    public DateTime? RenewedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsGradeUpgrade { get; set; }
    public string? ClientSecret { get; set; }
}
