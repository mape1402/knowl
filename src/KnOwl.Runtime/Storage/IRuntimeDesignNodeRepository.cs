using KnOwl.Runtime.Distribution;

namespace KnOwl.Runtime.Storage;

/// <summary>
/// Defines persistence operations for Control Plane nodes trusted by an KnOwl Runtime host.
/// </summary>
public interface IRuntimeDesignNodeRepository
{
    /// <summary>
    /// Gets all configured Control Plane nodes.
    /// </summary>
    Task<IReadOnlyList<RuntimeDesignNode>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a Control Plane node by identifier.
    /// </summary>
    Task<RuntimeDesignNode?> GetById(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a Control Plane node by runtime-local key.
    /// </summary>
    Task<RuntimeDesignNode?> GetByKey(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a Control Plane node by inbound client id used for token issuance.
    /// </summary>
    Task<RuntimeDesignNode?> GetByInboundClientId(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a Control Plane node.
    /// </summary>
    Task Upsert(RuntimeDesignNode designNode, CancellationToken cancellationToken = default);
}
