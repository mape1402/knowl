using KnOwl.Security.Authorization;
using KnOwl.Security.Subjects;

namespace KnOwl.Security.Storage;

/// <summary>
/// In-memory security store useful for tests and hosts that provide authorization only through bootstrap configuration.
/// </summary>
public sealed class InMemoryKnOwlSecurityStore : IKnOwlSecurityStore
{
    private readonly List<KnOwlSubject> subjects = [];
    private readonly List<KnOwlRoleAssignment> roleAssignments = [];
    private readonly List<KnOwlPermissionAssignment> permissionAssignments = [];
    private readonly List<KnOwlExternalGroupRoleAssignment> groupRoleAssignments = [];

    public Task<IReadOnlyList<KnOwlSubject>> GetSubjects(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KnOwlSubject>>(subjects.ToArray());

    public Task<KnOwlSubject?> GetSubject(string provider, string subjectId, CancellationToken cancellationToken = default)
        => Task.FromResult(subjects.SingleOrDefault(x => SameSubject(x.Provider, x.SubjectId, provider, subjectId)));

    public Task<IReadOnlyList<KnOwlRoleAssignment>> GetRoleAssignments(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KnOwlRoleAssignment>>(roleAssignments.ToArray());

    public Task<IReadOnlyList<KnOwlRoleAssignment>> GetRoleAssignments(string provider, string subjectId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KnOwlRoleAssignment>>(roleAssignments.Where(x => SameSubject(x.Provider, x.SubjectId, provider, subjectId)).ToArray());

    public Task<IReadOnlyList<KnOwlPermissionAssignment>> GetPermissionAssignments(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KnOwlPermissionAssignment>>(permissionAssignments.ToArray());

    public Task<IReadOnlyList<KnOwlPermissionAssignment>> GetPermissionAssignments(string provider, string subjectId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KnOwlPermissionAssignment>>(permissionAssignments.Where(x => SameSubject(x.Provider, x.SubjectId, provider, subjectId)).ToArray());

    public Task<IReadOnlyList<KnOwlExternalGroupRoleAssignment>> GetExternalGroupRoleAssignments(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KnOwlExternalGroupRoleAssignment>>(groupRoleAssignments.ToArray());

    public Task<IReadOnlyList<KnOwlExternalGroupRoleAssignment>> GetExternalGroupRoleAssignments(
        string provider,
        IReadOnlyCollection<string> externalGroupIds,
        CancellationToken cancellationToken = default)
    {
        var groups = externalGroupIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Task.FromResult<IReadOnlyList<KnOwlExternalGroupRoleAssignment>>(
            groupRoleAssignments
                .Where(x => string.Equals(x.Provider, provider, StringComparison.OrdinalIgnoreCase) && groups.Contains(x.ExternalGroupId))
                .ToArray());
    }

    public Task<bool> HasEnabledAdmin(CancellationToken cancellationToken = default)
        => Task.FromResult(roleAssignments.Any(x => x.IsEnabled && string.Equals(x.Role, KnOwlRoles.Admin, StringComparison.OrdinalIgnoreCase)));

    public Task SynchronizeBootstrapAdmin(KnOwlExternalSubject subject, CancellationToken cancellationToken = default)
    {
        UpsertSubject(new KnOwlSubject
        {
            Provider = subject.Provider,
            SubjectId = subject.SubjectId,
            DisplayName = subject.DisplayName,
            Email = subject.Email,
            IsEnabled = true
        }, cancellationToken);

        if (!roleAssignments.Any(x => SameSubject(x.Provider, x.SubjectId, subject.Provider, subject.SubjectId) &&
                                      string.Equals(x.Role, KnOwlRoles.Admin, StringComparison.OrdinalIgnoreCase)))
        {
            roleAssignments.Add(new KnOwlRoleAssignment
            {
                Provider = subject.Provider,
                SubjectId = subject.SubjectId,
                Role = KnOwlRoles.Admin,
                ScopeType = KnOwlAuthorizationScopeTypes.Global,
                ScopeId = "*",
                Source = KnOwlSecurityAssignmentSource.BootstrapConfig
            });
        }

        return Task.CompletedTask;
    }

    public Task<KnOwlSubject> UpsertSubject(KnOwlSubject subject, CancellationToken cancellationToken = default)
    {
        var existing = subjects.SingleOrDefault(x => SameSubject(x.Provider, x.SubjectId, subject.Provider, subject.SubjectId));
        if (existing is null)
        {
            subjects.Add(subject);
            return Task.FromResult(subject);
        }

        existing.DisplayName = subject.DisplayName;
        existing.Email = subject.Email;
        existing.IsEnabled = subject.IsEnabled;
        existing.UpdatedAtUtc = DateTime.UtcNow;
        return Task.FromResult(existing);
    }

    public Task<KnOwlRoleAssignment> AssignRole(KnOwlRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        roleAssignments.Add(assignment);
        return Task.FromResult(assignment);
    }

    public Task<KnOwlPermissionAssignment> AssignPermission(KnOwlPermissionAssignment assignment, CancellationToken cancellationToken = default)
    {
        permissionAssignments.Add(assignment);
        return Task.FromResult(assignment);
    }

    public Task<KnOwlExternalGroupRoleAssignment> AssignExternalGroupRole(KnOwlExternalGroupRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        groupRoleAssignments.Add(assignment);
        return Task.FromResult(assignment);
    }

    private static bool SameSubject(string leftProvider, string leftSubjectId, string rightProvider, string rightSubjectId)
        => string.Equals(leftProvider, rightProvider, StringComparison.OrdinalIgnoreCase) &&
           string.Equals(leftSubjectId, rightSubjectId, StringComparison.OrdinalIgnoreCase);
}
