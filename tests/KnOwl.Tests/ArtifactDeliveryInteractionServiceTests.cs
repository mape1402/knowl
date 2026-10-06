using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Application.Distribution;
using KnOwl.ControlPlane.Application.Distribution.ArtifactDelivery;
using KnOwl.ControlPlane.Application.Distribution.Security;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace KnOwl.Tests;

public sealed class ArtifactDeliveryInteractionServiceTests
{
    [Fact]
    public async Task GetPendingForPullReturnsRuntimePackages()
    {
        var runtimeNodeId = Guid.NewGuid();
        var target = CreateTarget(runtimeNodeId, ContractReleaseTargetStatus.AvailableForPull);
        TargetRepository targets = new([target]);

        using var provider = CreateProvider(targets, new ReleaseRepository());
        var service = provider.GetRequiredService<IArtifactDeliveryInteractionService>();

        var packages = await service.GetPendingForPull(runtimeNodeId);

        var package = Assert.Single(packages);
        Assert.Equal(target.Id, package.ReleaseTargetId);
        Assert.Equal(target.ArtifactId, package.ArtifactId);
        Assert.Equal("customer.created", package.Topic);
    }

    [Fact]
    public async Task AcknowledgePullActivatesReleaseTarget()
    {
        var runtimeNodeId = Guid.NewGuid();
        var target = CreateTarget(runtimeNodeId, ContractReleaseTargetStatus.AvailableForPull);
        TargetRepository targets = new([target]);

        using var provider = CreateProvider(targets, new ReleaseRepository());
        var service = provider.GetRequiredService<IArtifactDeliveryInteractionService>();

        var result = await service.AcknowledgePull(runtimeNodeId, target.Id, "runtime-artifact-1");

        Assert.True(result.Succeeded);
        Assert.Equal(ContractReleaseTargetStatus.Activated, target.Status);
        Assert.Equal(ContractReleaseActivationStatus.Activated, target.ActivationStatus);
        Assert.Equal("runtime-artifact-1", target.RuntimeVersionApplied);
    }

    [Fact]
    public async Task PushForPullNodeLeavesTargetAvailableForPull()
    {
        var runtimeNodeId = Guid.NewGuid();
        var target = CreateTarget(runtimeNodeId, ContractReleaseTargetStatus.PushScheduled, DistributionMode.Pull);
        TargetRepository targets = new([target]);

        using var provider = CreateProvider(targets, new ReleaseRepository());
        var service = provider.GetRequiredService<IArtifactDeliveryInteractionService>();

        var result = await service.Push(target.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(ContractReleaseTargetStatus.AvailableForPull, target.Status);
        Assert.NotNull(target.AvailableAtUtc);
    }

    [Fact]
    public async Task PushForPullNodeRecordsPullAttempt()
    {
        var runtimeNodeId = Guid.NewGuid();
        var target = CreateTarget(runtimeNodeId, ContractReleaseTargetStatus.PushScheduled, DistributionMode.Pull);
        TargetRepository targets = new([target]);

        using var provider = CreateProvider(targets, new ReleaseRepository());
        var service = provider.GetRequiredService<IArtifactDeliveryInteractionService>();

        await service.Push(target.Id, "unit-test");

        var attempt = Assert.Single(targets.Attempts);
        Assert.Equal(target.Id, attempt.ReleaseTargetId);
        Assert.Equal("Pull", attempt.Action);
        Assert.Equal("unit-test", attempt.InitiatedBy);
        Assert.True(attempt.Succeeded);
    }

    [Fact]
    public async Task PushAlreadyActivatedTargetReturnsCurrentRuntimeArtifact()
    {
        var runtimeNodeId = Guid.NewGuid();
        var target = CreateTarget(runtimeNodeId, ContractReleaseTargetStatus.Activated, DistributionMode.Push);
        target.ActivationStatus = ContractReleaseActivationStatus.Activated;
        target.RuntimeVersionApplied = "runtime-artifact-existing";
        TargetRepository targets = new([target]);

        using var provider = CreateProvider(targets, new ReleaseRepository());
        var service = provider.GetRequiredService<IArtifactDeliveryInteractionService>();

        var result = await service.Push(target.Id);

        Assert.True(result.Succeeded);
        Assert.Equal("runtime-artifact-existing", result.RuntimeArtifactId);
        Assert.Empty(targets.Attempts);
    }

    [Fact]
    public async Task PushForPushNodePostsPackageAndActivatesReadyResponse()
    {
        var runtimeNodeId = Guid.NewGuid();
        var target = CreateTarget(runtimeNodeId, ContractReleaseTargetStatus.PushScheduled, DistributionMode.Push);
        ConfigurePushRuntime(target.RuntimeNode!, endpointPath: "");
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new StringContent("""{"accepted":true,"status":"Ready","runtimeArtifactId":"runtime-123","message":"OK"}""", Encoding.UTF8, "application/json")
        });
        var releases = new ReleaseRepository();
        TargetRepository targets = new([target]);

