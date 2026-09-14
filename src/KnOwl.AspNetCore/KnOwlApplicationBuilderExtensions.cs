using Microsoft.AspNetCore.Builder;

namespace KnOwl.AspNetCore;

/// <summary>
/// Provides application builder extensions for KnOwl.
/// </summary>
public static class KnOwlApplicationBuilderExtensions
{
    /// <summary>
    /// Adds KnOwl HTTP idempotency middleware to the application pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The same application builder for fluent configuration.</returns>
    public static IApplicationBuilder UseKnOwl(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseMiddleware<KnOwlMiddleware>();
    }
}
