using System.ComponentModel.DataAnnotations;

namespace KnOwl.Contracts.Artifacts;

/// <summary>
/// Represents an immutable deployable contract artifact generated from an event or command version.
/// </summary>
public sealed class ContractArtifact
{
    public Guid Id { get; set; } = Guid.NewGuid();

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

    [Required]
    [MaxLength(32)]
    public string SourceStatus { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
