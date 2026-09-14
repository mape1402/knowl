using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.Contracts.Security;

namespace KnOwl.ControlPlane.Application.Distribution.Security;

/// <summary>
/// Provides cached access tokens for outbound Control Plane calls into Runtime nodes.
/// </summary>
public interface IRuntimeAccessTokenProvider
{
    /// <summary>
    /// Attaches a bearer token to an outbound request.
    /// </summary>
    Task AttachToken(
        HttpRequestMessage request,
        RuntimeNode node,
        IReadOnlyCollection<ArtifactDeliveryScope> scopes,
        CancellationToken cancellationToken = default);
}
