using System.ComponentModel.DataAnnotations;

namespace KnOwl.Security.Storage;

/// <summary>
/// Represents an external subject known by KnOwl.
/// </summary>
public sealed class KnOwlSubject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Provider { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string SubjectId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? DisplayName { get; set; }

    [MaxLength(320)]
    public string? Email { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
