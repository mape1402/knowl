using System.ComponentModel.DataAnnotations;

namespace KnOwl.Documentation;

/// <summary>
/// Represents source markdown, original packages, generated files, and related resources for a page version.
/// </summary>
public sealed class DocumentationAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PageVersionId { get; set; }

    public DocumentationPageVersion? PageVersion { get; set; }

    [Required]
    [MaxLength(500)]
    public string LogicalPath { get; set; } = string.Empty;

    [Required]
    [MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long Length { get; set; }

    [Required]
    [MaxLength(128)]
    public string ContentHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string StorageKey { get; set; } = string.Empty;

    public DocAssetKind Kind { get; set; }

    public bool IsDownloadable { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
