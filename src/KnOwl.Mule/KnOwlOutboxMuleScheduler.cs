using Mule;

namespace KnOwl.Mule;

/// <summary>
/// Schedules persisted outbox envelopes through Mule durable actions.
/// </summary>
public sealed class KnOwlOutboxMuleScheduler : IOutboxDeferredScheduler
{
    private readonly IMuleClient _mule;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnOwlOutboxMuleScheduler"/> class.
    /// </summary>
    /// <param name="mule">The Mule client.</param>
    public KnOwlOutboxMuleScheduler(IMuleClient mule)
    {
        _mule = mule ?? throw new ArgumentNullException(nameof(mule));
    }

    /// <inheritdoc />
    public async ValueTask ScheduleAsync(
        OutboxEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        await _mule.EnqueueAsync(
            KnOwlOutboxMuleActionKeys.PublishKey,
            new KnOwlOutboxEnvelopeReference { EnvelopeId = envelope.Id.ToString() },
            options =>
            {
                options.CorrelationId ??= envelope.CorrelationId;
                options.DeduplicationKey ??= envelope.Id.ToString();
                options.Metadata[KnOwlMuleMetadata.OutboxEnvelopeId] = envelope.Id.ToString();
                options.Metadata["transport"] = envelope.Transport;
                options.Metadata["operation"] = envelope.Operation;
                options.Metadata["destination"] = envelope.Destination;
            },
            cancellationToken);
    }
}
