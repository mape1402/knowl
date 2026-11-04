namespace KnOwl.ControlPlane.WebUI.ButterMorph;

/// <summary>
/// Configures retention for transient results exchanged between KnOwl forms and ButterMorph editors.
/// </summary>
public sealed class KnOwlButterMorphDraftStoreOptions
{
    /// <summary>
    /// Gets or sets how long an unconsumed transient result is kept.
    /// </summary>
    public TimeSpan Retention { get; set; } = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Gets or sets how often expired transient results are removed.
    /// </summary>
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets the maximum number of retained contexts.
    /// </summary>
    public int MaximumEntries { get; set; } = 256;
}
