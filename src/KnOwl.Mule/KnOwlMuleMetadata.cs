namespace KnOwl.Mule;

/// <summary>
/// Defines metadata keys used by KnOwl when scheduling Mule durable actions.
/// </summary>
public static class KnOwlMuleMetadata
{
    /// <summary>
    /// Gets the metadata key that stores the KnOwl inbox entry ULID.
    /// </summary>
    public const string InboxEntryId = "knowl-inbox-id";

    /// <summary>
    /// Gets the metadata key that stores the effective idempotency key.
    /// </summary>
    public const string IdempotencyKey = "idempotency-key";

    /// <summary>
    /// Gets the metadata key that stores the KnOwl outbox envelope ULID.
    /// </summary>
    public const string OutboxEnvelopeId = "knowl-outbox-id";
}
