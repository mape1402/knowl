using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace KnOwl.ControlPlane.WebUI.ButterMorph;

/// <summary>
/// Periodically releases expired transient ButterMorph results.
/// </summary>
public sealed class KnOwlButterMorphDraftStoreCleanupService(
    KnOwlButterMorphDraftStore drafts,
    IOptions<KnOwlButterMorphDraftStoreOptions> options) : BackgroundService
{
    private readonly TimeSpan sweepInterval = NormalizeInterval(options.Value.SweepInterval);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(sweepInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            drafts.PurgeExpired();
        }
    }

    private static TimeSpan NormalizeInterval(TimeSpan configured)
        => configured > TimeSpan.Zero ? configured : TimeSpan.FromMinutes(2);
}
