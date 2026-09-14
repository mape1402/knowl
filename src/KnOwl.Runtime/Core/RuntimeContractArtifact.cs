using KnOwl.Contracts.Artifacts;
using System.ComponentModel.DataAnnotations;

namespace KnOwl.Runtime.Core;

/// <summary>
/// Represents a contract artifact deployed into KnOwl runtime storage.
/// </summary>
public sealed class RuntimeContractArtifact
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SourceArtifactId { get; set; }

    public Guid SourceReleaseId { get; set; }

    public ContractArtifactType ArtifactType { get; set; }

    public Guid DefinitionId { get; set; }

    public Guid VersionId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(70)]
    public string Topic { get; set; } = string.Empty;

    [Required]
    [MaxLength(13)]
    public string VersionNumber { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public string PayloadSchemaJson { get; set; } = "{}";

    [Required]
    [MaxLength(128)]
    public string ContentHash { get; set; } = string.Empty;

    public DateTime DeployedAtUtc { get; set; } = DateTime.UtcNow;
}
