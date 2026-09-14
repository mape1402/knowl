namespace KnOwl.Runtime.Application.ArtifactDelivery;

/// <inheritdoc />
internal sealed class ControlPlaneArtifactPullOrchestrator(
    IControlPlaneArtifactPullService pullService) : IControlPlaneArtifactPullOrchestrator
{
    /// <inheritdoc />
    public async Task<ControlPlaneArtifactPullExecutionResult> PullAvailable(CancellationToken cancellationToken = default)
    {
        var sources = await pullService.GetSources(cancellationToken);
        var result = new ControlPlaneArtifactPullExecutionResult
        {
            SourcesScanned = sources.Count
        };
        List<string> errors = [];

        foreach (var source in sources)
        {
            try
            {
                var packages = await pullService.GetPending(source.Key, cancellationToken);
                result.PackagesFound += packages.Count;

                foreach (var package in packages)
                {
                    try
                    {
                        var deployment = await pullService.ApplyPackage(source.Key, package, cancellationToken);
                        if (deployment.Accepted)
                        {
                            result.Applied++;
                        }
                        else
                        {
                            result.Failed++;
                            errors.Add($"{source.Key}:{package.ReleaseTargetId:N}: {deployment.Message}");
                        }
                    }
                    catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or KeyNotFoundException)
                    {
                        result.Failed++;
                        errors.Add($"{source.Key}:{package.ReleaseTargetId:N}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or KeyNotFoundException)
            {
                result.Failed++;
                errors.Add($"{source.Key}: {ex.Message}");
            }
        }

        result.Errors = errors;
        return result;
    }
}
