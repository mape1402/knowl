namespace KnOwl;

/// <summary>
/// Describes how KnOwl obtained an idempotency key.
/// </summary>
public enum InboxIdempotencyKeySource
{
    /// <summary>
    /// The caller supplied the key explicitly.
    /// </summary>
    Explicit,

    /// <summary>
    /// KnOwl computed the key from the semantic payload hash.
    /// </summary>
    ComputedFromPayload
}
