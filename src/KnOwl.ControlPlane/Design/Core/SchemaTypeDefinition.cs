using System.ComponentModel.DataAnnotations;

namespace KnOwl.ControlPlane.Design.Core;

/// <summary>
/// Represents a system or custom schema type definition.
/// </summary>
public class SchemaTypeDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSystem { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<SchemaTypeVersion> Versions { get; set; } = new List<SchemaTypeVersion>();
}

