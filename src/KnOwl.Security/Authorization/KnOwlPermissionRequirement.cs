using Microsoft.AspNetCore.Authorization;

namespace KnOwl.Security.Authorization;

/// <summary>
/// ASP.NET Core authorization requirement for a KnOwl permission.
/// </summary>
public sealed class KnOwlPermissionRequirement(string permission, KnOwlAuthorizationScope? scope = null) : IAuthorizationRequirement
{
    /// <summary>Required KnOwl permission.</summary>
    public string Permission { get; } = permission;

    /// <summary>Required KnOwl authorization scope.</summary>
    public KnOwlAuthorizationScope Scope { get; } = scope ?? KnOwlAuthorizationScope.Global;
}
