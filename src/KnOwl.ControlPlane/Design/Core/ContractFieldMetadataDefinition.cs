using System.ComponentModel.DataAnnotations;

namespace KnOwl.ControlPlane.Design.Core;

/// <summary>
/// Represents a configurable custom metadata field definition.
/// </summary>
public class ContractFieldMetadataDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ContractFieldMetadataVersion> Versions { get; set; } = [];
}

