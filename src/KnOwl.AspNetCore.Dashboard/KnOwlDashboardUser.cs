namespace KnOwl.AspNetCore.Dashboard;

/// <summary>
/// Represents an authenticated KnOwl dashboard user.
/// </summary>
public sealed class KnOwlDashboardUser
{
    /// <summary>
    /// Gets or sets the dashboard username.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Gets or sets the display name shown in the dashboard.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets the dashboard roles assigned to the user.
    /// </summary>
    public IList<string> Roles { get; } = [];
}
