using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Distribution.Core;

namespace KnOwl.ControlPlane.Application.Distribution.Artifacts;

/// <summary>
/// Creates immutable contract artifacts from approved event and command versions.
/// </summary>
public interface IContractArtifactBuilder
{
    /// <summary>
    /// Creates or returns an existing event artifact for the provided version.
    /// </summary>
    Task<ContractArtifact> BuildEventArtifact(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or returns existing command artifacts for the provided version.
    /// </summary>
    Task<IReadOnlyList<ContractArtifact>> BuildCommandArtifacts(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or returns the command request artifact for the provided version.
    /// </summary>
    Task<ContractArtifact> BuildCommandArtifact(Guid versionId, CancellationToken cancellationToken = default);
}

