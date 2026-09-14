using KnOwl.Runtime.Application.ArtifactDelivery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnOwl.Runtime.Bootstrap.Runtime;

/// <summary>
/// Background worker that pulls available artifacts from Control Plane sources.
/// </summary>
internal sealed class KnOwlRuntimeArtifactPullWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<KnOwlRuntimeArtifactPullOptions> options,
    ILogger<KnOwlRuntimeArtifactPullWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("KnOwl Runtime artifact pull worker is disabled.");
            return;
        }

        if (settings.InitialDelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(settings.InitialDelaySeconds), stoppingToken);
        }

        using PeriodicTimer timer = new(TimeSpan.FromSeconds(Math.Max(5, settings.IntervalSeconds)));
        do
        {
            await ExecuteCycle(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExecuteCycle(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<IControlPlaneArtifactPullOrchestrator>();
            var result = await orchestrator.PullAvailable(cancellationToken);

            if (result.PackagesFound > 0 || result.Failed > 0)
            {
                logger.LogInformation(
                    "KnOwl Runtime pull cycle completed. Sources: {Sources}. Packages: {Packages}. Applied: {Applied}. Failed: {Failed}.",
                    result.SourcesScanned,
                    result.PackagesFound,
                    result.Applied,
                    result.Failed);
            }

            foreach (var error in result.Errors)
            {
                logger.LogWarning("KnOwl Runtime pull error: {Error}", error);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "KnOwl Runtime pull cycle failed.");
        }
    }
}
