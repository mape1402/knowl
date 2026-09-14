namespace KnOwl.ControlPlane.Distribution.Core;

/// <summary>
/// Records one distribution action attempted against a release target.
/// </summary>
public sealed class ContractReleaseAttempt
{
    /// <summary>
    /// Attempt identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Release target affected by the attempt.
    /// </summary>
    public Guid ReleaseTargetId { get; set; }

    /// <summary>
    /// Action performed by the distribution flow, for example Push, Pull or Ack.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Actor or process that initiated the attempt.
    /// </summary>
    public string InitiatedBy { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the attempt started.
    /// </summary>
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when the attempt finished.
    /// </summary>
    public DateTime? FinishedAtUtc { get; set; }

    /// <summary>
    /// Indicates whether the attempt finished successfully.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// Machine-readable failure code when the attempt failed.
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable failure details when the attempt failed.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// External runtime reference produced by the attempt.
    /// </summary>
    public string ExternalReference { get; set; } = string.Empty;

    /// <summary>
    /// Release target navigation.
    /// </summary>
    public ContractReleaseTarget? ReleaseTarget { get; set; }
}
