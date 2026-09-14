using Microsoft.AspNetCore.Http;

namespace KnOwl.AspNetCore.Dashboard;

/// <summary>
/// HTTP-context-backed implementation of <see cref="IKnOwlDashboardUserAccessor"/>.
/// </summary>
public sealed class KnOwlDashboardUserAccessor : IKnOwlDashboardUserAccessor
{
    internal const string ItemKey = "__KnOwlDashboardUser";
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnOwlDashboardUserAccessor"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public KnOwlDashboardUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public KnOwlDashboardUser Current
        => _httpContextAccessor.HttpContext?.Items[ItemKey] as KnOwlDashboardUser;
}
