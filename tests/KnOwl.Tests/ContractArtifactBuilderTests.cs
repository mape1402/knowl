using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Application.Distribution;
using KnOwl.ControlPlane.Application.Distribution.Artifacts;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Tests;

public sealed class ContractArtifactBuilderTests
{
    [Fact]
    public async Task BuildEventArtifactCreatesImmutableArtifactForApprovedVersion()
    {
        var versionId = Guid.NewGuid();
        ArtifactEventRepository events = new()
        {
            Version = new EventVersion
            {
                Id = versionId,
                EventDefinitionId = Guid.NewGuid(),
                VersionNumber = "1.0.0",
                Status = ContractVersionStatus.Approved,
                PayloadSchemaJson = "{\"properties\":{},\"$defs\":{}}",
                EventDefinition = new EventDefinition { Name = "Customer Registered", Topic = "customer.registered" }
            }
        };
        ArtifactRepository artifacts = new();

        using var provider = CreateProvider(events, new ArtifactCommandRepository(), artifacts);
        var builder = provider.GetRequiredService<IContractArtifactBuilder>();

        var artifact = await builder.BuildEventArtifact(versionId);

        Assert.Equal(ContractArtifactType.Event, artifact.ArtifactType);
        Assert.Equal(versionId, artifact.VersionId);
        Assert.Equal("customer.registered", artifact.Topic);
        Assert.Equal("1.0.0", artifact.VersionNumber);
        Assert.False(string.IsNullOrWhiteSpace(artifact.ContentHash));
        Assert.Single(artifacts.Items);
    }

    [Fact]
    public async Task BuildEventArtifactIsIdempotentForSameSourceVersion()
    {
        var versionId = Guid.NewGuid();
        ArtifactEventRepository events = new()
        {
            Version = new EventVersion
            {
                Id = versionId,
                EventDefinitionId = Guid.NewGuid(),
                VersionNumber = "1.0.0",
                Status = ContractVersionStatus.Approved,
                PayloadSchemaJson = "{\"properties\":{},\"$defs\":{}}",
                EventDefinition = new EventDefinition { Name = "Customer Registered", Topic = "customer.registered" }
            }
        };
        ArtifactRepository artifacts = new();

        using var provider = CreateProvider(events, new ArtifactCommandRepository(), artifacts);
        var builder = provider.GetRequiredService<IContractArtifactBuilder>();

        var first = await builder.BuildEventArtifact(versionId);
        var second = await builder.BuildEventArtifact(versionId);

        Assert.Same(first, second);
        Assert.Single(artifacts.Items);
    }

    [Fact]
    public async Task BuildCommandArtifactBlocksDraftVersion()
    {
        var versionId = Guid.NewGuid();
        ArtifactCommandRepository commands = new()
        {
            Version = new CommandVersion
            {
                Id = versionId,
                CommandDefinitionId = Guid.NewGuid(),
                VersionNumber = "1.0.0",
                Status = ContractVersionStatus.Draft,
                PayloadSchemaJson = "{\"properties\":{},\"$defs\":{}}",
                CommandDefinition = new CommandDefinition { Name = "Register Customer", Topic = "customer.register" }
            }
        };

        using var provider = CreateProvider(new ArtifactEventRepository(), commands, new ArtifactRepository());
        var builder = provider.GetRequiredService<IContractArtifactBuilder>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildCommandArtifact(versionId));
    }

    private static ServiceProvider CreateProvider(IEventRepository events, ICommandRepository commands, IContractArtifactRepository artifacts)
    {
        return new ServiceCollection()
            .AddSingleton(events)
            .AddSingleton(commands)
            .AddSingleton<ISchemaTypeRepository>(new ArtifactSchemaTypeRepository())
            .AddSingleton<IContractFieldMetadataRepository>(new ArtifactMetadataRepository())
            .AddSingleton(artifacts)
            .AddKnOwlControlPlaneApplication()
            .AddKnOwlControlPlaneDistributionApplication()
            .BuildServiceProvider();
    }

    private sealed class ArtifactRepository : IContractArtifactRepository
    {
        public List<ContractArtifact> Items { get; } = [];
        public Task<IReadOnlyList<ContractArtifact>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractArtifact>>(Items);
        public Task<IReadOnlyList<ContractArtifact>> GetDeployed(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractArtifact>>(Items.Where(IsDeployed).OrderByDescending(x => x.CreatedAtUtc).ToList());
        public Task<ContractArtifact?> GetById(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<ContractArtifact>> GetByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractArtifact>>(Items.Where(x => ids.Contains(x.Id)).ToList());
        public Task<ContractArtifact?> GetBySourceVersion(ContractArtifactType artifactType, Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => x.ArtifactType == artifactType && x.VersionId == versionId));
        public Task<ContractArtifact?> GetByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));
        public Task<ContractArtifact?> GetDeployedByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => IsDeployed(x) && x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));
        public Task<ContractArtifact?> GetLatestDeployed(ContractArtifactType artifactType, string topic, CancellationToken cancellationToken = default) => Task.FromResult(Items.Where(x => IsDeployed(x) && x.ArtifactType == artifactType && x.Topic == topic).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault());

        public Task Create(ContractArtifact artifact, CancellationToken cancellationToken = default)
        {
            Items.Add(artifact);
            return Task.CompletedTask;
        }

        private static bool IsDeployed(ContractArtifact artifact)
            => string.Equals(artifact.SourceStatus, ContractVersionStatus.Deployed.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ArtifactEventRepository : IEventRepository
    {
        public EventVersion? Version { get; init; }
        public Task<IReadOnlyList<EventDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<EventDefinition>>([]);
        public Task<EventDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult<EventDefinition?>(null);
        public Task<bool> VersionExists(Guid eventId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<EventVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(Version);
        public Task Create(EventDefinition eventDefinition, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid eventId, EventVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateVersionStatus(Guid versionId, ContractVersionStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Delete(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ArtifactCommandRepository : ICommandRepository
    {
        public CommandVersion? Version { get; init; }
        public Task<IReadOnlyList<CommandDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CommandDefinition>>([]);
        public Task<CommandDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult<CommandDefinition?>(null);
        public Task<bool> VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<CommandVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(Version);
        public Task Create(CommandDefinition commandDefinition, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid commandId, CommandVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateVersionStatus(Guid versionId, ContractVersionStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Delete(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ArtifactSchemaTypeRepository : ISchemaTypeRepository
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

    private sealed class ArtifactMetadataRepository : IContractFieldMetadataRepository
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

