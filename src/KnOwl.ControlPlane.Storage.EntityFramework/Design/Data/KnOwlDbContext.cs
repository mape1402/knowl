using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.Contracts.Security;
using KnOwl.Runtime.Core;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;

/// <summary>
/// EF Core database context for KnOwl async contract persistence.
/// </summary>
public class KnOwlDbContext(DbContextOptions<KnOwlDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Event definitions persisted by KnOwl.
    /// </summary>
    public DbSet<EventDefinition> Events => Set<EventDefinition>();
    /// <summary>
    /// Versioned payload schemas for event definitions.
    /// </summary>
    public DbSet<EventVersion> EventVersions => Set<EventVersion>();
    /// <summary>
    /// Command definitions persisted by KnOwl.
    /// </summary>
    public DbSet<CommandDefinition> Commands => Set<CommandDefinition>();
    /// <summary>
    /// Versioned payload schemas for command definitions.
    /// </summary>
    public DbSet<CommandVersion> CommandVersions => Set<CommandVersion>();
    /// <summary>
    /// System and custom schema type definitions.
    /// </summary>
    public DbSet<SchemaTypeDefinition> SchemaTypes => Set<SchemaTypeDefinition>();
    /// <summary>
    /// Versioned schema type definitions.
    /// </summary>
    public DbSet<SchemaTypeVersion> SchemaTypeVersions => Set<SchemaTypeVersion>();
    /// <summary>
    /// Custom field metadata definitions.
    /// </summary>
    public DbSet<ContractFieldMetadataDefinition> ContractFieldMetadataDefinitions => Set<ContractFieldMetadataDefinition>();
    /// <summary>
    /// Versioned custom field metadata definitions.
    /// </summary>
    public DbSet<ContractFieldMetadataVersion> ContractFieldMetadataVersions => Set<ContractFieldMetadataVersion>();
    /// <summary>
    /// Immutable event and command artifacts generated for distribution.
    /// </summary>
    public DbSet<ContractArtifact> ContractArtifacts => Set<ContractArtifact>();
    /// <summary>
    /// Release bundles assembled from immutable contract artifacts.
    /// </summary>
    public DbSet<ContractRelease> ContractReleases => Set<ContractRelease>();
    /// <summary>
    /// Artifact selections for release bundles.
    /// </summary>
    public DbSet<ContractReleaseItem> ContractReleaseItems => Set<ContractReleaseItem>();
    /// <summary>
    /// Runtime environments registered for distribution.
    /// </summary>
    public DbSet<RuntimeEnvironment> RuntimeEnvironments => Set<RuntimeEnvironment>();
    /// <summary>
    /// Runtime nodes registered as distribution targets.
    /// </summary>
    public DbSet<RuntimeNode> RuntimeNodes => Set<RuntimeNode>();
    /// <summary>
    /// Release artifact targets assigned to runtime nodes.
    /// </summary>
    public DbSet<ContractReleaseTarget> ContractReleaseTargets => Set<ContractReleaseTarget>();
    /// <summary>
    /// Distribution attempts recorded for release targets.
    /// </summary>
    public DbSet<ContractReleaseAttempt> ContractReleaseAttempts => Set<ContractReleaseAttempt>();
    /// <summary>
    /// Contract artifacts deployed to runtime storage.
    /// </summary>
    public DbSet<RuntimeContractArtifact> RuntimeContractArtifacts => Set<RuntimeContractArtifact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventDefinition>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Topic);
            entity.HasIndex(x => x.IsActive);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<EventVersion>(entity =>
        {
            entity.ToTable("EventVersions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.EventDefinitionId, x.VersionNumber }).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasDefaultValue(ContractVersionStatus.Draft)
                .IsRequired();
            entity.HasOne(x => x.EventDefinition)
                .WithMany(x => x.Versions)
                .HasForeignKey(x => x.EventDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CommandDefinition>(entity =>
        {
            entity.ToTable("Commands");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Topic);
            entity.HasIndex(x => x.IsActive);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<CommandVersion>(entity =>
        {
            entity.ToTable("CommandVersions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CommandDefinitionId, x.VersionNumber }).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasDefaultValue(ContractVersionStatus.Draft)
                .IsRequired();
            entity.HasOne(x => x.CommandDefinition)
                .WithMany(x => x.Versions)
                .HasForeignKey(x => x.CommandDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SchemaTypeDefinition>(entity =>
        {
            entity.ToTable("SchemaTypeDefinitions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Key).IsUnique();
            entity.HasIndex(x => x.IsActive);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Key).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<SchemaTypeVersion>(entity =>
        {
            entity.ToTable("SchemaTypeVersions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.SchemaTypeDefinitionId, x.VersionNumber }).IsUnique();
            entity.HasIndex(x => x.IsActive);
            entity.Property(x => x.VersionNumber).HasMaxLength(13).IsRequired();
            entity.Property(x => x.DefinitionJson).IsRequired();
            entity.Property(x => x.Comment).HasMaxLength(1000);
            entity.HasOne(x => x.SchemaTypeDefinition)
                .WithMany(x => x.Versions)
                .HasForeignKey(x => x.SchemaTypeDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContractFieldMetadataDefinition>(entity =>
        {
            entity.ToTable("ContractFieldMetadataDefinitions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Key).IsUnique();
            entity.HasIndex(x => x.IsActive);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Key).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<ContractFieldMetadataVersion>(entity =>
        {
            entity.ToTable("ContractFieldMetadataVersions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ContractFieldMetadataDefinitionId, x.VersionNumber }).IsUnique();
            entity.HasIndex(x => x.IsActive);
            entity.Property(x => x.VersionNumber).HasMaxLength(13).IsRequired();
            entity.Property(x => x.Comment).HasMaxLength(1000);
            entity.Property(x => x.DefinitionJson).IsRequired();
            entity.HasOne(x => x.ContractFieldMetadataDefinition)
                .WithMany(x => x.Versions)
                .HasForeignKey(x => x.ContractFieldMetadataDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContractArtifact>(entity =>
        {
            entity.ToTable("ContractArtifacts");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ArtifactType, x.VersionId }).IsUnique();
            entity.HasIndex(x => new { x.ArtifactType, x.Topic, x.VersionNumber }).IsUnique();
            entity.Property(x => x.ArtifactType)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Topic).HasMaxLength(70).IsRequired();
            entity.Property(x => x.VersionNumber).HasMaxLength(13).IsRequired();
            entity.Property(x => x.PayloadSchemaJson).IsRequired();
            entity.Property(x => x.ContentHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.SourceStatus).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<ContractRelease>(entity =>
        {
            entity.ToTable("ContractReleases");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasDefaultValue(ContractReleaseStatus.Draft)
                .IsRequired();
        });

        modelBuilder.Entity<ContractReleaseItem>(entity =>
        {
            entity.ToTable("ContractReleaseItems");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ReleaseId, x.ArtifactId }).IsUnique();
            entity.HasOne(x => x.Release)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.ReleaseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Artifact)
                .WithMany()
                .HasForeignKey(x => x.ArtifactId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RuntimeEnvironment>(entity =>
        {
            entity.ToTable("RuntimeEnvironments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.IsEnabled);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(100).IsRequired();
            entity.Property(x => x.IsEnabled).HasDefaultValue(true);
        });

        modelBuilder.Entity<RuntimeNode>(entity =>
        {
            entity.ToTable("RuntimeNodes");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.EnvironmentId);
            entity.HasIndex(x => new { x.IsEnabled, x.Status });
            entity.HasIndex(x => x.InboundClientId);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EnvironmentName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DistributionMode)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.EndpointBaseUri).HasMaxLength(500).IsRequired();
            entity.Property(x => x.EndpointApiPath).HasMaxLength(200).IsRequired();
            entity.Property(x => x.AuthenticationMode)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.ClientId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.SecretReference).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ApiKeyReference).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.IsEnabled).HasDefaultValue(true);
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.Property(x => x.AccessTokenTtlSeconds).HasDefaultValue(86_400);
            entity.Property(x => x.TokenRefreshSkewSeconds).HasDefaultValue(300);
            entity.Property(x => x.TokenValidationCacheTtlSeconds).HasDefaultValue(300);
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
            entity.HasOne(x => x.Environment)
                .WithMany(x => x.RuntimeNodes)
                .HasForeignKey(x => x.EnvironmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ContractReleaseTarget>(entity =>
        {
            entity.ToTable("ContractReleaseTargets");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ReleaseId, x.ReleaseItemId, x.RuntimeNodeId }).IsUnique();
            entity.HasIndex(x => new { x.RuntimeNodeId, x.Status });
            entity.HasIndex(x => x.ArtifactId);
            entity.Property(x => x.RolloutGroup).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.ActivationStatus)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.FailureReason).HasMaxLength(2000);
            entity.Property(x => x.RuntimeVersionApplied).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
            entity.HasOne(x => x.Release)
                .WithMany(x => x.Targets)
                .HasForeignKey(x => x.ReleaseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ReleaseItem)
                .WithMany(x => x.Targets)
                .HasForeignKey(x => x.ReleaseItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RuntimeNode)
                .WithMany(x => x.Targets)
                .HasForeignKey(x => x.RuntimeNodeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Artifact)
                .WithMany()
                .HasForeignKey(x => x.ArtifactId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ContractReleaseAttempt>(entity =>
        {
            entity.ToTable("ContractReleaseAttempts");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ReleaseTargetId, x.StartedAtUtc });
            entity.Property(x => x.Action).HasMaxLength(64).IsRequired();
            entity.Property(x => x.InitiatedBy).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ErrorCode).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ErrorMessage).HasMaxLength(2048).IsRequired();
            entity.Property(x => x.ExternalReference).HasMaxLength(256).IsRequired();
            entity.HasOne(x => x.ReleaseTarget)
                .WithMany(x => x.Attempts)
                .HasForeignKey(x => x.ReleaseTargetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RuntimeContractArtifact>(entity =>
        {
            entity.ToTable("RuntimeContractArtifacts");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ArtifactType, x.Topic, x.VersionNumber }).IsUnique();
            entity.HasIndex(x => x.SourceReleaseId);
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

        SeedBasicSchemaTypes(modelBuilder);
    }

    private static void SeedBasicSchemaTypes(ModelBuilder modelBuilder)
    {
        var created = new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc);
        var basics = new[]
        {
            ("11111111-1111-1111-1111-111111111111", "21111111-1111-1111-1111-111111111111", "string", "string", "{\"type\":\"string\"}", "JSON Schema basic type: string"),
            ("11111111-1111-1111-1111-111111111112", "21111111-1111-1111-1111-111111111112", "number", "number", "{\"type\":\"number\"}", "JSON Schema basic type: number"),
            ("11111111-1111-1111-1111-111111111113", "21111111-1111-1111-1111-111111111113", "integer", "integer", "{\"type\":\"integer\"}", "JSON Schema basic type: integer"),
            ("11111111-1111-1111-1111-111111111114", "21111111-1111-1111-1111-111111111114", "boolean", "boolean", "{\"type\":\"boolean\"}", "JSON Schema basic type: boolean"),
            ("11111111-1111-1111-1111-111111111115", "21111111-1111-1111-1111-111111111115", "object", "object", "{\"type\":\"object\"}", "JSON Schema basic type: object"),
            ("11111111-1111-1111-1111-111111111116", "21111111-1111-1111-1111-111111111116", "array", "array", "{\"type\":\"array\"}", "JSON Schema basic type: array"),
            ("11111111-1111-1111-1111-111111111118", "21111111-1111-1111-1111-111111111118", "Date", "string", "{\"type\":\"string\",\"pattern\":\"^\\\\d{4}-\\\\d{2}-\\\\d{2}$\"}", "System schema type: Date"),
            ("11111111-1111-1111-1111-111111111119", "21111111-1111-1111-1111-111111111119", "DateTime", "string", "{\"type\":\"string\",\"pattern\":\"^\\\\d{4}-\\\\d{2}-\\\\d{2}T\\\\d{2}:\\\\d{2}:\\\\d{2}(?:\\\\.\\\\d{1,7})?(?:Z|[+-]\\\\d{2}:\\\\d{2})?$\"}", "System schema type: DateTime"),
            ("11111111-1111-1111-1111-111111111121", "21111111-1111-1111-1111-111111111121", "Time", "string", "{\"type\":\"string\",\"pattern\":\"^\\\\d{2}:\\\\d{2}:\\\\d{2}(?:\\\\.\\\\d{1,7})?$\"}", "System schema type: Time"),
            ("11111111-1111-1111-1111-111111111120", "21111111-1111-1111-1111-111111111120", "TimeSpan", "string", "{\"type\":\"string\",\"pattern\":\"^(?:\\\\d+\\\\.)?\\\\d{2}:\\\\d{2}:\\\\d{2}(?:\\\\.\\\\d{1,7})?$\"}", "System schema type: TimeSpan")
        };

        foreach (var (typeId, versionId, name, baseType, jsonSchema, description) in basics)
        {
            modelBuilder.Entity<SchemaTypeDefinition>().HasData(new SchemaTypeDefinition
            {
                Id = Guid.Parse(typeId),
                Key = name,
                Name = name,
                Description = description,
                IsSystem = true,
                IsActive = true,
                CreatedAtUtc = created,
                UpdatedAtUtc = created
            });

            modelBuilder.Entity<SchemaTypeVersion>().HasData(new SchemaTypeVersion
            {
                Id = Guid.Parse(versionId),
                SchemaTypeDefinitionId = Guid.Parse(typeId),
                VersionNumber = "1.0.0",
                DefinitionJson = CreateSchemaTypeDefinitionJson(name, description, "1.0.0", baseType, jsonSchema),
                IsActive = true,
                CreatedAtUtc = created,
                UpdatedAtUtc = created
            });
        }
    }

    private static string CreateSchemaTypeDefinitionJson(string key, string description, string version, string baseType, string schema)
    {
        return $$"""
{"key":"{{key}}","name":"{{key}}","description":"{{description}}","version":"{{version}}","baseType":"{{baseType}}","comment":"","schema":{{schema}}}
""";
    }
}





