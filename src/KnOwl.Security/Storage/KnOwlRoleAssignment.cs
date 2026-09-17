using System.ComponentModel.DataAnnotations;

namespace KnOwl.Security.Storage;

/// <summary>
/// Assigns a KnOwl role to an external subject in a scope.
/// </summary>
public sealed class KnOwlRoleAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Provider { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string SubjectId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Role { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ScopeType { get; set; } = "Global";

    [Required]
    [MaxLength(200)]
    public string ScopeId { get; set; } = "*";

    public KnOwlSecurityAssignmentSource Source { get; set; } = KnOwlSecurityAssignmentSource.Manual;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
