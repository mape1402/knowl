namespace KnOwl.Security.Authorization;

/// <summary>
/// Defines built-in KnOwl role names.
/// </summary>
public static class KnOwlRoles
{
    /// <summary>Read-only user.</summary>
    public const string Reader = "KnOwl.Reader";

    /// <summary>Contract designer user.</summary>
    public const string Designer = "KnOwl.Designer";

    /// <summary>Release manager user.</summary>
    public const string ReleaseManager = "KnOwl.ReleaseManager";

    /// <summary>Runtime operator user.</summary>
    public const string RuntimeOperator = "KnOwl.RuntimeOperator";

    /// <summary>Security administrator user.</summary>
    public const string SecurityAdmin = "KnOwl.SecurityAdmin";

    /// <summary>Administrator user with wildcard access.</summary>
    public const string Admin = "KnOwl.Admin";
}
