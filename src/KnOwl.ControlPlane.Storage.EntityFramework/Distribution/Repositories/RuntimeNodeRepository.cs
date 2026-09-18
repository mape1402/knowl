using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Distribution.Repositories;

/// <inheritdoc />
public sealed class RuntimeNodeRepository(KnOwlDbContext db) : IRuntimeNodeRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RuntimeNode>> GetAll(CancellationToken cancellationToken = default)
    {
        var nodes = await db.RuntimeNodes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        await HydrateEnvironments(nodes, cancellationToken);
        return OrderByEnvironment(nodes);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuntimeNode>> GetActiveEnabled(CancellationToken cancellationToken = default)
    {
        var nodes = await db.RuntimeNodes
            .AsNoTracking()
            .Where(x => x.IsEnabled && x.Status == RuntimeNodeStatus.Active)
            .ToListAsync(cancellationToken);

        await HydrateEnvironments(nodes, cancellationToken);
        return OrderByEnvironment(nodes);
    }

    /// <inheritdoc />
    public async Task<RuntimeNode?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var runtimeNode = await db.RuntimeNodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (runtimeNode?.EnvironmentId is not null)
        {
            runtimeNode.Environment = await db.RuntimeEnvironments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == runtimeNode.EnvironmentId.Value, cancellationToken);
        }

        return runtimeNode;
    }

    /// <inheritdoc />
    public async Task<RuntimeNode?> GetByCode(string code, CancellationToken cancellationToken = default)
    {
        return await db.RuntimeNodes
            .FirstOrDefaultAsync(x => x.Code == code.Trim(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RuntimeNode?> GetByInboundClientId(string clientId, CancellationToken cancellationToken = default)
    {
        return await db.RuntimeNodes
            .FirstOrDefaultAsync(x => x.InboundClientId == clientId.Trim(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task Create(RuntimeNode runtimeNode, CancellationToken cancellationToken = default)
    {
        db.RuntimeNodes.Add(runtimeNode);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task Update(RuntimeNode runtimeNode, CancellationToken cancellationToken = default)
    {
        runtimeNode.LastUpdatedAtUtc = DateTime.UtcNow;
        db.RuntimeNodes.Update(runtimeNode);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetIsEnabled(Guid id, bool isEnabled, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        var rows = await db.RuntimeNodes
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsEnabled, isEnabled)
                .SetProperty(x => x.LastUpdatedAtUtc, updatedAtUtc), cancellationToken);

        if (rows == 0)
        {
            throw new KeyNotFoundException($"Runtime node '{id}' was not found.");
        }
    }

    private async Task HydrateEnvironments(IReadOnlyCollection<RuntimeNode> nodes, CancellationToken cancellationToken)
    {
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

    private static IReadOnlyList<RuntimeNode> OrderByEnvironment(IEnumerable<RuntimeNode> nodes)
    {
        return nodes
            .OrderBy(x => x.Environment?.Name ?? x.EnvironmentName)
            .ThenBy(x => x.Name)
            .ToArray();
    }
}
