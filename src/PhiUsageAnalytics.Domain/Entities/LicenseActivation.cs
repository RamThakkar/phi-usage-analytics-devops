namespace PhiUsageAnalytics.Domain.Entities;

/// <summary>
/// Stores license activation details (device, platform, consumer).
/// Maps to [PhiSyllabusDb].[dbo].[LicenseActivations] table.
/// </summary>
public class LicenseActivation
{
    public int Id { get; set; }
    public string? LicenseKey { get; set; }
    public string? SchoolName { get; set; }
    public string? ConsumerName { get; set; }
    public string? ConsumerEmail { get; set; }
    public string? ConsumerPhoneNumber { get; set; }
    public string? City { get; set; }
    public DateTime ActivatedDate { get; set; }
    public DateTime ExpiredDate { get; set; }
    public string? HardwareId { get; set; }
    public int? ClassId { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? Platform { get; set; }
}
