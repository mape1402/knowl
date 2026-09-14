namespace KnOwl.ControlPlane.Application.Distribution.Security;

/// <summary>
/// Coordinates Control Plane-side Runtime node credential exchange.
/// </summary>
public interface IRuntimeNodeConnectionInteractionService
{
    /// <summary>
    /// Generates a credential package that a Runtime host can import to call this Control Plane.
    /// </summary>
    Task<RuntimeNodeCredentialPackageModel> GenerateCredentialPackage(
        Guid runtimeNodeId,
        string issuerBaseUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a Runtime-generated credential package so this Control Plane can call Runtime.
    /// </summary>
    Task ImportCredentialPackage(ImportRuntimeNodeCredentialPackageInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the Runtime connection using imported outbound credentials.
    /// </summary>
    Task<RuntimeNodeConnectionValidationModel> ValidateConnection(Guid runtimeNodeId, CancellationToken cancellationToken = default);
}
