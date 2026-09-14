using KnOwl.Contracts.Artifacts;
namespace KnOwl.ControlPlane.Distribution.Core;

/// <summary>
/// Represents an artifact selected into a contract release bundle.
/// </summary>
public sealed class ContractReleaseItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ReleaseId { get; set; }

    public Guid ArtifactId { get; set; }

    public ContractRelease? Release { get; set; }

    public ContractArtifact? Artifact { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ContractReleaseTarget> Targets { get; set; } = [];
}
