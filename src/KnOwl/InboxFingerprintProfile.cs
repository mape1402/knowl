namespace KnOwl;

/// <summary>
/// Configures semantic payload fingerprints discovered by KnOwl.
/// </summary>
public abstract class InboxFingerprintProfile
{
    /// <summary>
    /// Configures fingerprint rules for one or more payload types.
    /// </summary>
    /// <param name="builder">The profile builder.</param>
    public abstract void Configure(InboxFingerprintProfileBuilder builder);
}
