using System.ComponentModel.DataAnnotations;

namespace KnOwl.Documentation;

/// <summary>
/// Captures a semantic version of a documentation page.
/// </summary>
public sealed class DocumentationPageVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PageId { get; set; }

    public DocumentationPage? Page { get; set; }

    [Required]
    [MaxLength(50)]
    public string VersionNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string EntryPath { get; set; } = "index.md";

    [Required]
    [MaxLength(128)]
    public string ContentHash { get; set; } = string.Empty;

    public DocPageVersionStatus Status { get; set; } = DocPageVersionStatus.Draft;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? PublishedAtUtc { get; set; }

    public DateTime? ArchivedAtUtc { get; set; }

    public List<DocumentationAsset> Assets { get; } = [];
}
