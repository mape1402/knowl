namespace KnOwl.AspNetCore.Dashboard;

/// <summary>
/// Represents the result of dashboard authentication.
/// </summary>
public sealed class KnOwlDashboardAuthResult
{
    /// <summary>
    /// Gets an unauthenticated result.
    /// </summary>
    public static KnOwlDashboardAuthResult Rejected { get; } = new(false);

    /// <summary>
    /// Initializes a new instance of the <see cref="KnOwlDashboardAuthResult"/> class.
    /// </summary>
    /// <param name="succeeded">Whether authentication succeeded.</param>
    /// <param name="user">The authenticated user.</param>
    public KnOwlDashboardAuthResult(bool succeeded, KnOwlDashboardUser user = null)
    {
        Succeeded = succeeded;
        User = user;
    }

    /// <summary>
    /// Gets a value indicating whether authentication succeeded.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the authenticated user.
    /// </summary>
    public KnOwlDashboardUser User { get; }

    /// <summary>
    /// Creates an authenticated result.
    /// </summary>
    /// <param name="user">The authenticated dashboard user.</param>
    /// <returns>The authenticated result.</returns>
    public static KnOwlDashboardAuthResult Success(KnOwlDashboardUser user)
        => new(true, user ?? throw new ArgumentNullException(nameof(user)));
}
