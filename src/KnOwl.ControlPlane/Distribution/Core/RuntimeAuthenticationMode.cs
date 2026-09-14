namespace KnOwl.ControlPlane.Distribution.Core;

/// <summary>
/// Represents authentication modes supported by runtime node endpoints.
/// </summary>
public enum RuntimeAuthenticationMode
{
    /// <summary>
    /// Runtime node endpoint does not require authentication.
    /// </summary>
    None = 1,

    /// <summary>
    /// Runtime node endpoint authenticates through client credentials.
    /// </summary>
    ClientCredentials = 2,

    /// <summary>
    /// Runtime node endpoint authenticates through an API key.
    /// </summary>
    ApiKey = 3
}
