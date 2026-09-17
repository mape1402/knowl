using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace KnOwl.Security.Subjects;

/// <summary>
/// Resolves KnOwl subjects from configurable claims.
/// </summary>
public sealed class ClaimsKnOwlSubjectResolver(IOptions<KnOwlSecurityOptions> options) : IKnOwlSubjectResolver
{
    private readonly KnOwlSubjectResolverOptions subjectOptions = options.Value.Subject;

    /// <inheritdoc />
    public KnOwlExternalSubject? Resolve(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var subjectId = FindFirstValue(user, subjectOptions.SubjectIdClaimTypes);
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            return null;
        }

        return new KnOwlExternalSubject(
            subjectOptions.Provider,
            subjectId,
            FindFirstValue(user, subjectOptions.DisplayNameClaimTypes),
            FindFirstValue(user, subjectOptions.EmailClaimTypes),
            FindValues(user, subjectOptions.GroupClaimTypes));
    }

    private static string? FindFirstValue(ClaimsPrincipal user, IReadOnlyCollection<string> claimTypes)
        => claimTypes
            .Select(type => user.FindFirst(type)?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static IReadOnlyList<string> FindValues(ClaimsPrincipal user, IReadOnlyCollection<string> claimTypes)
        => user.Claims
            .Where(claim => claimTypes.Contains(claim.Type) && !string.IsNullOrWhiteSpace(claim.Value))
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
