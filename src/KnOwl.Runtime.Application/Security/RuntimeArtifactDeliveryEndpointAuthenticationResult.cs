namespace KnOwl.Runtime.Application.Security;

/// <summary>
/// Represents the result of authenticating a Control Plane request against Runtime.
/// </summary>
public sealed class RuntimeArtifactDeliveryEndpointAuthenticationResult
{
    private RuntimeArtifactDeliveryEndpointAuthenticationResult(bool succeeded, string sourceKey, string message)
    {
        Succeeded = succeeded;
        SourceKey = sourceKey;
        Message = message;
    }

    /// <summary>
    /// Gets a value indicating whether authentication succeeded.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the authenticated Control Plane source key.
    /// </summary>
    public string SourceKey { get; }

    /// <summary>
    /// Gets the authentication message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Creates a successful authentication result.
    /// </summary>
    public static RuntimeArtifactDeliveryEndpointAuthenticationResult Success(string sourceKey) => new(true, sourceKey, string.Empty);

    /// <summary>
    /// Creates a failed authentication result.
    /// </summary>
    public static RuntimeArtifactDeliveryEndpointAuthenticationResult Failure(string message) => new(false, string.Empty, message);
}
