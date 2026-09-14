namespace KnOwl.Contracts.Security;

/// <summary>
/// Formats and parses artifact delivery scopes used in token requests.
/// </summary>
public interface IConnectionScopeFormatter
{
    /// <summary>
    /// Formats one scope for the wire contract.
    /// </summary>
    string Format(ArtifactDeliveryScope scope);

    /// <summary>
    /// Formats multiple scopes as a space-separated value.
    /// </summary>
    string FormatMany(IEnumerable<ArtifactDeliveryScope> scopes);

    /// <summary>
    /// Parses a space-separated wire scope value.
    /// </summary>
    IReadOnlyCollection<ArtifactDeliveryScope> ParseMany(string value);
}
