using System.ComponentModel.DataAnnotations;

namespace KnOwl.Documentation;

/// <summary>
/// Represents a top-level documentation area, such as Orchestrator or KnOwl.
/// </summary>
public sealed class DocumentationSpace
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(120)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public List<DocumentationTopic> Topics { get; } = [];
}
