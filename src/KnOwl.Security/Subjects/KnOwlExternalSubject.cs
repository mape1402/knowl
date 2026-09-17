namespace KnOwl.Security.Subjects;

/// <summary>
/// Represents the authenticated external identity resolved from host claims.
/// </summary>
public sealed record KnOwlExternalSubject(
    string Provider,
    string SubjectId,
    string? DisplayName,
    string? Email,
    IReadOnlyList<string> ExternalGroupIds);
