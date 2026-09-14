namespace KnOwl;

/// <summary>
/// Subscribes to live KnOwl observability events.
/// </summary>
public interface IKnOwlEventSubscriber
{
    /// <summary>
    /// Subscribes a handler to future events.
    /// </summary>
    /// <param name="handler">The event handler.</param>
    /// <returns>A disposable subscription.</returns>
    IDisposable Subscribe(Func<KnOwlEvent, CancellationToken, ValueTask> handler);
}
