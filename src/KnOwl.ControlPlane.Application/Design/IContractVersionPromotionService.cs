using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Application;

/// <summary>
/// Coordinates lifecycle promotion transitions for event and command versions.
/// </summary>
public interface IContractVersionPromotionService
{
    /// <summary>
    /// Transitions an event version to the requested lifecycle status.
    /// </summary>
    Task TransitionEventVersion(Guid versionId, ContractVersionStatus targetStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions a command version to the requested lifecycle status.
    /// </summary>
    Task TransitionCommandVersion(Guid versionId, ContractVersionStatus targetStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the allowed target statuses from the provided status.
    /// </summary>
    IReadOnlyCollection<ContractVersionStatus> GetAllowedTargets(ContractVersionStatus currentStatus);
}
