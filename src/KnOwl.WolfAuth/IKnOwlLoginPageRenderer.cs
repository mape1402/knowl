using Microsoft.AspNetCore.Http;

namespace KnOwl.WolfAuth;

/// <summary>
/// Renders the KnOwl login shell used by hosts that delegate authentication to WolfAuth.
/// </summary>
public interface IKnOwlLoginPageRenderer
{
    /// <summary>
    /// Renders a login response for the current request.
    /// </summary>
    IResult Render(HttpContext context, KnOwlWolfAuthOptions options);
}
