using Mule;

namespace KnOwl.Mule;

/// <summary>
/// Defines Mule action keys used by KnOwl operation continuations.
/// </summary>
public static class KnOwlOperationMuleActionKeys
{
    /// <summary>
    /// Gets the textual action key for deferred KnOwl operations.
    /// </summary>
    public const string Operation = "knowl.operation.v1";

    /// <summary>
    /// Gets the Mule action key for deferred KnOwl operations.
    /// </summary>
    public static readonly ActionKey OperationKey = ActionKey.From(Operation);
}
