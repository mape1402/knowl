using System.ComponentModel.DataAnnotations;

namespace KnOwl.ControlPlane.Design.Core;

/// <summary>
/// Represents a versioned custom metadata field definition.
/// </summary>
public class ContractFieldMetadataVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ContractFieldMetadataDefinitionId { get; set; }

    [Required]
    [MaxLength(13)]
    public string VersionNumber { get; set; } = "1.0.0";

    [MaxLength(1000)]
    public string? Comment { get; set; }

    [Required]
    public string DefinitionJson { get; set; } = "{}";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ContractFieldMetadataDefinition? ContractFieldMetadataDefinition { get; set; }
}

