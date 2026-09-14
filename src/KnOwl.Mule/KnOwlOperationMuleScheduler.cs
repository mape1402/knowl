namespace KnOwl.Mule;

/// <summary>
/// Schedules declared KnOwl operations through Mule durable actions.
/// </summary>
public sealed class KnOwlOperationMuleScheduler : IKnOwlOperationDeferredScheduler
{
    private readonly IInboxMuleScheduler _scheduler;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnOwlOperationMuleScheduler"/> class.
    /// </summary>
    /// <param name="scheduler">The current inbox Mule scheduler.</param>
    public KnOwlOperationMuleScheduler(IInboxMuleScheduler scheduler)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
    }

    /// <inheritdoc />
    public async ValueTask ScheduleAsync(
        InboxContext context,
        KnOwlOperationEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(envelope);

        await _scheduler.EnqueueCurrentAsync(
            KnOwlOperationMuleActionKeys.OperationKey,
            envelope,
            cancellationToken: cancellationToken);
    }
}
