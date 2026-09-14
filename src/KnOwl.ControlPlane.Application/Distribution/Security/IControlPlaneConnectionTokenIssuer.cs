using KnOwl.Contracts.Security;

namespace KnOwl.ControlPlane.Application.Distribution.Security;

/// <summary>
/// Issues short-lived access tokens for inbound Control Plane distribution calls.
/// </summary>
public interface IControlPlaneConnectionTokenIssuer
{
    /// <summary>
    /// Issues a token after validating a Control Plane-owned inbound credential.
    /// </summary>
    Task<ConnectionTokenResponse> Issue(ConnectionTokenRequest request, CancellationToken cancellationToken = default);
}
