using Mule;

namespace KnOwl.Mule;

/// <summary>
/// Provides the Mule action payload together with its continued KnOwl inbox context.
/// </summary>
/// <typeparam name="TPayload">The Mule payload type.</typeparam>
public sealed class KnOwlMuleActionContext<TPayload>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KnOwlMuleActionContext{TPayload}"/> class.
    /// </summary>
    /// <param name="inbox">The continued inbox context.</param>
    /// <param name="mule">The Mule action context.</param>
    public KnOwlMuleActionContext(InboxContext inbox, MuleActionContext<TPayload> mule)
    {
        Inbox = inbox ?? throw new ArgumentNullException(nameof(inbox));
        Mule = mule ?? throw new ArgumentNullException(nameof(mule));
    }

    /// <summary>
    /// Gets the continued KnOwl inbox context.
    /// </summary>
    public InboxContext Inbox { get; }

    /// <summary>
    /// Gets the original Mule action context.
    /// </summary>
    public MuleActionContext<TPayload> Mule { get; }

    /// <summary>
    /// Gets the Mule action payload.
    /// </summary>
    public TPayload Payload => Mule.Payload;
}
