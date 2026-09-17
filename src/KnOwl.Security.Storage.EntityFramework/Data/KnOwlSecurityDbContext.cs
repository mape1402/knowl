using KnOwl.Security.Storage;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.Security.Storage.EntityFramework.Data;

/// <summary>
/// EF Core context for provider-agnostic KnOwl security state.
/// </summary>
public sealed class KnOwlSecurityDbContext(DbContextOptions<KnOwlSecurityDbContext> options) : DbContext(options)
{
    public DbSet<KnOwlSubject> Subjects => Set<KnOwlSubject>();

    public DbSet<KnOwlRoleAssignment> RoleAssignments => Set<KnOwlRoleAssignment>();

    public DbSet<KnOwlPermissionAssignment> PermissionAssignments => Set<KnOwlPermissionAssignment>();

    public DbSet<KnOwlExternalGroupRoleAssignment> ExternalGroupRoleAssignments => Set<KnOwlExternalGroupRoleAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnOwlSubject>(entity =>
        {
            entity.ToTable("KnOwlSecuritySubjects");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Provider, x.SubjectId }).IsUnique();
            entity.Property(x => x.Provider).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SubjectId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200);
            entity.Property(x => x.Email).HasMaxLength(320);
        });

        modelBuilder.Entity<KnOwlRoleAssignment>(entity =>
        {
            entity.ToTable("KnOwlSecurityRoleAssignments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Provider, x.SubjectId, x.Role, x.ScopeType, x.ScopeId });
            entity.Property(x => x.Provider).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SubjectId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ScopeType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ScopeId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Source).HasConversion<string>().HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<KnOwlPermissionAssignment>(entity =>
        {
            entity.ToTable("KnOwlSecurityPermissionAssignments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Provider, x.SubjectId, x.Permission, x.ScopeType, x.ScopeId });
            entity.Property(x => x.Provider).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SubjectId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Permission).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ScopeType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ScopeId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Source).HasConversion<string>().HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<KnOwlExternalGroupRoleAssignment>(entity =>
        {
            entity.ToTable("KnOwlSecurityExternalGroupRoleAssignments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Provider, x.ExternalGroupId, x.Role, x.ScopeType, x.ScopeId });
            entity.Property(x => x.Provider).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ExternalGroupId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ScopeType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ScopeId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Source).HasConversion<string>().HasMaxLength(50).IsRequired();
        });
    }
}
