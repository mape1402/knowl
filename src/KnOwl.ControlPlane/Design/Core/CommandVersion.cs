using System.ComponentModel.DataAnnotations;

namespace KnOwl.ControlPlane.Design.Core;

/// <summary>
/// Represents a versioned payload schema for a command contract.
/// </summary>
public class CommandVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CommandDefinitionId { get; set; }
    public CommandDefinition CommandDefinition { get; set; } = null!;

    [Required]
    [MaxLength(13)]
    public string VersionNumber { get; set; } = string.Empty;

    [Required]
    public string PayloadSchemaJson { get; set; } = "{}";

    /// <summary>
    /// Optional schema returned by the command handler.
    /// </summary>
    public string? ReplyPayloadSchemaJson { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public ContractVersionStatus Status { get; set; } = ContractVersionStatus.Draft;

    public DateTime? InReviewAtUtc { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }

    public DateTime? DeployedAtUtc { get; set; }

    public DateTime? DeprecatedAtUtc { get; set; }

    public DateTime? ArchivedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

