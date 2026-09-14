using KnOwl.ControlPlane.Distribution.Core;

namespace KnOwl.ControlPlane.Application.Distribution.ReleaseBundles;

/// <summary>
/// Coordinates creation of contract release bundles.
/// </summary>
public interface IContractReleaseInteractionService
{
    /// <summary>
    /// Creates a release bundle from existing artifacts.
    /// </summary>
    Task<ContractRelease> Create(string name, string? description, IReadOnlyCollection<Guid> artifactIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates release targets for the selected runtime nodes and moves the release into distribution.
    /// </summary>
    Task<ContractRelease> Plan(Guid releaseId, IReadOnlyCollection<Guid> runtimeNodeIds, string rolloutGroup = "ManualRelease", CancellationToken cancellationToken = default);
}

