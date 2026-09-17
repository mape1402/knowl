using System.Security.Claims;
using KnOwl.Security.Storage;
using KnOwl.Security.Subjects;
using Microsoft.Extensions.Options;

namespace KnOwl.Security.Authorization;

/// <inheritdoc />
public sealed class KnOwlAuthorizationService(
    IKnOwlSubjectResolver subjectResolver,
    IKnOwlSecurityStore store,
    IOptions<KnOwlSecurityOptions> options) : IKnOwlAuthorizationService
{
    private readonly KnOwlSecurityOptions securityOptions = options.Value;

    /// <inheritdoc />
    public async Task<bool> HasPermission(
        ClaimsPrincipal user,
        string permission,
        KnOwlAuthorizationScope scope,
        CancellationToken cancellationToken = default)
    {
        var subject = subjectResolver.Resolve(user);
        if (subject is null)
        {
            return false;
        }

        if (IsBootstrapAdmin(subject))
        {
            if (securityOptions.AllowBootstrapAdminSync)
            {
                await store.SynchronizeBootstrapAdmin(subject, cancellationToken);
            }

            return true;
        }

        var knownSubject = await store.GetSubject(subject.Provider, subject.SubjectId, cancellationToken);
        if (securityOptions.RequireKnownSubject && knownSubject?.IsEnabled != true)
        {
            return false;
        }

        if (knownSubject is { IsEnabled: false })
        {
            return false;
        }

        var directPermissions = await store.GetPermissionAssignments(subject.Provider, subject.SubjectId, cancellationToken);
        if (directPermissions.Any(assignment => GrantsPermission(assignment, permission, scope)))
        {
            return true;
        }

        var directRoles = await store.GetRoleAssignments(subject.Provider, subject.SubjectId, cancellationToken);
        if (directRoles.Any(assignment => GrantsRole(assignment, permission, scope)))
        {
            return true;
        }

        var groupRoles = await store.GetExternalGroupRoleAssignments(subject.Provider, subject.ExternalGroupIds, cancellationToken);
        return groupRoles.Any(assignment => GrantsRole(assignment, permission, scope));
    }

    private bool IsBootstrapAdmin(KnOwlExternalSubject subject)
        => securityOptions.BootstrapAdmins.Any(admin =>
            string.Equals(admin.Provider, subject.Provider, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(admin.SubjectId, subject.SubjectId, StringComparison.OrdinalIgnoreCase));

    private static bool GrantsPermission(KnOwlPermissionAssignment assignment, string permission, KnOwlAuthorizationScope scope)
        => assignment.IsEnabled &&
           (string.Equals(assignment.Permission, "*", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assignment.Permission, permission, StringComparison.OrdinalIgnoreCase)) &&
           ScopeMatches(assignment.ScopeType, assignment.ScopeId, scope);

    private static bool GrantsRole(KnOwlRoleAssignment assignment, string permission, KnOwlAuthorizationScope scope)
        => assignment.IsEnabled &&
           ScopeMatches(assignment.ScopeType, assignment.ScopeId, scope) &&
           RoleGrants(assignment.Role, permission);

    private static bool GrantsRole(KnOwlExternalGroupRoleAssignment assignment, string permission, KnOwlAuthorizationScope scope)
        => assignment.IsEnabled &&
           ScopeMatches(assignment.ScopeType, assignment.ScopeId, scope) &&
           RoleGrants(assignment.Role, permission);

    private static bool RoleGrants(string role, string permission)
    {
        var rolePermissions = KnOwlRolePermissionCatalog.GetPermissions(role);
        return rolePermissions.Contains("*") || rolePermissions.Contains(permission);
    }

    private static bool ScopeMatches(string assignmentScopeType, string assignmentScopeId, KnOwlAuthorizationScope requestedScope)
    {
        if (string.Equals(assignmentScopeType, KnOwlAuthorizationScopeTypes.Global, StringComparison.OrdinalIgnoreCase) &&
            assignmentScopeId == "*")
        {
            return true;
        }

        return string.Equals(assignmentScopeType, requestedScope.ScopeType, StringComparison.OrdinalIgnoreCase) &&
               (assignmentScopeId == "*" || string.Equals(assignmentScopeId, requestedScope.ScopeId, StringComparison.OrdinalIgnoreCase));
    }
}
