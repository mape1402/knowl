using System.ComponentModel.DataAnnotations;

namespace KnOwl.ControlPlane.Design.Core;

/// <summary>
/// Represents a versioned schema definition for a schema type.
/// </summary>
public class SchemaTypeVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SchemaTypeDefinitionId { get; set; }

    public SchemaTypeDefinition? SchemaTypeDefinition { get; set; }

    [Required]
    [MaxLength(13)]
    public string VersionNumber { get; set; } = string.Empty;

    [Required]
    public string DefinitionJson { get; set; } = "{}";

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

