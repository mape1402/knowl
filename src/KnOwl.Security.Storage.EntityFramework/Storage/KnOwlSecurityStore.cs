using KnOwl.Security.Authorization;
using KnOwl.Security.Storage;
using KnOwl.Security.Storage.EntityFramework.Data;
using KnOwl.Security.Subjects;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.Security.Storage.EntityFramework.Storage;

/// <summary>
/// EF Core implementation of KnOwl security storage.
/// </summary>
public sealed class KnOwlSecurityStore(KnOwlSecurityDbContext db) : IKnOwlSecurityStore
{
    public async Task<IReadOnlyList<KnOwlSubject>> GetSubjects(CancellationToken cancellationToken = default)
        => await db.Subjects
            .AsNoTracking()
            .OrderBy(x => x.Provider)
            .ThenBy(x => x.SubjectId)
            .ToListAsync(cancellationToken);

    public Task<KnOwlSubject?> GetSubject(string provider, string subjectId, CancellationToken cancellationToken = default)
        => db.Subjects.SingleOrDefaultAsync(x => x.Provider == provider && x.SubjectId == subjectId, cancellationToken);

    public async Task<IReadOnlyList<KnOwlRoleAssignment>> GetRoleAssignments(CancellationToken cancellationToken = default)
        => await db.RoleAssignments
            .AsNoTracking()
            .OrderBy(x => x.Provider)
            .ThenBy(x => x.SubjectId)
            .ThenBy(x => x.Role)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<KnOwlRoleAssignment>> GetRoleAssignments(string provider, string subjectId, CancellationToken cancellationToken = default)
        => await db.RoleAssignments
            .AsNoTracking()
            .Where(x => x.Provider == provider && x.SubjectId == subjectId && x.IsEnabled)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<KnOwlPermissionAssignment>> GetPermissionAssignments(CancellationToken cancellationToken = default)
        => await db.PermissionAssignments
            .AsNoTracking()
            .OrderBy(x => x.Provider)
            .ThenBy(x => x.SubjectId)
            .ThenBy(x => x.Permission)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<KnOwlPermissionAssignment>> GetPermissionAssignments(string provider, string subjectId, CancellationToken cancellationToken = default)
        => await db.PermissionAssignments
            .AsNoTracking()
            .Where(x => x.Provider == provider && x.SubjectId == subjectId && x.IsEnabled)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<KnOwlExternalGroupRoleAssignment>> GetExternalGroupRoleAssignments(CancellationToken cancellationToken = default)
        => await db.ExternalGroupRoleAssignments
            .AsNoTracking()
            .OrderBy(x => x.Provider)
            .ThenBy(x => x.ExternalGroupId)
            .ThenBy(x => x.Role)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<KnOwlExternalGroupRoleAssignment>> GetExternalGroupRoleAssignments(
        string provider,
        IReadOnlyCollection<string> externalGroupIds,
        CancellationToken cancellationToken = default)
    {
        if (externalGroupIds.Count == 0)
        {
            return [];
        }

        return await db.ExternalGroupRoleAssignments
            .AsNoTracking()
            .Where(x => x.Provider == provider && x.IsEnabled && externalGroupIds.Contains(x.ExternalGroupId))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasEnabledAdmin(CancellationToken cancellationToken = default)
        => db.RoleAssignments.AnyAsync(x => x.IsEnabled && x.Role == KnOwlRoles.Admin, cancellationToken);

    public async Task SynchronizeBootstrapAdmin(KnOwlExternalSubject subject, CancellationToken cancellationToken = default)
    {
        await UpsertSubject(new KnOwlSubject
        {
            Provider = subject.Provider,
            SubjectId = subject.SubjectId,
            DisplayName = subject.DisplayName,
            Email = subject.Email,
            IsEnabled = true
        }, cancellationToken);

        var hasAdmin = await db.RoleAssignments.AnyAsync(x =>
            x.Provider == subject.Provider &&
            x.SubjectId == subject.SubjectId &&
            x.Role == KnOwlRoles.Admin &&
            x.ScopeType == KnOwlAuthorizationScopeTypes.Global &&
            x.ScopeId == "*",
            cancellationToken);

        if (!hasAdmin)
        {
            db.RoleAssignments.Add(new KnOwlRoleAssignment
            {
                Provider = subject.Provider,
                SubjectId = subject.SubjectId,
                Role = KnOwlRoles.Admin,
                ScopeType = KnOwlAuthorizationScopeTypes.Global,
                ScopeId = "*",
                Source = KnOwlSecurityAssignmentSource.BootstrapConfig
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<KnOwlSubject> UpsertSubject(KnOwlSubject subject, CancellationToken cancellationToken = default)
    {
        var existing = await db.Subjects.SingleOrDefaultAsync(x =>
            x.Provider == subject.Provider &&
            x.SubjectId == subject.SubjectId,
            cancellationToken);

        if (existing is null)
        {
            db.Subjects.Add(subject);
            await db.SaveChangesAsync(cancellationToken);
            return subject;
        }

        existing.DisplayName = subject.DisplayName;
        existing.Email = subject.Email;
        existing.IsEnabled = subject.IsEnabled;
        existing.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<KnOwlRoleAssignment> AssignRole(KnOwlRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        db.RoleAssignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);
        return assignment;
    }

    public async Task<KnOwlPermissionAssignment> AssignPermission(KnOwlPermissionAssignment assignment, CancellationToken cancellationToken = default)
    {
        db.PermissionAssignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);
        return assignment;
    }

    public async Task<KnOwlExternalGroupRoleAssignment> AssignExternalGroupRole(KnOwlExternalGroupRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        db.ExternalGroupRoleAssignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);
        return assignment;
    }
}
