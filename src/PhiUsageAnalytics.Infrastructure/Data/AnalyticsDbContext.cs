using Microsoft.EntityFrameworkCore;
using PhiUsageAnalytics.Domain.Entities;

namespace PhiUsageAnalytics.Infrastructure.Data;

/// <summary>
/// DbContext for PhiSyllabusDb — contains all required tables.
/// Read-only, no migrations.
/// </summary>
public class SyllabusDbContext : DbContext
{
    public SyllabusDbContext(DbContextOptions<SyllabusDbContext> options) : base(options) { }

    public DbSet<License> Licenses => Set<License>();
    public DbSet<LicenseActivation> LicenseActivations => Set<LicenseActivation>();
    public DbSet<PanelUsageData> PanelUsageDatas => Set<PanelUsageData>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<TopicDetail> TopicDetails => Set<TopicDetail>();
    public DbSet<SubCategory> SubCategories => Set<SubCategory>();
    public DbSet<SubCategoryDetail> SubCategoryDetails => Set<SubCategoryDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<License>(entity =>
        {
            entity.ToTable("Licenses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired();
            entity.Property(e => e.OrganizationId).HasMaxLength(450);
        });

        modelBuilder.Entity<LicenseActivation>(entity =>
        {
            entity.ToTable("LicenseActivations");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<PanelUsageData>(entity =>
        {
            entity.ToTable("PanelUsageDatas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BoardId).HasMaxLength(450);
            entity.Property(e => e.GradeId).HasMaxLength(450);
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.ToTable("Topics");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<TopicDetail>(entity =>
        {
            entity.ToTable("TopicDetails");
            entity.HasKey(e => new { e.TopicId, e.LanguageId });
        });

        modelBuilder.Entity<SubCategory>(entity =>
        {
            entity.ToTable("SubCategories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(450);
            entity.Property(e => e.ParentId).HasMaxLength(450);
        });

        modelBuilder.Entity<SubCategoryDetail>(entity =>
        {
            entity.ToTable("SubCategoryDetails");
            entity.HasKey(e => new { e.SubCategoryId, e.LanguageId });
            entity.Property(e => e.SubCategoryId).HasMaxLength(450);
        });
    }
}
