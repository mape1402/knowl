using KnOwl.Security.Subjects;

namespace KnOwl.Security.Storage;

/// <summary>
/// Defines provider-agnostic persistence operations for KnOwl authorization data.
/// </summary>
public interface IKnOwlSecurityStore
{
    Task<IReadOnlyList<KnOwlSubject>> GetSubjects(CancellationToken cancellationToken = default);

    Task<KnOwlSubject?> GetSubject(string provider, string subjectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnOwlRoleAssignment>> GetRoleAssignments(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnOwlRoleAssignment>> GetRoleAssignments(string provider, string subjectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnOwlPermissionAssignment>> GetPermissionAssignments(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnOwlPermissionAssignment>> GetPermissionAssignments(string provider, string subjectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnOwlExternalGroupRoleAssignment>> GetExternalGroupRoleAssignments(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnOwlExternalGroupRoleAssignment>> GetExternalGroupRoleAssignments(
        string provider,
        IReadOnlyCollection<string> externalGroupIds,
        CancellationToken cancellationToken = default);

    Task<bool> HasEnabledAdmin(CancellationToken cancellationToken = default);

    Task SynchronizeBootstrapAdmin(KnOwlExternalSubject subject, CancellationToken cancellationToken = default);

    Task<KnOwlSubject> UpsertSubject(KnOwlSubject subject, CancellationToken cancellationToken = default);

    Task<KnOwlRoleAssignment> AssignRole(KnOwlRoleAssignment assignment, CancellationToken cancellationToken = default);

    Task<KnOwlPermissionAssignment> AssignPermission(KnOwlPermissionAssignment assignment, CancellationToken cancellationToken = default);

    Task<KnOwlExternalGroupRoleAssignment> AssignExternalGroupRole(KnOwlExternalGroupRoleAssignment assignment, CancellationToken cancellationToken = default);
}
