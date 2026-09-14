namespace KnOwl.AspNetCore.Dashboard;

/// <summary>
/// Provides access to the current dashboard user.
/// </summary>
public interface IKnOwlDashboardUserAccessor
{
    /// <summary>
    /// Gets the current dashboard user.
    /// </summary>
    KnOwlDashboardUser Current { get; }
}
