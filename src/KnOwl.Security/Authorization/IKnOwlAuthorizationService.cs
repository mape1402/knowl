using System.Security.Claims;

namespace KnOwl.Security.Authorization;

/// <summary>
/// Evaluates KnOwl permissions for authenticated host principals.
/// </summary>
public interface IKnOwlAuthorizationService
{
    /// <summary>
    /// Determines whether the principal has the requested permission in the requested scope.
    /// </summary>
    Task<bool> HasPermission(
        ClaimsPrincipal user,
        string permission,
        KnOwlAuthorizationScope scope,
        CancellationToken cancellationToken = default);
}
