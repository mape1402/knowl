using System.ComponentModel.DataAnnotations;

namespace KnOwl.ControlPlane.Distribution.Core;

/// <summary>
/// Represents a logical runtime environment that groups runtime nodes.
/// </summary>
public sealed class RuntimeEnvironment
{
    /// <summary>
    /// Runtime environment identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable environment name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Stable environment code.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Optional environment description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether new releases can target this environment.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// UTC timestamp when the environment was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when the environment was last updated.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// Runtime nodes assigned to this environment.
    /// </summary>
    public ICollection<RuntimeNode> RuntimeNodes { get; set; } = [];
}
