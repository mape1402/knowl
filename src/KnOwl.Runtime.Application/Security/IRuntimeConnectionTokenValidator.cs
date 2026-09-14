using KnOwl.Contracts.Security;

namespace KnOwl.Runtime.Application.Security;

/// <summary>
/// Validates bearer tokens presented to Runtime distribution endpoints.
/// </summary>
public interface IRuntimeConnectionTokenValidator
{
    /// <summary>
    /// Validates a bearer token and required scopes.
    /// </summary>
    Task<ConnectionTokenValidationResult> Validate(
        string token,
        IReadOnlyCollection<ArtifactDeliveryScope> requiredScopes,
        CancellationToken cancellationToken = default);
}
