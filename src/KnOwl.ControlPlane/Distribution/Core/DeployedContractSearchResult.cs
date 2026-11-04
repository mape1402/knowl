using KnOwl.Contracts.Artifacts;

namespace KnOwl.ControlPlane.Distribution.Core;

/// <summary>
/// Identifies a deployed contract without transferring its schema document.
/// </summary>
public sealed record DeployedContractSearchResult(
    Guid Id,
    ContractArtifactType ArtifactType,
    string ContractKey,
    string VersionNumber,
    string ContentHash,
    DateTime CreatedAtUtc,
    bool HasUsableSchema);
