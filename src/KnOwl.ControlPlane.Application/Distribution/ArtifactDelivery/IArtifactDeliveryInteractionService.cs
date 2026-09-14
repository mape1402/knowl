using KnOwl.Contracts.ArtifactDelivery;

namespace KnOwl.ControlPlane.Application.Distribution.ArtifactDelivery;

/// <summary>
/// Coordinates push and pull delivery operations between Distribution and runtime nodes.
/// </summary>
public interface IArtifactDeliveryInteractionService
{
    /// <summary>
    /// Pushes a release target to its configured runtime node endpoint.
    /// </summary>
    Task<RuntimeArtifactDeliveryResult> Push(Guid releaseTargetId, string initiatedBy = "distribution", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets packages available for a runtime node to pull.
    /// </summary>
    Task<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>> GetPendingForPull(Guid runtimeNodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one package available for a runtime node to pull.
    /// </summary>
    Task<RuntimeArtifactDeliveryPackage> GetForPull(
        Guid runtimeNodeId,
        Guid releaseTargetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges a package pulled and stored by a runtime node.
    /// </summary>
    Task<RuntimeArtifactDeliveryResult> AcknowledgePull(
        Guid runtimeNodeId,
        Guid releaseTargetId,
        string runtimeArtifactId,
        string runtimeArtifactStatus = "Ready",
        CancellationToken cancellationToken = default);
}
