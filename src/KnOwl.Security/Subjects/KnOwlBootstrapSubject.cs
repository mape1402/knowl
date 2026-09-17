namespace KnOwl.Security.Subjects;

/// <summary>
/// Defines an external subject that receives bootstrap administrator access.
/// </summary>
public sealed class KnOwlBootstrapSubject
{
    /// <summary>External identity provider key.</summary>
    public string Provider { get; set; } = "default";

    /// <summary>External subject identifier.</summary>
    public string SubjectId { get; set; } = string.Empty;
}
