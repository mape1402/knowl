using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.Runtime.Distribution;

namespace KnOwl.Runtime.Application.ArtifactDelivery;

/// <summary>
/// Coordinates runtime pull operations from registered Control Plane sources.
/// </summary>
public interface IControlPlaneArtifactPullService
{
    /// <summary>
    /// Gets the enabled Control Plane sources known by Runtime.
    /// </summary>
    Task<IReadOnlyCollection<ControlPlaneDistributionSource>> GetSources(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending artifacts available for pull from a Control Plane source.
    /// </summary>
    Task<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>> GetPending(string sourceKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls, stores and acknowledges one release target from a Control Plane source.
    /// </summary>
    Task<RuntimeArtifactDeploymentResult> Apply(string sourceKey, Guid releaseTargetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores and acknowledges an already fetched artifact package from a Control Plane source.
    /// </summary>
    Task<RuntimeArtifactDeploymentResult> ApplyPackage(
        string sourceKey,
        RuntimeArtifactDeliveryPackage package,
        CancellationToken cancellationToken = default);
}
