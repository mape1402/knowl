using KnOwl.Documentation;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.Documentation.Storage.EntityFramework.Data;

/// <summary>
/// EF Core context for KnOwl documentation metadata and database-backed content.
/// </summary>
public sealed class KnOwlDocumentationDbContext(DbContextOptions<KnOwlDocumentationDbContext> options) : DbContext(options)
{
    public DbSet<DocumentationSpace> Spaces => Set<DocumentationSpace>();
    public DbSet<DocumentationTopic> Topics => Set<DocumentationTopic>();
    public DbSet<DocumentationPage> Pages => Set<DocumentationPage>();
    public DbSet<DocumentationPageVersion> PageVersions => Set<DocumentationPageVersion>();
    public DbSet<DocumentationAsset> Assets => Set<DocumentationAsset>();
    public DbSet<DocumentationContentBlob> ContentBlobs => Set<DocumentationContentBlob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("documentation");

        modelBuilder.Entity<DocumentationSpace>(entity =>
        {
            entity.ToTable("Spaces");
            entity.HasIndex(x => x.Key).IsUnique();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.HasMany(x => x.Topics).WithOne(x => x.Space).HasForeignKey(x => x.SpaceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentationTopic>(entity =>
        {
            entity.ToTable("Topics");
            entity.HasIndex(x => new { x.SpaceId, x.Key }).IsUnique();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.HasMany(x => x.Pages).WithOne(x => x.Topic).HasForeignKey(x => x.TopicId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentationPage>(entity =>
        {
            entity.ToTable("Pages");
            entity.HasIndex(x => new { x.TopicId, x.Key }).IsUnique();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.HasMany(x => x.Versions).WithOne(x => x.Page).HasForeignKey(x => x.PageId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentationPageVersion>(entity =>
        {
            entity.ToTable("PageVersions");
            entity.HasIndex(x => new { x.PageId, x.VersionNumber }).IsUnique();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).HasDefaultValue(DocPageVersionStatus.Draft);
            entity.HasMany(x => x.Assets).WithOne(x => x.PageVersion).HasForeignKey(x => x.PageVersionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentationAsset>(entity =>
        {
            entity.ToTable("Assets");
            entity.HasIndex(x => new { x.PageVersionId, x.LogicalPath }).IsUnique();
            entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.IsDownloadable).HasDefaultValue(true);
        });

        modelBuilder.Entity<DocumentationContentBlob>(entity =>
        {
            entity.ToTable("ContentBlobs");
            entity.Property(x => x.Content).IsRequired();
        });
    }
}
