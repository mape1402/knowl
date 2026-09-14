using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Application.Distribution.ArtifactDelivery;
using KnOwl.ControlPlane.Distribution.Storage;

namespace KnOwl.ControlPlane.Application.Distribution.ReleaseBundles;

/// <inheritdoc />
internal sealed class ContractReleaseExecutionService(
    IContractReleaseInteractionService releaseService,
    IContractReleaseRepository releases,
    IArtifactDeliveryInteractionService delivery) : IContractReleaseExecutionService
{
    /// <inheritdoc />
    public async Task<ContractReleaseExecutionResult> CreateAndExecute(
        string name,
        string? description,
        IReadOnlyCollection<Guid> artifactIds,
        IReadOnlyCollection<Guid> runtimeNodeIds,
        string rolloutGroup = "ManualRelease",
        string initiatedBy = "distribution",
        CancellationToken cancellationToken = default)
    {
        var release = await releaseService.Create(name, description, artifactIds, cancellationToken);
        await releaseService.Plan(release.Id, runtimeNodeIds, rolloutGroup, cancellationToken);
        return await Execute(release.Id, initiatedBy, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContractReleaseExecutionResult> Execute(
        Guid releaseId,
        string initiatedBy = "distribution",
        CancellationToken cancellationToken = default)
    {
        var release = await releases.GetById(releaseId, includeTargets: true, cancellationToken: cancellationToken)
            ?? throw new KeyNotFoundException($"Release '{releaseId}' was not found.");

        var pendingTargets = release.Targets
            .Where(IsExecutable)
            .OrderBy(x => x.AssignedAtUtc)
            .ToArray();

        List<KnOwl.Contracts.ArtifactDelivery.RuntimeArtifactDeliveryResult> results = [];
        foreach (var target in pendingTargets)
        {
            results.Add(await delivery.Push(target.Id, initiatedBy, cancellationToken));
        }

        return new ContractReleaseExecutionResult
        {
            ReleaseId = releaseId,
            TotalTargets = results.Count,
            Succeeded = results.Count(x => x.Succeeded),
            Failed = results.Count(x => !x.Succeeded),
            AvailableForPull = results.Count(x => string.Equals(x.Status, ContractReleaseTargetStatus.AvailableForPull.ToString(), StringComparison.OrdinalIgnoreCase)),
            Results = results
        };
    }

    private static bool IsExecutable(ContractReleaseTarget target)
    {
        return target.Status is ContractReleaseTargetStatus.Pending
            or ContractReleaseTargetStatus.PushScheduled
            or ContractReleaseTargetStatus.AvailableForPull
            or ContractReleaseTargetStatus.Failed;
    }
}