        using var provider = CreateProvider(targets, releases, handler);
        var service = provider.GetRequiredService<IArtifactDeliveryInteractionService>();

        var result = await service.Push(target.Id, "push-user");

        Assert.True(result.Succeeded);
        Assert.Equal(ContractReleaseTargetStatus.Activated, target.Status);
        Assert.Equal(ContractReleaseActivationStatus.Activated, target.ActivationStatus);
        Assert.Equal("runtime-123", target.RuntimeVersionApplied);
        Assert.Equal(ContractReleaseStatus.Completed, releases.Status);
        Assert.Equal("Bearer", handler.Request!.Headers.Authorization!.Scheme);
        Assert.EndsWith("/runtime/artifacts/deploy", handler.Request.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
        Assert.Contains(target.Artifact!.Topic, await handler.ReadBody());
        Assert.Contains(targets.Attempts, x => x.Action == "Push" && x.Succeeded && x.InitiatedBy == "push-user");
    }

    [Fact]
    public async Task PushForPushNodeAcceptedButNotReadyMarksDelivered()
    {
        var runtimeNodeId = Guid.NewGuid();
        var target = CreateTarget(runtimeNodeId, ContractReleaseTargetStatus.PushScheduled, DistributionMode.Push);
        ConfigurePushRuntime(target.RuntimeNode!, endpointPath: "/custom/deploy");
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new StringContent("""{"accepted":true,"status":"Processing","runtimeArtifactId":"runtime-456"}""", Encoding.UTF8, "application/json")
        });
        TargetRepository targets = new([target]);

        using var provider = CreateProvider(targets, new ReleaseRepository(), handler);
        var service = provider.GetRequiredService<IArtifactDeliveryInteractionService>();

        var result = await service.Push(target.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(ContractReleaseTargetStatus.Delivered, target.Status);
        Assert.Equal(ContractReleaseActivationStatus.Activating, target.ActivationStatus);
        Assert.Equal("runtime-456", result.RuntimeArtifactId);
        Assert.EndsWith("/custom/deploy", handler.Request!.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PushForPushNodeRecordsFailureFromRuntimeTextResponse()
    {
        var runtimeNodeId = Guid.NewGuid();
        var target = CreateTarget(runtimeNodeId, ContractReleaseTargetStatus.PushScheduled, DistributionMode.Push);
        ConfigurePushRuntime(target.RuntimeNode!, endpointPath: "/deploy");
        var longError = new string('x', 520);
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(longError + "\nsecond line", Encoding.UTF8, "text/plain")
        });
        TargetRepository targets = new([target]);

        using var provider = CreateProvider(targets, new ReleaseRepository(), handler);
        var service = provider.GetRequiredService<IArtifactDeliveryInteractionService>();

        var result = await service.Push(target.Id, " ");

        Assert.False(result.Succeeded);
        Assert.Equal(ContractReleaseTargetStatus.Failed, target.Status);
        Assert.Equal(ContractReleaseActivationStatus.ActivationFailed, target.ActivationStatus);
        Assert.Equal(500, target.FailureReason!.Length);
        var attempt = Assert.Single(targets.Attempts);
        Assert.Equal("distribution", attempt.InitiatedBy);
        Assert.False(attempt.Succeeded);
        Assert.Equal("ArtifactDeliveryFailed", attempt.ErrorCode);
    }

    [Fact]
    public async Task HybridPushFailureFallsBackToRuntimePull()
    {
        var runtimeNodeId = Guid.NewGuid();
        var target = CreateTarget(runtimeNodeId, ContractReleaseTargetStatus.PushScheduled, DistributionMode.Hybrid);
        ConfigurePushRuntime(target.RuntimeNode!, endpointPath: "/deploy");
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new StringContent("not-json", Encoding.UTF8, "text/plain")
        });
        TargetRepository targets = new([target]);

        using var provider = CreateProvider(targets, new ReleaseRepository(), handler);
        var service = provider.GetRequiredService<IArtifactDeliveryInteractionService>();

        var result = await service.Push(target.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(ContractReleaseTargetStatus.AvailableForPull, target.Status);
        Assert.NotNull(target.AvailableAtUtc);
        Assert.Collection(
            targets.Attempts,
            push => Assert.Equal("Push", push.Action),
            pull => Assert.Equal("Pull", pull.Action));
    }

    [Fact]
    public async Task GetForPullMarksHybridTargetAvailable()
    {
        var runtimeNodeId = Guid.NewGuid();
        var target = CreateTarget(runtimeNodeId, ContractReleaseTargetStatus.PushScheduled, DistributionMode.Hybrid);
        TargetRepository targets = new([target]);

        using var provider = CreateProvider(targets, new ReleaseRepository());
        var service = provider.GetRequiredService<IArtifactDeliveryInteractionService>();

        var package = await service.GetForPull(runtimeNodeId, target.Id);

        Assert.Equal(target.Id, package.ReleaseTargetId);
        Assert.Equal(ContractReleaseTargetStatus.AvailableForPull, target.Status);
        Assert.Contains(targets.Attempts, x => x.Action == "Pull" && x.Succeeded);
    }

    [Fact]
    public async Task GetForPullBlocksPushOnlyRuntimeNodes()
    {
        var runtimeNodeId = Guid.NewGuid();
        var target = CreateTarget(runtimeNodeId, ContractReleaseTargetStatus.PushScheduled, DistributionMode.Push);
        TargetRepository targets = new([target]);

        using var provider = CreateProvider(targets, new ReleaseRepository());
        var service = provider.GetRequiredService<IArtifactDeliveryInteractionService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetForPull(runtimeNodeId, target.Id));
    }

    [Fact]
    public async Task GetPendingForPullMarksPendingTargetsAvailableAndSkipsPushScheduledPullNodes()
    {
        var runtimeNodeId = Guid.NewGuid();
        var pending = CreateTarget(runtimeNodeId, ContractReleaseTargetStatus.Pending, DistributionMode.Pull);
        var pushScheduledPullNode = CreateTarget(runtimeNodeId, ContractReleaseTargetStatus.PushScheduled, DistributionMode.Pull);
        TargetRepository targets = new([pending, pushScheduledPullNode]);

        using var provider = CreateProvider(targets, new ReleaseRepository());
        var service = provider.GetRequiredService<IArtifactDeliveryInteractionService>();

        var packages = await service.GetPendingForPull(runtimeNodeId);

        var package = Assert.Single(packages);
        Assert.Equal(pending.Id, package.ReleaseTargetId);
        Assert.Equal(ContractReleaseTargetStatus.AvailableForPull, pending.Status);
        Assert.Equal(ContractReleaseTargetStatus.PushScheduled, pushScheduledPullNode.Status);
    }

    [Fact]
    public async Task GetForPullRejectsTargetsForDifferentRuntimeOrUnavailableState()
    {
        var runtimeNodeId = Guid.NewGuid();
        var target = CreateTarget(runtimeNodeId, ContractReleaseTargetStatus.Delivered, DistributionMode.Hybrid);
        TargetRepository targets = new([target]);

        using var provider = CreateProvider(targets, new ReleaseRepository());
        var service = provider.GetRequiredService<IArtifactDeliveryInteractionService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetForPull(Guid.NewGuid(), target.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetForPull(runtimeNodeId, target.Id));
    }

    [Fact]
    public async Task AcknowledgePullWithProcessingStatusLeavesActivationInProgress()
    {
        var runtimeNodeId = Guid.NewGuid();
        var target = CreateTarget(runtimeNodeId, ContractReleaseTargetStatus.AvailableForPull);
        TargetRepository targets = new([target]);

        using var provider = CreateProvider(targets, new ReleaseRepository());
        var service = provider.GetRequiredService<IArtifactDeliveryInteractionService>();

        var result = await service.AcknowledgePull(runtimeNodeId, target.Id, " runtime-789 ", "Processing");

        Assert.True(result.Succeeded);
        Assert.Equal(ContractReleaseTargetStatus.Acknowledged, target.Status);
        Assert.Equal(ContractReleaseActivationStatus.Activating, target.ActivationStatus);
        Assert.Equal("runtime-789", target.RuntimeVersionApplied);
        Assert.Contains(targets.Attempts, x => x.Action == "Ack" && x.ExternalReference == "runtime-789");
    }

    private static ServiceProvider CreateProvider(
        IContractReleaseTargetRepository targets,
        IContractReleaseRepository releases,
        HttpMessageHandler? handler = null)
    {
        var services = new ServiceCollection()
            .AddSingleton(targets)
            .AddSingleton(releases);

        services.AddKnOwlControlPlaneDistributionApplication();
        if (handler is not null)
        {
            services.AddHttpClient("KnOwlRuntimeDistribution")
                .ConfigurePrimaryHttpMessageHandler(() => handler);
        }

        services.RemoveAll<IRuntimeAccessTokenProvider>();
        services.AddSingleton<IRuntimeAccessTokenProvider, StubRuntimeAccessTokenProvider>();
        return services.BuildServiceProvider();
    }

    private static void ConfigurePushRuntime(RuntimeNode runtimeNode, string endpointPath)
    {
        runtimeNode.EndpointBaseUri = "https://runtime.example.test/";
        runtimeNode.EndpointApiPath = endpointPath;
        runtimeNode.OutboundCredentialStatus = ConnectionCredentialStatus.Active;
    }

    private static ContractReleaseTarget CreateTarget(Guid runtimeNodeId, ContractReleaseTargetStatus status, DistributionMode mode = DistributionMode.Pull)
    {
        var artifact = new ContractArtifact
        {
            Id = Guid.NewGuid(),
            ArtifactType = ContractArtifactType.Event,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Name = "Customer Created",
            Topic = "customer.created",
            VersionNumber = "1.0.0",
            PayloadSchemaJson = "{}",
            ContentHash = "hash-a",
            SourceStatus = "Deployed"
        };

        return new ContractReleaseTarget
        {
            Id = Guid.NewGuid(),
            ReleaseId = Guid.NewGuid(),
            ReleaseItemId = Guid.NewGuid(),
            RuntimeNodeId = runtimeNodeId,
            ArtifactId = artifact.Id,
            Artifact = artifact,
            RuntimeNode = new RuntimeNode
            {
                Id = runtimeNodeId,
                Name = "Runtime",
                Code = "runtime",
                DistributionMode = mode,
                IsEnabled = true,
                Status = RuntimeNodeStatus.Active
            },
            Status = status,
            ActivationStatus = ContractReleaseActivationStatus.NotActivated,
            RolloutGroup = "ManualRelease",
            CorrelationId = string.Empty
        };
    }

    private sealed class StubRuntimeAccessTokenProvider : IRuntimeAccessTokenProvider
    {
        public Task AttachToken(
            HttpRequestMessage request,
            RuntimeNode node,
            IReadOnlyCollection<ArtifactDeliveryScope> scopes,
            CancellationToken cancellationToken = default)
        {
            Assert.Contains(ArtifactDeliveryScope.ArtifactPush, scopes);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "stub-token");
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        private string body = string.Empty;

        public HttpRequestMessage? Request { get; private set; }

        public Task<string> ReadBody() => Task.FromResult(body);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return responder(request);
        }
    }

    private sealed class ReleaseRepository : IContractReleaseRepository
    {
        public ContractReleaseStatus? Status { get; private set; }
        public Task<IReadOnlyList<ContractRelease>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractRelease>>([]);
        public Task<ContractRelease?> GetById(Guid id, bool includeItems = false, bool includeTargets = false, CancellationToken cancellationToken = default) => Task.FromResult<ContractRelease?>(null);
        public Task Create(ContractRelease release, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateStatus(Guid id, ContractReleaseStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default)
        {
            Status = status;
            return Task.CompletedTask;
        }
    }

    private sealed class TargetRepository(List<ContractReleaseTarget> targets) : IContractReleaseTargetRepository
    {
        public List<ContractReleaseAttempt> Attempts { get; } = [];
        public Task<IReadOnlyList<ContractReleaseTarget>> GetByRelease(Guid releaseId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractReleaseTarget>>(targets.Where(x => x.ReleaseId == releaseId).ToList());
        public Task<IReadOnlyList<ContractReleaseTarget>> GetPendingForRuntimeNode(Guid runtimeNodeId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractReleaseTarget>>(targets.Where(x => x.RuntimeNodeId == runtimeNodeId && (x.Status == ContractReleaseTargetStatus.AvailableForPull || x.Status == ContractReleaseTargetStatus.PushScheduled || x.Status == ContractReleaseTargetStatus.Pending)).ToList());
        public Task<ContractReleaseTarget?> GetById(Guid id, bool includeArtifact = false, CancellationToken cancellationToken = default) => Task.FromResult(targets.FirstOrDefault(x => x.Id == id));
        public Task CreateMany(IReadOnlyCollection<ContractReleaseTarget> values, CancellationToken cancellationToken = default) { targets.AddRange(values); return Task.CompletedTask; }
        public Task Update(ContractReleaseTarget target, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddAttempt(ContractReleaseAttempt attempt, CancellationToken cancellationToken = default) { Attempts.Add(attempt); return Task.CompletedTask; }
    }
}


