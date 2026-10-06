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

    [Fact]
    public async Task BuildEventArtifactRejectsMissingDefinitionSnapshot()
    {
        var versionId = Guid.NewGuid();
        using var provider = CreateProvider(
            new ArtifactEventRepository
            {
                Version = new EventVersion
                {
                    Id = versionId,
                    EventDefinitionId = Guid.NewGuid(),
                    VersionNumber = "1.0.0",
                    Status = ContractVersionStatus.Approved,
                    PayloadSchemaJson = "{}"
                }
            },
            new ArtifactCommandRepository(),
            new ArtifactRepository());

        var builder = provider.GetRequiredService<IContractArtifactBuilder>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildEventArtifact(versionId));
    }

    [Fact]
    public async Task BuildCommandArtifactRejectsMissingDefinitionSnapshot()
    {
        var versionId = Guid.NewGuid();
        using var provider = CreateProvider(
            new ArtifactEventRepository(),
            new ArtifactCommandRepository
            {
                Version = new CommandVersion
                {
                    Id = versionId,
                    CommandDefinitionId = Guid.NewGuid(),
                    VersionNumber = "1.0.0",
                    Status = ContractVersionStatus.Approved,
                    PayloadSchemaJson = "{}"
                }
            },
            new ArtifactRepository());

        var builder = provider.GetRequiredService<IContractArtifactBuilder>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildCommandArtifact(versionId));
    }

    [Fact]
    public async Task BuildArtifactRejectsExistingArtifactForSameSourceWithDifferentHash()
    {
        var sourceConflictVersion = CreateApprovedEventVersion("customer.created", "1.0.0");
        var sourceConflictArtifacts = new ArtifactRepository();
        sourceConflictArtifacts.Items.Add(new ContractArtifact
        {
            ArtifactType = ContractArtifactType.Event,
            VersionId = sourceConflictVersion.Id,
            Topic = "customer.created",
            VersionNumber = "1.0.0",
            ContentHash = "different"
        });
        using var provider = CreateProvider(new ArtifactEventRepository { Version = sourceConflictVersion }, new ArtifactCommandRepository(), sourceConflictArtifacts);

        var builder = provider.GetRequiredService<IContractArtifactBuilder>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildEventArtifact(sourceConflictVersion.Id));
    }

    [Fact]
    public async Task BuildArtifactRejectsExistingArtifactWithSameIdentityAndDifferentSource()
    {
        var identityConflictVersion = CreateApprovedEventVersion("customer.created", "1.0.0");
        var identityConflictArtifacts = new ArtifactRepository();
        identityConflictArtifacts.Items.Add(new ContractArtifact
        {
            ArtifactType = ContractArtifactType.Event,
            VersionId = Guid.NewGuid(),
            Topic = "customer.created",
            VersionNumber = "1.0.0",
            ContentHash = "different"
        });
        using var provider = CreateProvider(new ArtifactEventRepository { Version = identityConflictVersion }, new ArtifactCommandRepository(), identityConflictArtifacts);

        var builder = provider.GetRequiredService<IContractArtifactBuilder>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildEventArtifact(identityConflictVersion.Id));
    }

    [Fact]
    public async Task BuildCommandArtifactsCreatesSingleCommandArtifactWithRequestAndReplySchemas()
    {
        var versionId = Guid.NewGuid();
        ArtifactCommandRepository commands = new()
        {
            Version = new CommandVersion
            {
                Id = versionId,
                CommandDefinitionId = Guid.NewGuid(),
                VersionNumber = "1.0.0",
                Status = ContractVersionStatus.Approved,
                PayloadSchemaJson = "{\"type\":\"object\",\"properties\":{\"customerId\":{\"type\":\"string\"}}}",
                ReplyPayloadSchemaJson = "{\"type\":\"object\",\"properties\":{\"accepted\":{\"type\":\"boolean\"}}}",
                CommandDefinition = new CommandDefinition { Name = "Register Customer", Topic = "customer.register" }
            }
        };
        ArtifactRepository artifacts = new();

        using var provider = CreateProvider(new ArtifactEventRepository(), commands, artifacts);
        var builder = provider.GetRequiredService<IContractArtifactBuilder>();

        var created = await builder.BuildCommandArtifacts(versionId);

        var artifact = Assert.Single(created);
        Assert.Equal(ContractArtifactType.Command, artifact.ArtifactType);
        Assert.Equal("Register Customer", artifact.Name);
        Assert.Equal("customer.register", artifact.Topic);
        var payload = CommandArtifactPayloadDocument.Read(artifact.PayloadSchemaJson);
        Assert.Contains("customerId", payload.RequestPayloadSchemaJson, StringComparison.Ordinal);
        Assert.Contains("accepted", payload.ReplyPayloadSchemaJson!, StringComparison.Ordinal);
        Assert.Single(artifacts.Items);
    }

    [Fact]
    public void CommandArtifactPayloadDocumentReadsEmptyAndLegacyPayloads()
    {
        var empty = CommandArtifactPayloadDocument.Read(string.Empty);
        var legacy = CommandArtifactPayloadDocument.Read("[]");

        Assert.Equal("{}", empty.RequestPayloadSchemaJson);
        Assert.Null(empty.ReplyPayloadSchemaJson);
        Assert.Equal("[]", legacy.RequestPayloadSchemaJson);
        Assert.Null(legacy.ReplyPayloadSchemaJson);
    }

    [Fact]
    public void CommandArtifactPayloadDocumentReadsComposedPayloadWithoutReply()
    {
        var withoutReply = CommandArtifactPayloadDocument.Read(CommandArtifactPayloadDocument.Compose("{\"type\":\"object\"}", null));
        var withNullReply = CommandArtifactPayloadDocument.Read("""{"request":{"type":"object"},"reply":null}""");

        Assert.Equal("""{"type":"object"}""", withoutReply.RequestPayloadSchemaJson);
        Assert.Null(withoutReply.ReplyPayloadSchemaJson);
        Assert.Null(withNullReply.ReplyPayloadSchemaJson);
    }

    [Fact]
    public void CommandArtifactPayloadDocumentRejectsInvalidPayloads()
    {
        Assert.Throws<ArgumentException>(() => CommandArtifactPayloadDocument.Compose(string.Empty, null));
        Assert.Throws<ArgumentException>(() => CommandArtifactPayloadDocument.Compose("{", null));
        Assert.Throws<ArgumentException>(() => CommandArtifactPayloadDocument.Compose("{\"type\":\"object\"}", "{"));
    }

    private static EventVersion CreateApprovedEventVersion(string topic, string versionNumber)
        => new()
        {
            Id = Guid.NewGuid(),
            EventDefinitionId = Guid.NewGuid(),
            VersionNumber = versionNumber,
            Status = ContractVersionStatus.Approved,
            PayloadSchemaJson = "{}",
            EventDefinition = new EventDefinition { Name = topic, Topic = topic }
        };

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

