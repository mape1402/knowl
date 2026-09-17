using System.Security.Claims;

namespace KnOwl.Security.Subjects;

/// <summary>
/// Resolves a KnOwl external subject from an authenticated host principal.
/// </summary>
public interface IKnOwlSubjectResolver
{
    /// <summary>
    /// Resolves the external subject or returns <c>null</c> when required claims are missing.
    /// </summary>
    KnOwlExternalSubject? Resolve(ClaimsPrincipal user);
}
