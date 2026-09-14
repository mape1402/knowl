using KnOwl.Contracts.Security;
using KnOwl.Runtime.Distribution;

namespace KnOwl.Runtime.Application.Security;

/// <summary>
/// Provides cached access tokens for outbound Runtime calls into Control Plane.
/// </summary>
public interface IControlPlaneAccessTokenProvider
{
    /// <summary>
    /// Attaches a bearer token to an outbound request.
    /// </summary>
    Task AttachToken(
        HttpRequestMessage request,
        ControlPlaneDistributionSource source,
        IReadOnlyCollection<ArtifactDeliveryScope> scopes,
        CancellationToken cancellationToken = default);
}
