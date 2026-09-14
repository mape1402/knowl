using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Application.Distribution;
using KnOwl.ControlPlane.Application.Distribution.ArtifactDelivery;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.Extensions.DependencyInjection;

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

    private static ServiceProvider CreateProvider(IContractReleaseTargetRepository targets, IContractReleaseRepository releases)
    {
        return new ServiceCollection()
            .AddSingleton(targets)
            .AddSingleton(releases)
            .AddKnOwlControlPlaneDistributionApplication()
            .BuildServiceProvider();
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
            RolloutGroup = "ManualRelease"
        };
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
        public Task<IReadOnlyList<ContractReleaseTarget>> GetPendingForRuntimeNode(Guid runtimeNodeId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractReleaseTarget>>(targets.Where(x => x.RuntimeNodeId == runtimeNodeId && (x.Status == ContractReleaseTargetStatus.AvailableForPull || x.Status == ContractReleaseTargetStatus.PushScheduled)).ToList());
        public Task<ContractReleaseTarget?> GetById(Guid id, bool includeArtifact = false, CancellationToken cancellationToken = default) => Task.FromResult(targets.FirstOrDefault(x => x.Id == id));
        public Task CreateMany(IReadOnlyCollection<ContractReleaseTarget> values, CancellationToken cancellationToken = default) { targets.AddRange(values); return Task.CompletedTask; }
        public Task Update(ContractReleaseTarget target, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddAttempt(ContractReleaseAttempt attempt, CancellationToken cancellationToken = default) { Attempts.Add(attempt); return Task.CompletedTask; }
    }
}


