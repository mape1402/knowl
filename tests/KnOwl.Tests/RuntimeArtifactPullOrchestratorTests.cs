using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.Contracts.Security;
using KnOwl.Runtime.Distribution;
using KnOwl.Runtime.Application;
using KnOwl.Runtime.Application.ArtifactDelivery;
using KnOwl.Runtime.Application.Deployments;
using KnOwl.Runtime.Application.Security;
using KnOwl.Runtime.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnOwl.Tests;

public sealed class RuntimeArtifactPullOrchestratorTests
{
    [Fact]
    public async Task PullAvailableAppliesEveryPendingPackage()
    {
        FakePullService pull = new();
        pull.Sources.Add(new ControlPlaneDistributionSource { Key = "control" });
        pull.Pending["control"] =
        [
            CreatePackage("customer.created"),
            CreatePackage("customer.updated")
        ];

        using var provider = CreateProvider(pull);
        var orchestrator = provider.GetRequiredService<IControlPlaneArtifactPullOrchestrator>();

        var result = await orchestrator.PullAvailable();

        Assert.Equal(1, result.SourcesScanned);
        Assert.Equal(2, result.PackagesFound);
        Assert.Equal(2, result.Applied);
        Assert.Equal(0, result.Failed);
        Assert.Equal(2, pull.AppliedPackages.Count);
    }

    [Fact]
    public async Task PullAvailableCapturesRejectedPackages()
    {
        var rejected = CreatePackage("customer.rejected");
        FakePullService pull = new();
        pull.Sources.Add(new ControlPlaneDistributionSource { Key = "control" });
        pull.Pending["control"] = [rejected];
        pull.RejectedReleaseTargetIds.Add(rejected.ReleaseTargetId);

        using var provider = CreateProvider(pull);
        var orchestrator = provider.GetRequiredService<IControlPlaneArtifactPullOrchestrator>();

        var result = await orchestrator.PullAvailable();

        Assert.Equal(1, result.PackagesFound);
        Assert.Equal(0, result.Applied);
        Assert.Equal(1, result.Failed);
        Assert.Contains("Rejected by fake runtime", result.Errors.Single());
    }

    [Fact]
    public async Task GetSourcesReturnsOnlyPullReadyControlPlanes()
    {
        var readyPull = CreateDesignNode("ready-pull", DistributionMode.Pull, ConnectionCredentialStatus.Active);
        var readyHybrid = CreateDesignNode("ready-hybrid", DistributionMode.Hybrid, ConnectionCredentialStatus.Active);
        var pushOnly = CreateDesignNode("push-only", DistributionMode.Push, ConnectionCredentialStatus.Active);
        var missingCredential = CreateDesignNode("missing-credential", DistributionMode.Pull, ConnectionCredentialStatus.Missing);
        var disabled = CreateDesignNode("disabled", DistributionMode.Pull, ConnectionCredentialStatus.Active);
        disabled.IsEnabled = false;

        var service = new ControlPlaneArtifactPullService(
            new HttpClient(),
            new DesignNodeRepository([readyPull, readyHybrid, pushOnly, missingCredential, disabled]),
            new NoopControlPlaneAccessTokenProvider(),
            new NoopDeploymentService());

        var sources = await service.GetSources();

        Assert.Collection(
            sources.OrderBy(x => x.Key),
            source => Assert.Equal("ready-hybrid", source.Key),
            source => Assert.Equal("ready-pull", source.Key));
    }

    private static ServiceProvider CreateProvider(IControlPlaneArtifactPullService pull)
    {
        var services = new ServiceCollection();
        services.AddKnOwlRuntimeApplication();
        services.RemoveAll<IControlPlaneArtifactPullService>();
        services.AddSingleton(pull);
        return services.BuildServiceProvider();
    }

