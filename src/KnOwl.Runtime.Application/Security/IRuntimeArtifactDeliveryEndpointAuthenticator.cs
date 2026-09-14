using Microsoft.AspNetCore.Http;

namespace KnOwl.Runtime.Application.Security;

/// <summary>
/// Authenticates Control Plane artifact delivery requests sent to Runtime.
/// </summary>
public interface IRuntimeArtifactDeliveryEndpointAuthenticator
{
    /// <summary>
    /// Authenticates a Control Plane artifact delivery request.
    /// </summary>
    Task<RuntimeArtifactDeliveryEndpointAuthenticationResult> Authenticate(
        HttpRequest request,
        string body,
        CancellationToken cancellationToken = default);
}
