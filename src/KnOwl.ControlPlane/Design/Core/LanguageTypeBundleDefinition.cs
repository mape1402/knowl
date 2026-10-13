using System.ComponentModel.DataAnnotations;

namespace KnOwl.ControlPlane.Design.Core;

/// <summary>
/// Host-owned ButterMorph language type bundle metadata.
/// </summary>
public sealed class LanguageTypeBundleDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string AssemblyId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string AssemblyKey { get; set; } = string.Empty;

    public string? AssemblyDescription { get; set; }

    [Required]
    [MaxLength(64)]
    public string AssemblyVersion { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string LanguageKey { get; set; } = string.Empty;

    [Required]
    public string BundleJson { get; set; } = "{}";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
