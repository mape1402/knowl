using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Distribution.Repositories;

/// <inheritdoc />
public sealed class ContractReleaseTargetRepository(KnOwlDbContext db) : IContractReleaseTargetRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractReleaseTarget>> GetByRelease(Guid releaseId, CancellationToken cancellationToken = default)
    {
        var targets = await db.ContractReleaseTargets
            .AsNoTracking()
            .Where(x => x.ReleaseId == releaseId)
            .ToListAsync(cancellationToken);

        await Hydrate(targets, includeArtifact: true, includeRuntimeNode: true, includeAttempts: true, cancellationToken);
        return targets
            .OrderBy(x => x.RuntimeNode?.Environment?.Name ?? x.RuntimeNode?.EnvironmentName)
            .ThenBy(x => x.RuntimeNode?.Name)
            .ThenBy(x => x.Artifact?.Topic)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractReleaseTarget>> GetPendingForRuntimeNode(Guid runtimeNodeId, CancellationToken cancellationToken = default)
    {
        var targets = await db.ContractReleaseTargets
            .AsNoTracking()
            .Where(x => x.RuntimeNodeId == runtimeNodeId &&
                (x.Status == ContractReleaseTargetStatus.AvailableForPull || x.Status == ContractReleaseTargetStatus.PushScheduled))
            .OrderBy(x => x.AssignedAtUtc)
            .ToListAsync(cancellationToken);

        await Hydrate(targets, includeArtifact: true, includeRuntimeNode: true, includeAttempts: false, cancellationToken);
        return targets;
    }

    /// <inheritdoc />
    public async Task<ContractReleaseTarget?> GetById(Guid id, bool includeArtifact = false, CancellationToken cancellationToken = default)
    {
        var target = await db.ContractReleaseTargets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (target is null)
        {
            return null;
        }

        if (includeArtifact)
        {
            await Hydrate([target], includeArtifact: true, includeRuntimeNode: true, includeAttempts: false, cancellationToken);
        }

        return target;
    }

    /// <inheritdoc />
    public async Task CreateMany(IReadOnlyCollection<ContractReleaseTarget> targets, CancellationToken cancellationToken = default)
    {
        var rows = targets.Select(CloneForInsert).ToArray();
        db.ContractReleaseTargets.AddRange(rows);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task Update(ContractReleaseTarget target, CancellationToken cancellationToken = default)
    {
        db.ChangeTracker.Clear();
        db.ContractReleaseTargets.Update(CloneForInsert(target));
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAttempt(ContractReleaseAttempt attempt, CancellationToken cancellationToken = default)
    {
        db.ContractReleaseAttempts.Add(attempt);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static ContractReleaseTarget CloneForInsert(ContractReleaseTarget target)
    {
        return new ContractReleaseTarget
        {
            Id = target.Id,
            ReleaseId = target.ReleaseId,
            ReleaseItemId = target.ReleaseItemId,
            RuntimeNodeId = target.RuntimeNodeId,
            ArtifactId = target.ArtifactId,
            RolloutGroup = target.RolloutGroup,
            Status = target.Status,
            ActivationStatus = target.ActivationStatus,
            AssignedAtUtc = target.AssignedAtUtc,
            AvailableAtUtc = target.AvailableAtUtc,
            DeliveredAtUtc = target.DeliveredAtUtc,
            AcknowledgedAtUtc = target.AcknowledgedAtUtc,
            ActivatedAtUtc = target.ActivatedAtUtc,
            FailedAtUtc = target.FailedAtUtc,
            FailureReason = target.FailureReason,
            RuntimeVersionApplied = target.RuntimeVersionApplied,
            CorrelationId = target.CorrelationId
        };
    }

    private async Task Hydrate(
        IReadOnlyCollection<ContractReleaseTarget> targets,
        bool includeArtifact,
        bool includeRuntimeNode,
        bool includeAttempts,
        CancellationToken cancellationToken)
    {
        if (targets.Count == 0)
        {
            return;
        }

        if (includeArtifact)
        {
            var artifactIds = targets.Select(x => x.ArtifactId).Distinct().ToArray();
            var artifacts = await db.ContractArtifacts
                .AsNoTracking()
                .Where(x => artifactIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            foreach (var target in targets)
            {
                if (artifacts.TryGetValue(target.ArtifactId, out var artifact))
                {
                    target.Artifact = artifact;
                }
            }
        }

        if (includeRuntimeNode)
        {
            var runtimeNodeIds = targets.Select(x => x.RuntimeNodeId).Distinct().ToArray();
            var runtimeNodes = await db.RuntimeNodes
                .AsNoTracking()
                .Where(x => runtimeNodeIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            await HydrateRuntimeNodeEnvironments(runtimeNodes.Values, cancellationToken);

            foreach (var target in targets)
            {
                if (runtimeNodes.TryGetValue(target.RuntimeNodeId, out var runtimeNode))
                {
                    target.RuntimeNode = runtimeNode;
                }
            }
        }

        if (includeAttempts)
        {
            var targetIds = targets.Select(x => x.Id).Distinct().ToArray();
            var attempts = await db.ContractReleaseAttempts
                .AsNoTracking()
                .Where(x => targetIds.Contains(x.ReleaseTargetId))
                .OrderByDescending(x => x.StartedAtUtc)
                .ToListAsync(cancellationToken);
            var attemptsByTarget = attempts.ToLookup(x => x.ReleaseTargetId);

            foreach (var target in targets)
            {
                target.Attempts = attemptsByTarget[target.Id].ToList();
            }
        }
    }

    private async Task HydrateRuntimeNodeEnvironments(IEnumerable<RuntimeNode> runtimeNodes, CancellationToken cancellationToken)
    {
        var nodes = runtimeNodes.ToArray();
        var environmentIds = nodes
            .Select(x => x.EnvironmentId)
            .OfType<Guid>()
            .Distinct()
            .ToArray();

        if (environmentIds.Length == 0)
        {
            return;
        }

        var environments = await db.RuntimeEnvironments
            .AsNoTracking()
            .Where(x => environmentIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var node in nodes)
        {
            if (node.EnvironmentId is Guid environmentId && environments.TryGetValue(environmentId, out var environment))
            {
                node.Environment = environment;
            }
        }
    }
}
