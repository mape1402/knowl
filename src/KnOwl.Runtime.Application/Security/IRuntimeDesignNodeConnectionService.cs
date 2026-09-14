using KnOwl.Runtime.Distribution;

namespace KnOwl.Runtime.Application.Security;

/// <summary>
/// Coordinates Runtime-side Control Plane credential exchange.
/// </summary>
public interface IRuntimeDesignNodeConnectionService
{
    /// <summary>
    /// Creates or updates a Control Plane node in Runtime storage.
    /// </summary>
    Task<RuntimeDesignNode> UpsertDesignNode(
        Guid? id,
        string key,
        string name,
        string endpointBaseUri,
        string remoteRuntimeNodeId,
        bool isEnabled,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a credential package that the Control Plane can import to push artifacts into Runtime.
    /// </summary>
    Task<RuntimeDesignNodeCredentialPackageModel> GenerateCredentialPackage(
        Guid designNodeId,
        string issuerBaseUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a Control Plane-generated credential package so Runtime can pull from Control Plane.
    /// </summary>
    Task ImportCredentialPackage(ImportRuntimeDesignNodeCredentialPackageInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the Control Plane connection using imported outbound credentials.
    /// </summary>
    Task<RuntimeDesignNodeConnectionValidationModel> ValidateConnection(Guid designNodeId, CancellationToken cancellationToken = default);
}
