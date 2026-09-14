namespace KnOwl.ControlPlane.Application.Distribution.Security;

/// <summary>
/// Represents the result of authenticating a Runtime request against the Control Plane.
/// </summary>
public sealed class ArtifactDeliveryEndpointAuthenticationResult
{
    private ArtifactDeliveryEndpointAuthenticationResult(bool succeeded, string message)
    {
        Succeeded = succeeded;
        Message = message;
    }

    /// <summary>
    /// Gets a value indicating whether authentication succeeded.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the authentication message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Creates a successful authentication result.
    /// </summary>
    public static ArtifactDeliveryEndpointAuthenticationResult Success() => new(true, string.Empty);

    /// <summary>
    /// Creates a failed authentication result.
    /// </summary>
    public static ArtifactDeliveryEndpointAuthenticationResult Failure(string message) => new(false, message);
}
