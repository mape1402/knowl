using KnOwl.Contracts.Artifacts;

namespace KnOwl.ControlPlane.Distribution.Core;

/// <summary>
/// Describes a bounded search across deployed contract artifacts.
/// </summary>
public sealed class DeployedContractSearchQuery
{
    /// <summary>
    /// Gets the number of results returned when a caller does not specify a limit.
    /// </summary>
    public const int DefaultLimit = 50;

    /// <summary>
    /// Gets the largest number of results a single search can return.
    /// </summary>
    public const int MaximumLimit = 100;

    /// <summary>
    /// Gets or sets the artifact type to search, or <see langword="null"/> for events and logical commands.
    /// </summary>
    public ContractArtifactType? ArtifactType { get; init; }

    /// <summary>
    /// Gets or sets the optional text matched against a contract key, version, artifact identifier, or hash.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Gets or sets the requested number of results.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Gets the requested limit constrained to the supported range.
    /// </summary>
    public int EffectiveLimit => Limit switch
    {
        null => DefaultLimit,
        <= 0 => 1,
        > MaximumLimit => MaximumLimit,
        _ => Limit.Value
    };

    /// <summary>
    /// Gets the trimmed search text, or an empty string when no text was supplied.
    /// </summary>
    public string NormalizedText => Text?.Trim() ?? string.Empty;
}
