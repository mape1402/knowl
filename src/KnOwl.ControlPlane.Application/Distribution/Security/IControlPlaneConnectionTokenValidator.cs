using KnOwl.Contracts.Security;

namespace KnOwl.ControlPlane.Application.Distribution.Security;

/// <summary>
/// Validates bearer tokens presented to Control Plane distribution endpoints.
/// </summary>
public interface IControlPlaneConnectionTokenValidator
{
    /// <summary>
    /// Validates a bearer token and required scopes for a runtime node route.
    /// </summary>
    Task<ConnectionTokenValidationResult> Validate(
        string token,
        Guid runtimeNodeId,
        IReadOnlyCollection<ArtifactDeliveryScope> requiredScopes,
        CancellationToken cancellationToken = default);
}
