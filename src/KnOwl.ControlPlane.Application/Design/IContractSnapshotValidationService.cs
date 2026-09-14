namespace KnOwl.ControlPlane.Application;

/// <summary>
/// Validates embedded payload snapshots before contract versions are promoted.
/// </summary>
public interface IContractSnapshotValidationService
{
    /// <summary>
    /// Validates an event version payload snapshot.
    /// </summary>
    Task<ContractSnapshotValidationResult> ValidateEventVersion(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a command version payload snapshot.
    /// </summary>
    Task<ContractSnapshotValidationResult> ValidateCommandVersion(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a raw payload snapshot JSON document.
    /// </summary>
    Task<ContractSnapshotValidationResult> ValidatePayloadSchema(string payloadSchemaJson, CancellationToken cancellationToken = default);
}
