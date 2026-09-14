using Microsoft.Extensions.Options;

namespace KnOwl;

/// <summary>
/// Default policy resolver backed by <see cref="KnOwlOptions"/>.
/// </summary>
public sealed class DefaultInboxPolicyResolver : IInboxPolicyResolver
{
    private readonly IOptions<KnOwlOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultInboxPolicyResolver"/> class.
    /// </summary>
    /// <param name="options">The KnOwl options.</param>
    public DefaultInboxPolicyResolver(IOptions<KnOwlOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public InboxDecision Resolve(InboxOpenResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new InboxDecision(result.State, _options.Value.Policies.Resolve(result.State), result.Entry);
    }
}
