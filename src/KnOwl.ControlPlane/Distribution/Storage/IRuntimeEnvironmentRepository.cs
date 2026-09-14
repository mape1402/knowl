using KnOwl.ControlPlane.Distribution.Core;

namespace KnOwl.ControlPlane.Distribution.Storage;

/// <summary>
/// Defines persistence operations for runtime environments.
/// </summary>
public interface IRuntimeEnvironmentRepository
{
    /// <summary>
    /// Gets all runtime environments.
    /// </summary>
    Task<IReadOnlyList<RuntimeEnvironment>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets enabled runtime environments.
    /// </summary>
    Task<IReadOnlyList<RuntimeEnvironment>> GetEnabled(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an environment by identifier.
    /// </summary>
    Task<RuntimeEnvironment?> GetById(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a runtime environment.
    /// </summary>
    Task Create(RuntimeEnvironment environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a runtime environment.
    /// </summary>
    Task Update(RuntimeEnvironment environment, CancellationToken cancellationToken = default);
}
