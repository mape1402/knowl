using Mule;

namespace KnOwl.Mule;

/// <summary>
/// Defines Mule action keys used by KnOwl outbox publication.
/// </summary>
public static class KnOwlOutboxMuleActionKeys
{
    /// <summary>
    /// Gets the textual action key for outbox publication.
    /// </summary>
    public const string Publish = "knowl.outbox.publish.v1";

    /// <summary>
    /// Gets the Mule action key for outbox publication.
    /// </summary>
    public static readonly ActionKey PublishKey = ActionKey.From(Publish);
}
