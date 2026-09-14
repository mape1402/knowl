namespace KnOwl;

/// <summary>
/// Publishes KnOwl observability events.
/// </summary>
public interface IKnOwlEventPublisher
{
    /// <summary>
    /// Publishes a live event to registered sinks.
    /// </summary>
    /// <param name="event">The event to publish.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when listeners have been notified.</returns>
    ValueTask PublishAsync(KnOwlEvent @event, CancellationToken cancellationToken = default);
}
