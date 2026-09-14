namespace KnOwl.AspNetCore.Dashboard;

/// <summary>
/// Authenticates users for the KnOwl dashboard when custom authentication is enabled.
/// </summary>
public interface IKnOwlDashboardAuthenticator
{
    /// <summary>
    /// Authenticates the current dashboard request.
    /// </summary>
    /// <param name="request">The authentication request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The authentication result.</returns>
    Task<KnOwlDashboardAuthResult> AuthenticateAsync(
        KnOwlDashboardAuthRequest request,
        CancellationToken cancellationToken);
}
