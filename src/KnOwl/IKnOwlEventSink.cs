namespace KnOwl;

/// <summary>
/// Combines publishing, subscription, and recent event buffering for KnOwl events.
/// </summary>
public interface IKnOwlEventSink : IKnOwlEventPublisher, IKnOwlEventSubscriber
{
    /// <summary>
    /// Gets a snapshot of recently published events.
    /// </summary>
    /// <param name="limit">The maximum number of events to return.</param>
    /// <returns>The recent event snapshot.</returns>
    IReadOnlyList<KnOwlEvent> GetRecent(int limit = 100);
}
