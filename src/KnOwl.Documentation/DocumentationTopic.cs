using System.ComponentModel.DataAnnotations;

namespace KnOwl.Documentation;

/// <summary>
/// Groups related documentation pages inside a documentation space.
/// </summary>
public sealed class DocumentationTopic
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SpaceId { get; set; }

    public DocumentationSpace? Space { get; set; }

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

    public List<DocumentationPage> Pages { get; } = [];
}
