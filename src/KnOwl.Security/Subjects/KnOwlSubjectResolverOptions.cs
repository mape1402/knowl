using System.Security.Claims;

namespace KnOwl.Security.Subjects;

/// <summary>
/// Configures how KnOwl resolves an external subject from a claims principal.
/// </summary>
public sealed class KnOwlSubjectResolverOptions
{
    /// <summary>Provider key assigned to resolved subjects.</summary>
    public string Provider { get; set; } = "default";

    /// <summary>Claim types tried in order to resolve the stable subject identifier.</summary>
    public List<string> SubjectIdClaimTypes { get; } =
    [
        "oid",
        ClaimTypes.NameIdentifier,
        "sub"
    ];

    /// <summary>Claim types tried in order to resolve the display name.</summary>
    public List<string> DisplayNameClaimTypes { get; } =
    [
        "name",
        ClaimTypes.Name
    ];

    /// <summary>Claim types tried in order to resolve the email address.</summary>
    public List<string> EmailClaimTypes { get; } =
    [
        "preferred_username",
        ClaimTypes.Email,
        "email"
    ];

    /// <summary>Claim types used to resolve external group identifiers.</summary>
    public List<string> GroupClaimTypes { get; } =
    [
        "groups",
        ClaimTypes.GroupSid,
        ClaimTypes.Role
    ];
}
