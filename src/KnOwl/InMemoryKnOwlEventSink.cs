using System.Collections.Concurrent;

namespace KnOwl;

/// <summary>
/// In-memory event sink used by dashboards and tests to receive live KnOwl events.
/// </summary>
public sealed class InMemoryKnOwlEventSink : IKnOwlEventSink
{
    private const int MaxBufferedEvents = 500;
    private readonly ConcurrentQueue<KnOwlEvent> _recent = new();
    private readonly ConcurrentDictionary<Guid, Func<KnOwlEvent, CancellationToken, ValueTask>> _subscribers = new();

    /// <inheritdoc />
    public async ValueTask PublishAsync(KnOwlEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _recent.Enqueue(@event);
        while (_recent.Count > MaxBufferedEvents && _recent.TryDequeue(out _))
        {
        }

        foreach (var subscriber in _subscribers.Values)
        {
            try
            {
                await subscriber(@event, cancellationToken);
            }
            catch
            {
                // Observability subscribers must never affect inbox, outbox, or deferred execution.
            }
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe(Func<KnOwlEvent, CancellationToken, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _subscribers[id] = handler;
        return new Subscription(_subscribers, id);
    }

    /// <inheritdoc />
    public IReadOnlyList<KnOwlEvent> GetRecent(int limit = 100)
        => _recent.Reverse().Take(Math.Max(1, limit)).Reverse().ToArray();

    private sealed class Subscription : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, Func<KnOwlEvent, CancellationToken, ValueTask>> _subscribers;
        private readonly Guid _id;

        public Subscription(
            ConcurrentDictionary<Guid, Func<KnOwlEvent, CancellationToken, ValueTask>> subscribers,
            Guid id)
        {
            _subscribers = subscribers;
            _id = id;
        }

        public void Dispose()
            => _subscribers.TryRemove(_id, out _);
    }
}
