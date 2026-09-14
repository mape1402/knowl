namespace KnOwl.Contracts.ArtifactDelivery;

/// <summary>
/// Request sent by a Runtime node after pulling and storing an artifact.
/// </summary>
public sealed class RuntimeArtifactPullAckRequest
{
    /// <summary>
    /// Gets or sets the Runtime artifact identifier assigned by the Runtime host.
    /// </summary>
    public string RuntimeArtifactId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Runtime artifact status reported after the artifact was accepted.
    /// </summary>
    public string RuntimeArtifactStatus { get; set; } = string.Empty;
}
