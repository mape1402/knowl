using KnOwl.ControlPlane.Distribution.Core;

namespace KnOwl.ControlPlane.Distribution.Storage;

/// <summary>
/// Defines persistence operations for runtime nodes available to Distribution.
/// </summary>
public interface IRuntimeNodeRepository
{
    /// <summary>
    /// Gets all registered runtime nodes.
    /// </summary>
    Task<IReadOnlyList<RuntimeNode>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active and enabled runtime nodes.
    /// </summary>
    Task<IReadOnlyList<RuntimeNode>> GetActiveEnabled(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a runtime node by identifier.
    /// </summary>
    Task<RuntimeNode?> GetById(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a runtime node by stable code.
    /// </summary>
    Task<RuntimeNode?> GetByCode(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a runtime node by the inbound client id used for token issuance.
    /// </summary>
    Task<RuntimeNode?> GetByInboundClientId(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new runtime node.
    /// </summary>
    Task Create(RuntimeNode runtimeNode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing runtime node.
    /// </summary>
    Task Update(RuntimeNode runtimeNode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables a runtime node without deleting it.
    /// </summary>
    Task SetIsEnabled(Guid id, bool isEnabled, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
}
