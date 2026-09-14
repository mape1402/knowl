using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.Security;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Distribution;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.Runtime.Storage.EntityFramework.Data;

/// <summary>
/// EF Core database context for KnOwl Runtime metadata storage.
/// </summary>
public sealed class KnOwlRuntimeDbContext(DbContextOptions<KnOwlRuntimeDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Contract artifacts deployed to this runtime node.
    /// </summary>
    public DbSet<RuntimeContractArtifact> RuntimeContractArtifacts => Set<RuntimeContractArtifact>();

    /// <summary>
    /// Control Plane nodes trusted by this runtime node.
    /// </summary>
    public DbSet<RuntimeDesignNode> RuntimeDesignNodes => Set<RuntimeDesignNode>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RuntimeContractArtifact>(entity =>
        {
            entity.ToTable("RuntimeContractArtifacts");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ArtifactType, x.Topic, x.VersionNumber }).IsUnique();
            entity.HasIndex(x => x.SourceReleaseId);
            entity.HasIndex(x => new { x.ArtifactType, x.Topic, x.DeployedAtUtc });
            entity.Property(x => x.ArtifactType)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Topic).HasMaxLength(70).IsRequired();
            entity.Property(x => x.VersionNumber).HasMaxLength(13).IsRequired();
            entity.Property(x => x.PayloadSchemaJson).IsRequired();
            entity.Property(x => x.ContentHash).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<RuntimeDesignNode>(entity =>
        {
            entity.ToTable("RuntimeDesignNodes");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Key).IsUnique();
            entity.HasIndex(x => x.InboundClientId);
            entity.Property(x => x.Key).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EndpointBaseUri).HasMaxLength(500).IsRequired();
            entity.Property(x => x.RemoteRuntimeNodeId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DistributionMode)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.InboundClientId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.InboundKeyId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.InboundSecretHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.InboundAllowedScopes).HasMaxLength(500).IsRequired();
            entity.Property(x => x.InboundCredentialStatus)
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasDefaultValue(ConnectionCredentialStatus.Missing)
                .IsRequired();
            entity.Property(x => x.InboundLastFailureReason).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.OutboundClientId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.OutboundKeyId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ProtectedOutboundSecret).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.OutboundRequestedScopes).HasMaxLength(500).IsRequired();
            entity.Property(x => x.OutboundCredentialStatus)
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasDefaultValue(ConnectionCredentialStatus.Missing)
                .IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasDefaultValue(RuntimeDesignNodeStatus.Pending)
                .IsRequired();
            entity.Property(x => x.IsEnabled).HasDefaultValue(false);
        });
    }
}

