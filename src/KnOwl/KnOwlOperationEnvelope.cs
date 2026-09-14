namespace KnOwl;

/// <summary>
/// Durable payload used to resume a declared KnOwl operation.
/// </summary>
public sealed class KnOwlOperationEnvelope
{
    /// <summary>
    /// Gets or sets the assembly-qualified operation type name.
    /// </summary>
    public string OperationType { get; set; }

    /// <summary>
    /// Gets or sets the assembly-qualified request type name.
    /// </summary>
    public string RequestType { get; set; }

    /// <summary>
    /// Gets or sets the assembly-qualified result type name.
    /// </summary>
    public string ResultType { get; set; }

    /// <summary>
    /// Gets or sets the serialized request payload.
    /// </summary>
    public string RequestJson { get; set; }
}
