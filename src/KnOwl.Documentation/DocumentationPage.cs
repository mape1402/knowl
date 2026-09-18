using System.ComponentModel.DataAnnotations;

namespace KnOwl.Documentation;

/// <summary>
/// Represents a navigable documentation page inside a topic.
/// </summary>
public sealed class DocumentationPage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TopicId { get; set; }

    public DocumentationTopic? Topic { get; set; }

    [Required]
    [MaxLength(120)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public List<DocumentationPageVersion> Versions { get; } = [];
}
