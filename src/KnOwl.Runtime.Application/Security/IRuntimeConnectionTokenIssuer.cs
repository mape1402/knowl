using KnOwl.Contracts.Security;

namespace KnOwl.Runtime.Application.Security;

/// <summary>
/// Issues short-lived access tokens for inbound Runtime distribution calls.
/// </summary>
public interface IRuntimeConnectionTokenIssuer
{
    /// <summary>
    /// Issues a token after validating a Runtime-owned inbound credential.
    /// </summary>
    Task<ConnectionTokenResponse> Issue(ConnectionTokenRequest request, CancellationToken cancellationToken = default);
}
