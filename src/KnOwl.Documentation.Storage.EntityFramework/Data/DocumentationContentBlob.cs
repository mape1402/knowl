using System.ComponentModel.DataAnnotations;

namespace KnOwl.Documentation.Storage.EntityFramework.Data;

/// <summary>
/// Stores documentation content when the database content store is used.
/// </summary>
public sealed class DocumentationContentBlob
{
    [Key]
    [MaxLength(500)]
    public string StorageKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ContentType { get; set; } = "application/octet-stream";

    public byte[] Content { get; set; } = [];

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
