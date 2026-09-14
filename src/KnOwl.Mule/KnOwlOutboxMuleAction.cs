using Mule;

namespace KnOwl.Mule;

/// <summary>
/// Mule action that publishes a persisted KnOwl outbox envelope.
/// </summary>
[MuleAction(KnOwlOutboxMuleActionKeys.Publish)]
public sealed class KnOwlOutboxMuleAction : IMuleAction<KnOwlOutboxEnvelopeReference>
{
    private readonly IOutboxService _outbox;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnOwlOutboxMuleAction"/> class.
    /// </summary>
    /// <param name="outbox">The outbox service.</param>
    public KnOwlOutboxMuleAction(IOutboxService outbox)
    {
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    /// <inheritdoc />
    public async ValueTask ExecuteAsync(
        MuleActionContext<KnOwlOutboxEnvelopeReference> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var envelopeId = ResolveEnvelopeId(context);
        await _outbox.PublishAsync(envelopeId, cancellationToken);
    }

    private static Ulid ResolveEnvelopeId(MuleActionContext<KnOwlOutboxEnvelopeReference> context)
    {
        var rawEnvelopeId = context.Payload?.EnvelopeId;
        if (string.IsNullOrWhiteSpace(rawEnvelopeId) &&
            context.Metadata.TryGetValue(KnOwlMuleMetadata.OutboxEnvelopeId, out var metadataEnvelopeId))
        {
            rawEnvelopeId = metadataEnvelopeId;
        }

        if (string.IsNullOrWhiteSpace(rawEnvelopeId))
            throw new InvalidOperationException("KnOwl outbox Mule action is missing the envelope id.");

        try
        {
            return Ulid.Parse(rawEnvelopeId);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                $"KnOwl outbox envelope id '{rawEnvelopeId}' is not a valid ULID.",
                exception);
        }
    }
}
