using System.ComponentModel.DataAnnotations;

namespace KnOwl.Security.Storage;

/// <summary>
/// Maps an external identity-provider group to a KnOwl role in a scope.
/// </summary>
public sealed class KnOwlExternalGroupRoleAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Provider { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ExternalGroupId { get; set; } = string.Empty;

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
