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
using System.Net;
using System.Net.Http.Json;

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
    public async Task PullAvailableCapturesSourceAndPackageExceptions()
    {
        var failedPackage = CreatePackage("customer.failed");
        FakePullService pull = new();
        pull.Sources.Add(new ControlPlaneDistributionSource { Key = "source-failure" });
        pull.Sources.Add(new ControlPlaneDistributionSource { Key = "package-failure" });
        pull.Pending["package-failure"] = [failedPackage];
        pull.GetPendingFailures["source-failure"] = new InvalidOperationException("source unavailable");
        pull.ApplyFailures[failedPackage.ReleaseTargetId] = new HttpRequestException("apply unavailable");

        using var provider = CreateProvider(pull);
        var orchestrator = provider.GetRequiredService<IControlPlaneArtifactPullOrchestrator>();

        var result = await orchestrator.PullAvailable();

        Assert.Equal(2, result.SourcesScanned);
        Assert.Equal(1, result.PackagesFound);
        Assert.Equal(0, result.Applied);
        Assert.Equal(2, result.Failed);
        Assert.Contains(result.Errors, x => x.Contains("source unavailable", StringComparison.Ordinal));
        Assert.Contains(result.Errors, x => x.Contains("apply unavailable", StringComparison.Ordinal));
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

    [Fact]
    public async Task PullServiceGetsPendingAppliesPackageAndAcknowledgesAcceptedDeployment()
    {
        var node = CreateDesignNode("control", DistributionMode.Hybrid, ConnectionCredentialStatus.Active);
        node.EndpointBaseUri = "https://control.example.test/";
        node.RemoteRuntimeNodeId = "runtime-123";
        var package = CreatePackage("customer.created");
        var handler = new CapturingHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                Assert.EndsWith("/distribution/artifacts/runtime-nodes/runtime-123/pending", request.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>>([package])
                };
            }

            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith($"/distribution/artifacts/runtime-nodes/runtime-123/targets/{package.ReleaseTargetId}/ack", request.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var tokenProvider = new RecordingControlPlaneAccessTokenProvider();
        var deploymentService = new RecordingDeploymentService
        {
            Result = new RuntimeArtifactDeploymentResult
            {
                Accepted = true,
                RuntimeArtifactId = "runtime-artifact",
                Status = "Ready",
                Message = "Stored."
            }
        };
        var service = new ControlPlaneArtifactPullService(
            new HttpClient(handler),
            new DesignNodeRepository([node]),
            tokenProvider,
            deploymentService);

        var pending = await service.GetPending("control");
        var applied = await service.Apply("control", package.ReleaseTargetId);
        var direct = await service.ApplyPackage("control", package);

        Assert.Single(pending);
        Assert.True(applied.Accepted);
        Assert.True(direct.Accepted);
        Assert.Equal(4, handler.Requests.Count);
        Assert.Collection(
            tokenProvider.Scopes,
            scopes => Assert.Contains(ArtifactDeliveryScope.ReleaseRead, scopes),
            scopes => Assert.Contains(ArtifactDeliveryScope.ReleaseRead, scopes),
            scopes => Assert.Contains(ArtifactDeliveryScope.ArtifactAcknowledge, scopes),
            scopes => Assert.Contains(ArtifactDeliveryScope.ArtifactAcknowledge, scopes));
        Assert.Equal("control", deploymentService.LastSourceKey);
    }

    [Fact]
    public async Task PullServiceCoversRejectedDeploymentMissingPendingAndSourcePreconditions()
    {
        var source = CreateDesignNode("control", DistributionMode.Pull, ConnectionCredentialStatus.Active);
        var missingTarget = Guid.NewGuid();
        var service = new ControlPlaneArtifactPullService(
            new HttpClient(new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>>([])
            })),
            new DesignNodeRepository([source]),
            new NoopControlPlaneAccessTokenProvider(),
            new RecordingDeploymentService
            {
                Result = new RuntimeArtifactDeploymentResult
                {
                    Accepted = false,
                    Status = "Rejected",
                    Message = "Runtime rejected."
                }
            });

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetPending("missing"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.Apply("control", missingTarget));

        var rejected = await service.ApplyPackage("control", CreatePackage("customer.rejected"));
        Assert.False(rejected.Accepted);

        foreach (var node in new[]
        {
            CreateInvalidDesignNode("disabled", n => n.IsEnabled = false),
            CreateInvalidDesignNode("suspended", n => n.Status = RuntimeDesignNodeStatus.Suspended),
            CreateInvalidDesignNode("push", n => n.DistributionMode = DistributionMode.Push),
            CreateInvalidDesignNode("credential", n => n.OutboundCredentialStatus = ConnectionCredentialStatus.Missing),
            CreateInvalidDesignNode("remote", n => n.RemoteRuntimeNodeId = string.Empty)
        })
        {
            var invalid = new ControlPlaneArtifactPullService(
                new HttpClient(),
                new DesignNodeRepository([node]),
                new NoopControlPlaneAccessTokenProvider(),
                new NoopDeploymentService());
            await Assert.ThrowsAsync<InvalidOperationException>(() => invalid.GetPending(node.Key));
        }

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ApplyPackage("control", null!));
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

    private static RuntimeDesignNode CreateInvalidDesignNode(string key, Action<RuntimeDesignNode> mutate)
    {
        var node = CreateDesignNode(key, DistributionMode.Pull, ConnectionCredentialStatus.Active);
        mutate(node);
        return node;
    }

    private sealed class FakePullService : IControlPlaneArtifactPullService
    {
        public List<ControlPlaneDistributionSource> Sources { get; } = [];
        public Dictionary<string, IReadOnlyCollection<RuntimeArtifactDeliveryPackage>> Pending { get; } = [];
        public List<RuntimeArtifactDeliveryPackage> AppliedPackages { get; } = [];
        public HashSet<Guid> RejectedReleaseTargetIds { get; } = [];
        public Dictionary<string, Exception> GetPendingFailures { get; } = [];
        public Dictionary<Guid, Exception> ApplyFailures { get; } = [];

        public Task<IReadOnlyCollection<ControlPlaneDistributionSource>> GetSources(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ControlPlaneDistributionSource>>(Sources);

        public Task<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>> GetPending(string sourceKey, CancellationToken cancellationToken = default)
        {
            if (GetPendingFailures.TryGetValue(sourceKey, out var exception))
            {
                throw exception;
            }

            return Task.FromResult(Pending.GetValueOrDefault(sourceKey) ?? []);
        }

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
            if (ApplyFailures.TryGetValue(package.ReleaseTargetId, out var exception))
            {
                throw exception;
            }

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

    private sealed class RecordingControlPlaneAccessTokenProvider : IControlPlaneAccessTokenProvider
    {
        public List<IReadOnlyCollection<ArtifactDeliveryScope>> Scopes { get; } = [];

        public Task AttachToken(
            HttpRequestMessage request,
            ControlPlaneDistributionSource source,
            IReadOnlyCollection<ArtifactDeliveryScope> scopes,
            CancellationToken cancellationToken = default)
        {
            Scopes.Add(scopes);
            request.Headers.Authorization = new("Bearer", "token");
            return Task.CompletedTask;
        }
    }

    private sealed class NoopDeploymentService : IRuntimeContractDeploymentService
    {
        public Task<RuntimeArtifactDeploymentResult> DeployArtifact(
            RuntimeArtifactDeliveryPackage package,
            string sourceKey,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new RuntimeArtifactDeploymentResult { Accepted = true, Status = "Ready" });
    }

    private sealed class RecordingDeploymentService : IRuntimeContractDeploymentService
    {
        public RuntimeArtifactDeploymentResult Result { get; init; } = new();
        public string? LastSourceKey { get; private set; }

        public Task<RuntimeArtifactDeploymentResult> DeployArtifact(
            RuntimeArtifactDeliveryPackage package,
            string sourceKey,
            CancellationToken cancellationToken = default)
        {
            LastSourceKey = sourceKey;
            return Task.FromResult(Result);
        }
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }
}

