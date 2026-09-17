namespace KnOwl.Security.Authorization;

/// <summary>
/// Describes the resource scope where a KnOwl permission is evaluated.
/// </summary>
public sealed record KnOwlAuthorizationScope(string ScopeType, string ScopeId)
{
    /// <summary>Global scope.</summary>
    public static KnOwlAuthorizationScope Global { get; } = new(KnOwlAuthorizationScopeTypes.Global, "*");

    /// <summary>Control Plane scope.</summary>
    public static KnOwlAuthorizationScope ControlPlane { get; } = new(KnOwlAuthorizationScopeTypes.ControlPlane, "*");

    /// <summary>Runtime scope.</summary>
    public static KnOwlAuthorizationScope Runtime { get; } = new(KnOwlAuthorizationScopeTypes.Runtime, "*");
}
