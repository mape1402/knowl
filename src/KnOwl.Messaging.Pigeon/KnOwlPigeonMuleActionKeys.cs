using Mule;

namespace KnOwl.Messaging.Pigeon;

/// <summary>
/// Defines Mule action keys used by KnOwl Pigeon continuations.
/// </summary>
public static class KnOwlPigeonMuleActionKeys
{
    /// <summary>
    /// Gets the textual action key for deferred Pigeon consumers.
    /// </summary>
    public const string Consume = "knowl.pigeon.consume.v1";

    /// <summary>
    /// Gets the Mule action key for deferred Pigeon consumers.
    /// </summary>
    public static readonly ActionKey ConsumeKey = ActionKey.From(Consume);
}
