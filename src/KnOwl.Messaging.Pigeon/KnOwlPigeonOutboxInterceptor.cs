using Microsoft.Extensions.Options;
using Pigeon.Messaging.Producing;

namespace KnOwl.Messaging.Pigeon;

/// <summary>
/// Pigeon publish decision interceptor that redirects publish operations to KnOwl outbox.
/// </summary>
public sealed class KnOwlPigeonOutboxInterceptor : IPublishDecisionInterceptor
{
    private readonly IOutboxService _outbox;
    private readonly IPigeonPublishEnvelopeFactory _envelopeFactory;
    private readonly KnOwlPigeonOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnOwlPigeonOutboxInterceptor"/> class.
    /// </summary>
    public KnOwlPigeonOutboxInterceptor(
        IOutboxService outbox,
        IPigeonPublishEnvelopeFactory envelopeFactory,
        IOptions<KnOwlPigeonOptions> options)
    {
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _envelopeFactory = envelopeFactory ?? throw new ArgumentNullException(nameof(envelopeFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async ValueTask<PigeonPublishDecisionResult> InterceptAsync(
        PublishContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_options.EnableOutbox ||
            _options.OutboxPredicate is { } predicate && !predicate(context))
        {
            return PigeonPublishDecisionResult.Continue;
        }

        var publishEnvelope = await _envelopeFactory.CreateAsync(context, cancellationToken);
        var envelope = await _outbox.EnqueueAsync(CreateRequest(publishEnvelope), cancellationToken);

        return new PigeonPublishDecisionResult(
            PigeonPublishDecision.Skip,
            "KnOwl persisted the Pigeon publish operation in the outbox.")
        {
            Metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                [KnOwlPigeonMetadataNames.OutboxEnvelopeId] = envelope.Id.ToString()
            }
        };
    }

    private OutboxEnqueueRequest CreateRequest(PigeonPublishEnvelope envelope)
    {
        var request = new OutboxEnqueueRequest
        {
            Transport = _options.Transport,
            Operation = envelope.IsRaw ? "pigeon.publish.raw" : "pigeon.publish",
            Destination = ResolveDestination(envelope),
            Payload = envelope,
            PayloadType = typeof(PigeonPublishEnvelope),
            CorrelationId = envelope.CorrelationId,
            TraceId = envelope.TraceId
        };

        Copy(envelope.Headers, request.Headers);
        Copy(envelope.Metadata, request.Metadata);

        request.Metadata[KnOwlPigeonMetadataNames.Transport] = envelope.Transport;
        request.Metadata[KnOwlPigeonMetadataNames.Topic] = envelope.Topic;
        request.Metadata[KnOwlPigeonMetadataNames.Exchange] = envelope.Exchange;
        request.Metadata[KnOwlPigeonMetadataNames.RoutingKey] = envelope.RoutingKey;
        request.Metadata[KnOwlPigeonMetadataNames.PayloadType] = envelope.PayloadType;
        request.Metadata[KnOwlPigeonMetadataNames.ContentType] = envelope.ContentType;
        request.Metadata[KnOwlPigeonMetadataNames.IsRaw] = envelope.IsRaw.ToString();
        request.Metadata[KnOwlPigeonMetadataNames.Version] = envelope.Version;
        request.Metadata[KnOwlPigeonMetadataNames.Operation] = envelope.Operation;

        return request;
    }

    private static void Copy(IReadOnlyDictionary<string, string> source, IDictionary<string, string> target)
    {
        if (source is null)
            return;

        foreach (var item in source)
            target[item.Key] = item.Value;
    }

    private static string ResolveDestination(PigeonPublishEnvelope envelope)
    {
        if (!string.IsNullOrWhiteSpace(envelope.Destination))
            return envelope.Destination;

        if (!string.IsNullOrWhiteSpace(envelope.Exchange))
            return $"{envelope.Exchange}:{envelope.RoutingKey}";

        return envelope.Topic;
    }
}
