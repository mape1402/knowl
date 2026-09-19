using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Distribution.Repositories;

/// <inheritdoc />
public sealed class ContractReleaseRepository(KnOwlDbContext db) : IContractReleaseRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractRelease>> GetAll(CancellationToken cancellationToken = default)
    {
        var releases = await db.ContractReleases
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        await HydrateItems(releases, cancellationToken);
        return releases;
    }

    /// <inheritdoc />
    public async Task<ContractRelease?> GetById(Guid id, bool includeItems = false, bool includeTargets = false, CancellationToken cancellationToken = default)
    {
        var release = await db.ContractReleases
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (release is null)
        {
            return null;
        }

        if (includeItems)
        {
            await HydrateItems([release], cancellationToken);
        }

        if (includeTargets)
        {
            await HydrateTargets([release], cancellationToken);
        }

        return release;
    }

    /// <inheritdoc />
    public async Task Create(ContractRelease release, CancellationToken cancellationToken = default)
    {
        db.ContractReleases.Add(release);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateStatus(Guid id, ContractReleaseStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default)
    {
        var release = await db.ContractReleases.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (release is null)
        {
            throw new KeyNotFoundException($"Release '{id}' was not found.");
        }

        release.Status = status;
        switch (status)
        {
            case ContractReleaseStatus.Deployed:
                release.DeployedAtUtc = changedAtUtc;
                break;
            case ContractReleaseStatus.Completed:
                release.CompletedAtUtc = changedAtUtc;
                break;
            case ContractReleaseStatus.Failed:
                release.FailedAtUtc = changedAtUtc;
                break;
            case ContractReleaseStatus.Canceled:
                release.CanceledAtUtc = changedAtUtc;
                break;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task HydrateItems(IReadOnlyCollection<ContractRelease> releases, CancellationToken cancellationToken)
    {
        var releaseIds = releases.Select(x => x.Id).ToArray();
        if (releaseIds.Length == 0)
        {
            return;
        }

        var items = await db.ContractReleaseItems
            .AsNoTracking()
            .Where(x => releaseIds.Contains(x.ReleaseId))
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        await HydrateItemArtifacts(items, cancellationToken);

        var itemsByRelease = items.ToLookup(x => x.ReleaseId);
        foreach (var release in releases)
        {
            release.Items = itemsByRelease[release.Id].ToList();
        }
    }

    private async Task HydrateTargets(IReadOnlyCollection<ContractRelease> releases, CancellationToken cancellationToken)
    {
        var releaseIds = releases.Select(x => x.Id).ToArray();
        if (releaseIds.Length == 0)
        {
            return;
        }

        var targets = await db.ContractReleaseTargets
            .AsNoTracking()
            .Where(x => releaseIds.Contains(x.ReleaseId))
            .OrderBy(x => x.AssignedAtUtc)
            .ToListAsync(cancellationToken);

        await HydrateTargetDetails(targets, cancellationToken);

        var targetsByRelease = targets.ToLookup(x => x.ReleaseId);
        foreach (var release in releases)
        {
            release.Targets = targetsByRelease[release.Id].ToList();
        }
    }

    private async Task HydrateItemArtifacts(IReadOnlyCollection<ContractReleaseItem> items, CancellationToken cancellationToken)
    {
        var artifactIds = items.Select(x => x.ArtifactId).Distinct().ToArray();
        if (artifactIds.Length == 0)
        {
            return;
        }

        var artifacts = await db.ContractArtifacts
            .AsNoTracking()
            .Where(x => artifactIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var item in items)
        {
            if (artifacts.TryGetValue(item.ArtifactId, out var artifact))
            {
                item.Artifact = artifact;
            }
        }
    }

    private async Task HydrateTargetDetails(IReadOnlyCollection<ContractReleaseTarget> targets, CancellationToken cancellationToken)
    {
        var artifactIds = targets.Select(x => x.ArtifactId).Distinct().ToArray();
        var runtimeNodeIds = targets.Select(x => x.RuntimeNodeId).Distinct().ToArray();
        var targetIds = targets.Select(x => x.Id).Distinct().ToArray();

        var artifacts = artifactIds.Length == 0
            ? []
            : await db.ContractArtifacts
                .AsNoTracking()
                .Where(x => artifactIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

        var runtimeNodes = runtimeNodeIds.Length == 0
            ? []
            : await db.RuntimeNodes
                .AsNoTracking()
                .Where(x => runtimeNodeIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

        await HydrateRuntimeNodeEnvironments(runtimeNodes.Values, cancellationToken);

        var attempts = targetIds.Length == 0
            ? []
            : await db.ContractReleaseAttempts
                .AsNoTracking()
                .Where(x => targetIds.Contains(x.ReleaseTargetId))
                .OrderByDescending(x => x.StartedAtUtc)
                .ToListAsync(cancellationToken);

        var attemptsByTarget = attempts.ToLookup(x => x.ReleaseTargetId);
        foreach (var target in targets)
        {
            if (artifacts.TryGetValue(target.ArtifactId, out var artifact))
            {
                target.Artifact = artifact;
            }

            if (runtimeNodes.TryGetValue(target.RuntimeNodeId, out var runtimeNode))
            {
                target.RuntimeNode = runtimeNode;
            }

            target.Attempts = attemptsByTarget[target.Id].ToList();
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
