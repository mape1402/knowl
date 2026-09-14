using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Application.Distribution;
using KnOwl.ControlPlane.Application.Distribution.ReleaseBundles;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Tests;

public sealed class ContractReleaseInteractionServiceTests
{
    [Fact]
    public async Task CreateBuildsReleaseWithSelectedArtifacts()
    {
        var artifact = CreateArtifact("customer.created", "1.0.0");
        ReleaseRepository releases = new();

        using var provider = CreateProvider(new ReleaseArtifactRepository([artifact]), releases);
        var service = provider.GetRequiredService<IContractReleaseInteractionService>();

        var release = await service.Create("Customer contracts", null, [artifact.Id]);

        Assert.Equal("Customer contracts", release.Name);
        Assert.Single(release.Items);
        Assert.Same(release, releases.Items.Single());
    }

    [Fact]
    public async Task CreateBlocksDuplicateArtifactIdentity()
    {
        var first = CreateArtifact("customer.created", "1.0.0");
        var second = CreateArtifact("customer.created", "1.0.0");

        using var provider = CreateProvider(new ReleaseArtifactRepository([first, second]), new ReleaseRepository());
        var service = provider.GetRequiredService<IContractReleaseInteractionService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.Create("Duplicated", null, [first.Id, second.Id]));
    }

    [Fact]
    public async Task PlanCreatesTargetsForSelectedRuntimeNodes()
    {
        var artifact = CreateArtifact("customer.created", "1.0.0");
        ReleaseRepository releases = new();
        ReleaseTargetRepository targets = new();
        var pullNode = new RuntimeNode
        {
            Id = Guid.NewGuid(),
            Name = "Pull runtime",
            Code = "pull-runtime",
            DistributionMode = DistributionMode.Pull,
            IsEnabled = true,
            Status = RuntimeNodeStatus.Active
        };
        var pushNode = new RuntimeNode
        {
            Id = Guid.NewGuid(),
            Name = "Push runtime",
            Code = "push-runtime",
            DistributionMode = DistributionMode.Push,
            IsEnabled = true,
            Status = RuntimeNodeStatus.Active
        };

        using var provider = CreateProvider(
            new ReleaseArtifactRepository([artifact]),
            releases,
            new RuntimeNodeRepository([pullNode, pushNode]),
            targets);
        var service = provider.GetRequiredService<IContractReleaseInteractionService>();

        var release = await service.Create("Customer contracts", null, [artifact.Id]);
        await service.Plan(release.Id, [pullNode.Id, pushNode.Id]);

        Assert.Equal(ContractReleaseStatus.InProgress, release.Status);
        Assert.Equal(2, targets.Items.Count);
        Assert.Contains(targets.Items, x => x.RuntimeNodeId == pullNode.Id && x.Status == ContractReleaseTargetStatus.AvailableForPull);
        Assert.Contains(targets.Items, x => x.RuntimeNodeId == pushNode.Id && x.Status == ContractReleaseTargetStatus.PushScheduled);
    }

    [Fact]
    public async Task PlanDoesNotDuplicateTargetsWhenRepositoryFixesNavigation()
    {
        var artifact = CreateArtifact("customer.created", "1.0.0");
        ReleaseRepository releases = new();
        NavigationFixupReleaseTargetRepository targets = new(releases);
        var runtimeNode = new RuntimeNode
        {
            Id = Guid.NewGuid(),
            Name = "Push runtime",
            Code = "push-runtime",
            DistributionMode = DistributionMode.Push,
            IsEnabled = true,
            Status = RuntimeNodeStatus.Active
        };

        using var provider = CreateProvider(
            new ReleaseArtifactRepository([artifact]),
            releases,
            new RuntimeNodeRepository([runtimeNode]),
            targets);
        var service = provider.GetRequiredService<IContractReleaseInteractionService>();

        var release = await service.Create("Customer contracts", null, [artifact.Id]);
        await service.Plan(release.Id, [runtimeNode.Id]);

        Assert.Single(targets.Items);
        Assert.Single(release.Targets);
        Assert.Same(targets.Items.Single(), release.Targets.Single());
    }

    [Fact]
    public async Task PlanBlocksDisabledRuntimeNodes()
    {
        var artifact = CreateArtifact("customer.created", "1.0.0");
        ReleaseRepository releases = new();
        var runtimeNode = new RuntimeNode
        {
            Id = Guid.NewGuid(),
            Name = "Disabled runtime",
            Code = "disabled-runtime",
            DistributionMode = DistributionMode.Pull,
            IsEnabled = false,
            Status = RuntimeNodeStatus.Active
        };

        using var provider = CreateProvider(
            new ReleaseArtifactRepository([artifact]),
            releases,
            new RuntimeNodeRepository([runtimeNode]),
            new ReleaseTargetRepository());
        var service = provider.GetRequiredService<IContractReleaseInteractionService>();

        var release = await service.Create("Customer contracts", null, [artifact.Id]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.Plan(release.Id, [runtimeNode.Id]));
    }

    [Fact]
    public async Task CreateAndExecuteMakesPullTargetsAvailable()
    {
        var artifact = CreateArtifact("customer.created", "1.0.0");
        ReleaseRepository releases = new();
        ReleaseTargetRepository targets = new();
        var runtimeNode = new RuntimeNode
        {
            Id = Guid.NewGuid(),
            Name = "Pull runtime",
            Code = "pull-runtime",
            DistributionMode = DistributionMode.Pull,
            IsEnabled = true,
            Status = RuntimeNodeStatus.Active
        };

        using var provider = CreateProvider(
            new ReleaseArtifactRepository([artifact]),
            releases,
            new RuntimeNodeRepository([runtimeNode]),
            new ReleaseTargetRepository([runtimeNode], [artifact]));
        var service = provider.GetRequiredService<IContractReleaseExecutionService>();

        var result = await service.CreateAndExecute("Customer contracts", null, [artifact.Id], [runtimeNode.Id], initiatedBy: "unit-test");

        Assert.Equal(1, result.TotalTargets);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal(1, result.AvailableForPull);
        var target = Assert.Single(((ReleaseTargetRepository)provider.GetRequiredService<IContractReleaseTargetRepository>()).Items);
        Assert.Equal(ContractReleaseTargetStatus.AvailableForPull, target.Status);
        var attempt = Assert.Single(((ReleaseTargetRepository)provider.GetRequiredService<IContractReleaseTargetRepository>()).Attempts);
        Assert.Equal("Pull", attempt.Action);
        Assert.Equal("unit-test", attempt.InitiatedBy);
    }


    private static ContractArtifact CreateArtifact(string topic, string version)
    {
        return new ContractArtifact
        {
            Id = Guid.NewGuid(),
            ArtifactType = ContractArtifactType.Event,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Name = topic,
            Topic = topic,
            VersionNumber = version,
            PayloadSchemaJson = "{}",
            ContentHash = Guid.NewGuid().ToString("N"),
            SourceStatus = ContractVersionStatus.Approved.ToString()
        };
    }

    private static ServiceProvider CreateProvider(
        IContractArtifactRepository artifacts,
        IContractReleaseRepository releases,
        IRuntimeNodeRepository? runtimeNodes = null,
        IContractReleaseTargetRepository? releaseTargets = null)
    {
        return new ServiceCollection()
            .AddSingleton<IEventRepository>(new EmptyEventRepository())
            .AddSingleton<ICommandRepository>(new EmptyCommandRepository())
            .AddSingleton<ISchemaTypeRepository>(new EmptySchemaTypeRepository())
            .AddSingleton<IContractFieldMetadataRepository>(new EmptyMetadataRepository())
            .AddSingleton(artifacts)
            .AddSingleton(releases)
            .AddSingleton(runtimeNodes ?? new RuntimeNodeRepository([]))
            .AddSingleton(releaseTargets ?? new ReleaseTargetRepository())
            .AddKnOwlControlPlaneApplication()
            .AddKnOwlControlPlaneDistributionApplication()
            .BuildServiceProvider();
    }

    private sealed class ReleaseArtifactRepository(List<ContractArtifact> artifacts) : IContractArtifactRepository
    {
        public Task<IReadOnlyList<ContractArtifact>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractArtifact>>(artifacts);
        public Task<IReadOnlyList<ContractArtifact>> GetDeployed(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractArtifact>>(artifacts.Where(IsDeployed).OrderByDescending(x => x.CreatedAtUtc).ToList());
        public Task<ContractArtifact?> GetById(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(artifacts.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<ContractArtifact>> GetByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractArtifact>>(artifacts.Where(x => ids.Contains(x.Id)).ToList());
        public Task<ContractArtifact?> GetBySourceVersion(ContractArtifactType artifactType, Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.VersionId == versionId));
        public Task<ContractArtifact?> GetByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));
        public Task<ContractArtifact?> GetDeployedByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(artifacts.FirstOrDefault(x => IsDeployed(x) && x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));
        public Task<ContractArtifact?> GetLatestDeployed(ContractArtifactType artifactType, string topic, CancellationToken cancellationToken = default) => Task.FromResult(artifacts.Where(x => IsDeployed(x) && x.ArtifactType == artifactType && x.Topic == topic).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault());
        public Task Create(ContractArtifact artifact, CancellationToken cancellationToken = default) => Task.CompletedTask;

        private static bool IsDeployed(ContractArtifact artifact)
            => string.Equals(artifact.SourceStatus, ContractVersionStatus.Deployed.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ReleaseRepository : IContractReleaseRepository
    {
        public List<ContractRelease> Items { get; init; } = [];
        public Task<IReadOnlyList<ContractRelease>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractRelease>>(Items);
        public Task<ContractRelease?> GetById(Guid id, bool includeItems = false, bool includeTargets = false, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task Create(ContractRelease release, CancellationToken cancellationToken = default) { Items.Add(release); return Task.CompletedTask; }
        public Task UpdateStatus(Guid id, ContractReleaseStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default) { Items.First(x => x.Id == id).Status = status; return Task.CompletedTask; }
    }

    private sealed class RuntimeNodeRepository(List<RuntimeNode> runtimeNodes) : IRuntimeNodeRepository
    {
        public Task<IReadOnlyList<RuntimeNode>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RuntimeNode>>(runtimeNodes);
        public Task<IReadOnlyList<RuntimeNode>> GetActiveEnabled(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RuntimeNode>>(runtimeNodes.Where(x => x.IsEnabled && x.Status == RuntimeNodeStatus.Active).ToList());
        public Task<RuntimeNode?> GetById(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(runtimeNodes.FirstOrDefault(x => x.Id == id));
        public Task<RuntimeNode?> GetByCode(string code, CancellationToken cancellationToken = default) => Task.FromResult(runtimeNodes.FirstOrDefault(x => x.Code == code));
        public Task<RuntimeNode?> GetByInboundClientId(string clientId, CancellationToken cancellationToken = default) => Task.FromResult(runtimeNodes.FirstOrDefault(x => x.InboundClientId == clientId));
        public Task Create(RuntimeNode runtimeNode, CancellationToken cancellationToken = default) { runtimeNodes.Add(runtimeNode); return Task.CompletedTask; }
        public Task Update(RuntimeNode runtimeNode, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetIsEnabled(Guid id, bool isEnabled, DateTime updatedAtUtc, CancellationToken cancellationToken = default) { runtimeNodes.First(x => x.Id == id).IsEnabled = isEnabled; return Task.CompletedTask; }
    }

    private sealed class ReleaseTargetRepository(
        IReadOnlyCollection<RuntimeNode>? runtimeNodes = null,
        IReadOnlyCollection<ContractArtifact>? artifacts = null) : IContractReleaseTargetRepository
    {
        public List<ContractReleaseTarget> Items { get; } = [];
        public List<ContractReleaseAttempt> Attempts { get; } = [];
        public Task<IReadOnlyList<ContractReleaseTarget>> GetByRelease(Guid releaseId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractReleaseTarget>>(Items.Where(x => x.ReleaseId == releaseId).ToList());
        public Task<IReadOnlyList<ContractReleaseTarget>> GetPendingForRuntimeNode(Guid runtimeNodeId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractReleaseTarget>>(Items.Where(x => x.RuntimeNodeId == runtimeNodeId).ToList());
        public Task<ContractReleaseTarget?> GetById(Guid id, bool includeArtifact = false, CancellationToken cancellationToken = default)
        {
            var target = Items.FirstOrDefault(x => x.Id == id);
            if (includeArtifact && target is not null)
            {
                target.RuntimeNode ??= runtimeNodes?.FirstOrDefault(x => x.Id == target.RuntimeNodeId);
                target.Artifact ??= artifacts?.FirstOrDefault(x => x.Id == target.ArtifactId);
            }

            return Task.FromResult(target);
        }
        public Task CreateMany(IReadOnlyCollection<ContractReleaseTarget> targets, CancellationToken cancellationToken = default) { Items.AddRange(targets); return Task.CompletedTask; }
        public Task Update(ContractReleaseTarget target, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddAttempt(ContractReleaseAttempt attempt, CancellationToken cancellationToken = default) { Attempts.Add(attempt); return Task.CompletedTask; }
    }

    private sealed class NavigationFixupReleaseTargetRepository(ReleaseRepository releases) : IContractReleaseTargetRepository
    {
        public List<ContractReleaseTarget> Items { get; } = [];
        public Task<IReadOnlyList<ContractReleaseTarget>> GetByRelease(Guid releaseId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractReleaseTarget>>(Items.Where(x => x.ReleaseId == releaseId).ToList());
        public Task<IReadOnlyList<ContractReleaseTarget>> GetPendingForRuntimeNode(Guid runtimeNodeId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractReleaseTarget>>(Items.Where(x => x.RuntimeNodeId == runtimeNodeId).ToList());
        public Task<ContractReleaseTarget?> GetById(Guid id, bool includeArtifact = false, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task CreateMany(IReadOnlyCollection<ContractReleaseTarget> targets, CancellationToken cancellationToken = default)
        {
            Items.AddRange(targets);
            foreach (var target in targets)
            {
                releases.Items.First(x => x.Id == target.ReleaseId).Targets.Add(target);
            }

            return Task.CompletedTask;
        }

        public Task Update(ContractReleaseTarget target, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddAttempt(ContractReleaseAttempt attempt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class EmptyEventRepository : IEventRepository
    {
        public Task<IReadOnlyList<EventDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<EventDefinition>>([]);
        public Task<EventDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult<EventDefinition?>(null);
        public Task<bool> VersionExists(Guid eventId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<EventVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult<EventVersion?>(null);
        public Task Create(EventDefinition eventDefinition, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid eventId, EventVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateVersionStatus(Guid versionId, ContractVersionStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Delete(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class EmptyCommandRepository : ICommandRepository
    {
        public Task<IReadOnlyList<CommandDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CommandDefinition>>([]);
        public Task<CommandDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult<CommandDefinition?>(null);
        public Task<bool> VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<CommandVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult<CommandVersion?>(null);
        public Task Create(CommandDefinition commandDefinition, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid commandId, CommandVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateVersionStatus(Guid versionId, ContractVersionStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Delete(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class EmptySchemaTypeRepository : ISchemaTypeRepository
    {
        public Task<IReadOnlyList<SchemaTypeDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SchemaTypeDefinition>>([]);
        public Task<IReadOnlyList<SchemaTypeVersion>> GetActiveVersionsWithDefinitions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SchemaTypeVersion>>([]);
        public Task<SchemaTypeDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult<SchemaTypeDefinition?>(null);
        public Task<SchemaTypeVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult<SchemaTypeVersion?>(null);
        public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> VersionExists(Guid typeId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task Create(SchemaTypeDefinition schemaType, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid typeId, SchemaTypeVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetVersionActive(Guid typeId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class EmptyMetadataRepository : IContractFieldMetadataRepository
    {
        public Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractFieldMetadataDefinition>>([]);
        public Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetActiveWithVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractFieldMetadataDefinition>>([]);
        public Task<ContractFieldMetadataDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult<ContractFieldMetadataDefinition?>(null);
        public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> VersionExists(Guid metadataFieldId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task Create(ContractFieldMetadataDefinition metadataField, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpsertVersion(Guid metadataFieldId, ContractFieldMetadataVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetVersionActive(Guid metadataFieldId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}



