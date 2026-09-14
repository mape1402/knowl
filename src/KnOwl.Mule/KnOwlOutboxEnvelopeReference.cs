namespace KnOwl.Mule;

/// <summary>
/// Durable Mule payload that references a persisted KnOwl outbox envelope.
/// </summary>
public sealed class KnOwlOutboxEnvelopeReference
{
    /// <summary>
    /// Gets or sets the ULID outbox envelope id stored as text.
    /// </summary>
    public string EnvelopeId { get; set; }
}
