using Microsoft.AspNetCore.Http;

namespace KnOwl.ControlPlane.Application.Distribution.Security;

/// <summary>
/// Authenticates Runtime node requests sent to Control Plane artifact delivery endpoints.
/// </summary>
public interface IArtifactDeliveryEndpointAuthenticator
{
    /// <summary>
    /// Authenticates a Runtime node request.
    /// </summary>
    Task<ArtifactDeliveryEndpointAuthenticationResult> AuthenticateRuntimeNode(
        HttpRequest request,
        Guid runtimeNodeId,
        string body,
        CancellationToken cancellationToken = default);
}