    private static RuntimeArtifactDeliveryPackage CreatePackage(string topic)
        => new()
        {
            ReleaseTargetId = Guid.NewGuid(),
            ReleaseId = Guid.NewGuid(),
            RuntimeNodeId = Guid.NewGuid(),
            ArtifactId = Guid.NewGuid(),
            ArtifactType = ContractArtifactType.Event,
            Topic = topic,
            VersionNumber = "1.0.0",
            Name = topic,
            PayloadSchemaJson = "{}",
            ContentHash = Guid.NewGuid().ToString("N")
        };

    private static RuntimeDesignNode CreateDesignNode(
        string key,
        DistributionMode mode,
        ConnectionCredentialStatus credentialStatus)
        => new()
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = key,
            EndpointBaseUri = "http://control-plane",
            RemoteRuntimeNodeId = Guid.NewGuid().ToString("N"),
            DistributionMode = mode,
            Status = RuntimeDesignNodeStatus.Enabled,
            IsEnabled = true,
            OutboundCredentialStatus = credentialStatus
        };

    private sealed class FakePullService : IControlPlaneArtifactPullService
    {
        public List<ControlPlaneDistributionSource> Sources { get; } = [];
        public Dictionary<string, IReadOnlyCollection<RuntimeArtifactDeliveryPackage>> Pending { get; } = [];
        public List<RuntimeArtifactDeliveryPackage> AppliedPackages { get; } = [];
        public HashSet<Guid> RejectedReleaseTargetIds { get; } = [];

        public Task<IReadOnlyCollection<ControlPlaneDistributionSource>> GetSources(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ControlPlaneDistributionSource>>(Sources);

        public Task<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>> GetPending(string sourceKey, CancellationToken cancellationToken = default)
            => Task.FromResult(Pending.GetValueOrDefault(sourceKey) ?? []);

        public Task<RuntimeArtifactDeploymentResult> Apply(string sourceKey, Guid releaseTargetId, CancellationToken cancellationToken = default)
        {
            var package = Pending[sourceKey].First(x => x.ReleaseTargetId == releaseTargetId);
            return ApplyPackage(sourceKey, package, cancellationToken);
        }

        public Task<RuntimeArtifactDeploymentResult> ApplyPackage(
            string sourceKey,
            RuntimeArtifactDeliveryPackage package,
            CancellationToken cancellationToken = default)
        {
            AppliedPackages.Add(package);
            var accepted = !RejectedReleaseTargetIds.Contains(package.ReleaseTargetId);
            return Task.FromResult(new RuntimeArtifactDeploymentResult
            {
                Accepted = accepted,
                RuntimeArtifactId = accepted ? package.ArtifactId.ToString("N") : string.Empty,
                Status = accepted ? "Ready" : "Rejected",
                Message = accepted ? "Stored." : "Rejected by fake runtime."
            });
        }
    }

    private sealed class DesignNodeRepository(IReadOnlyList<RuntimeDesignNode> nodes) : IRuntimeDesignNodeRepository
    {
        public Task<IReadOnlyList<RuntimeDesignNode>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult(nodes);

        public Task<RuntimeDesignNode?> GetById(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(nodes.FirstOrDefault(x => x.Id == id));

        public Task<RuntimeDesignNode?> GetByKey(string key, CancellationToken cancellationToken = default)
            => Task.FromResult(nodes.FirstOrDefault(x => x.Key == key));

        public Task<RuntimeDesignNode?> GetByInboundClientId(string clientId, CancellationToken cancellationToken = default)
            => Task.FromResult(nodes.FirstOrDefault(x => x.InboundClientId == clientId));

        public Task Upsert(RuntimeDesignNode designNode, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoopControlPlaneAccessTokenProvider : IControlPlaneAccessTokenProvider
    {
        public Task AttachToken(
            HttpRequestMessage request,
            ControlPlaneDistributionSource source,
            IReadOnlyCollection<ArtifactDeliveryScope> scopes,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoopDeploymentService : IRuntimeContractDeploymentService
    {
        public Task<RuntimeArtifactDeploymentResult> DeployArtifact(
            RuntimeArtifactDeliveryPackage package,
            string sourceKey,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new RuntimeArtifactDeploymentResult { Accepted = true, Status = "Ready" });
    }
}

