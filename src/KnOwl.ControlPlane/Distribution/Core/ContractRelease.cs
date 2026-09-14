using System.ComponentModel.DataAnnotations;

namespace KnOwl.ControlPlane.Distribution.Core;

/// <summary>
/// Represents a deployable bundle of contract artifacts.
/// </summary>
public sealed class ContractRelease
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ContractReleaseStatus Status { get; set; } = ContractReleaseStatus.Draft;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Legacy timestamp kept only to avoid dropping existing release columns.
    /// </summary>
    public DateTime? InReviewAtUtc { get; set; }

    /// <summary>
    /// Legacy timestamp kept only to avoid dropping existing release columns.
    /// </summary>
    public DateTime? ApprovedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime? FailedAtUtc { get; set; }

    public DateTime? DeployedAtUtc { get; set; }

    public DateTime? CanceledAtUtc { get; set; }

    public ICollection<ContractReleaseItem> Items { get; set; } = [];

    public ICollection<ContractReleaseTarget> Targets { get; set; } = [];
}
