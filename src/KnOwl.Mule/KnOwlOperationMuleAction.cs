using System.Text.Json;
using Mule;

namespace KnOwl.Mule;

/// <summary>
/// Mule action that resumes a deferred declared KnOwl operation.
/// </summary>
[MuleAction(KnOwlOperationMuleActionKeys.Operation)]
public sealed class KnOwlOperationMuleAction : KnOwlMuleAction<KnOwlOperationEnvelope>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IKnOwlOperationService _operations;
    private object _result;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnOwlOperationMuleAction"/> class.
    /// </summary>
    /// <param name="inbox">The inbox service.</param>
    /// <param name="operations">The operation service.</param>
    public KnOwlOperationMuleAction(IInboxService inbox, IKnOwlOperationService operations)
        : base(inbox)
    {
        _operations = operations ?? throw new ArgumentNullException(nameof(operations));
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteInboxAsync(
        KnOwlMuleActionContext<KnOwlOperationEnvelope> context,
        CancellationToken cancellationToken)
    {
        _result = await _operations.ContinueAsync(context.Payload, cancellationToken);
    }

    /// <inheritdoc />
    protected override ValueTask<InboxCompletion> CreateCompletionAsync(
        KnOwlMuleActionContext<KnOwlOperationEnvelope> context,
        CancellationToken cancellationToken)
    {
        if (_result is null)
            return ValueTask.FromResult(InboxCompletion.Empty);

        var completion = new InboxCompletion
        {
            ContentType = "application/json",
            ResultPayload = JsonSerializer.SerializeToUtf8Bytes(_result, _result.GetType(), JsonOptions),
            ResultType = _result.GetType().AssemblyQualifiedName
        };

        completion.Metadata["result-type"] = _result.GetType().AssemblyQualifiedName;
        return ValueTask.FromResult(completion);
    }
}
