using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Application;

/// <inheritdoc />
internal sealed class ContractVersionPromotionService(
    IEventRepository events,
    ICommandRepository commands,
    IContractVersionTransitionPolicy transitionPolicy) : IContractVersionPromotionService
{
    /// <inheritdoc />
    public async Task TransitionEventVersion(Guid versionId, ContractVersionStatus targetStatus, CancellationToken cancellationToken = default)
    {
        var version = await events.GetVersionById(versionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event version '{versionId}' was not found.");

        transitionPolicy.EnsureCanTransition(version.Status, targetStatus);
        await events.UpdateVersionStatus(versionId, targetStatus, DateTime.UtcNow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task TransitionCommandVersion(Guid versionId, ContractVersionStatus targetStatus, CancellationToken cancellationToken = default)
    {
        var version = await commands.GetVersionById(versionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Command version '{versionId}' was not found.");

        transitionPolicy.EnsureCanTransition(version.Status, targetStatus);
        await commands.UpdateVersionStatus(versionId, targetStatus, DateTime.UtcNow, cancellationToken);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ContractVersionStatus> GetAllowedTargets(ContractVersionStatus currentStatus)
    {
        return transitionPolicy.GetAllowedTargets(currentStatus);
    }
}
