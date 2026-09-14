using KnOwl.Contracts.ArtifactDelivery;

namespace KnOwl.Runtime.Application.Deployments;

/// <summary>
/// Deploys contract artifact packages into KnOwl Runtime storage.
/// </summary>
public interface IRuntimeContractDeploymentService
{
    /// <summary>
    /// Deploys one artifact package received from a trusted Control Plane.
    /// </summary>
    Task<RuntimeArtifactDeploymentResult> DeployArtifact(
        RuntimeArtifactDeliveryPackage package,
        string sourceKey,
        CancellationToken cancellationToken = default);
}
