using KnOwl.Contracts.Distribution;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;

namespace KnOwl.ControlPlane.Application.Distribution.ReleaseBundles;

/// <inheritdoc />
internal sealed class ContractReleaseInteractionService(
    IContractArtifactRepository artifacts,
    IContractReleaseRepository releases,
    IRuntimeNodeRepository runtimeNodes,
    IContractReleaseTargetRepository releaseTargets) : IContractReleaseInteractionService
{
    /// <inheritdoc />
    public async Task<ContractRelease> Create(string name, string? description, IReadOnlyCollection<Guid> artifactIds, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Release name is required.", nameof(name));
        }

        if (artifactIds.Count == 0)
        {
            throw new InvalidOperationException("A release requires at least one artifact.");
        }

        var distinctIds = artifactIds.Distinct().ToArray();
        var selectedArtifacts = await artifacts.GetByIds(distinctIds, cancellationToken);
        if (selectedArtifacts.Count != distinctIds.Length)
        {
            var found = selectedArtifacts.Select(x => x.Id).ToHashSet();
            var missing = distinctIds.Where(x => !found.Contains(x));
            throw new InvalidOperationException($"Release contains missing artifacts: {string.Join(", ", missing)}.");
        }

        var duplicate = selectedArtifacts
            .GroupBy(x => $"{x.ArtifactType}:{x.Topic}:{x.VersionNumber}", StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Release contains duplicate artifact identity '{duplicate.Key}'.");
        }

        ContractRelease release = new()
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        foreach (var artifact in selectedArtifacts)
        {
            release.Items.Add(new ContractReleaseItem
            {
                ReleaseId = release.Id,
                ArtifactId = artifact.Id,
                CreatedAtUtc = release.CreatedAtUtc
            });
        }

        await releases.Create(release, cancellationToken);
        return release;
    }

    /// <inheritdoc />
    public async Task<ContractRelease> Plan(Guid releaseId, IReadOnlyCollection<Guid> runtimeNodeIds, string rolloutGroup = "ManualRelease", CancellationToken cancellationToken = default)
    {
        if (runtimeNodeIds.Count == 0)
        {
            throw new InvalidOperationException("A release plan requires at least one runtime node.");
        }

        var release = await releases.GetById(releaseId, includeItems: true, includeTargets: true, cancellationToken: cancellationToken)
            ?? throw new KeyNotFoundException($"Release '{releaseId}' was not found.");

        if (release.Items.Count == 0)
        {
            throw new InvalidOperationException($"Release '{releaseId}' does not contain artifacts.");
        }

        if (release.Status == ContractReleaseStatus.Canceled)
        {
            throw new InvalidOperationException($"Release '{releaseId}' is canceled and cannot be planned.");
        }

        var distinctRuntimeNodeIds = runtimeNodeIds.Distinct().ToArray();
        List<RuntimeNode> selectedNodes = [];
        foreach (var runtimeNodeId in distinctRuntimeNodeIds)
        {
            var runtimeNode = await runtimeNodes.GetById(runtimeNodeId, cancellationToken)
                ?? throw new InvalidOperationException($"Runtime node '{runtimeNodeId}' was not found.");

            if (!runtimeNode.IsEnabled || runtimeNode.Status != RuntimeNodeStatus.Active)
            {
                throw new InvalidOperationException($"Runtime node '{runtimeNode.Name}' is not active and enabled.");
            }

            selectedNodes.Add(runtimeNode);
        }

        var now = DateTime.UtcNow;
        var existingKeys = release.Targets
            .Select(x => $"{x.ReleaseItemId:N}:{x.RuntimeNodeId:N}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<ContractReleaseTarget> targets = [];

        foreach (var item in release.Items)
        {
            foreach (var runtimeNode in selectedNodes)
            {
                var key = $"{item.Id:N}:{runtimeNode.Id:N}";
                if (existingKeys.Contains(key))
                {
                    continue;
                }

                targets.Add(new ContractReleaseTarget
                {
                    ReleaseId = release.Id,
                    ReleaseItemId = item.Id,
                    ArtifactId = item.ArtifactId,
                    RuntimeNodeId = runtimeNode.Id,
                    RolloutGroup = string.IsNullOrWhiteSpace(rolloutGroup) ? "ManualRelease" : rolloutGroup.Trim(),
                    Status = GetInitialTargetStatus(runtimeNode.DistributionMode),
                    ActivationStatus = ContractReleaseActivationStatus.NotActivated,
                    AssignedAtUtc = now,
                    AvailableAtUtc = runtimeNode.DistributionMode == DistributionMode.Pull ? now : null,
                    CorrelationId = release.Id.ToString("N")
                });
            }
        }

        if (targets.Count > 0)
        {
            await releaseTargets.CreateMany(targets, cancellationToken);
        }

        if (release.Status != ContractReleaseStatus.InProgress)
        {
            await releases.UpdateStatus(release.Id, ContractReleaseStatus.InProgress, now, cancellationToken);
            release.Status = ContractReleaseStatus.InProgress;
        }

        var knownTargetIds = release.Targets.Select(x => x.Id).ToHashSet();
        foreach (var target in targets.Where(target => knownTargetIds.Add(target.Id)))
        {
            release.Targets.Add(target);
        }

        return release;
    }

    private static ContractReleaseTargetStatus GetInitialTargetStatus(DistributionMode mode)
    {
        return mode == DistributionMode.Pull
            ? ContractReleaseTargetStatus.AvailableForPull
            : ContractReleaseTargetStatus.PushScheduled;
    }
}


