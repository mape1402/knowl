using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Tests;

public sealed class ContractSnapshotValidationServiceTests
{
    [Fact]
    public async Task ValidatePayloadSchemaBlocksMissingDefinitionReference()
    {
        using var provider = CreateProvider(new SnapshotSchemaTypeRepository());
        var validator = provider.GetRequiredService<IContractSnapshotValidationService>();

        var result = await validator.ValidatePayloadSchema("""
            {
              "properties": {
                "contact": { "$ref": "#/$defs/Contact@1.0.0" }
              },
              "$defs": {}
            }
            """);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Contains("Contact@1.0.0", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidatePayloadSchemaBlocksMissingTypeVersion()
    {
        using var provider = CreateProvider(new SnapshotSchemaTypeRepository());
        var validator = provider.GetRequiredService<IContractSnapshotValidationService>();
        var missingVersionId = Guid.NewGuid();

        var result = await validator.ValidatePayloadSchema($$"""
            {
              "properties": {
                "contact": {
                  "$ref": "#/$defs/Contact@1.0.0",
                  "typeVersionId": "{{missingVersionId}}"
                }
              },
              "$defs": {
                "Contact@1.0.0": { "type": "object", "properties": {} }
              }
            }
            """);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Contains(missingVersionId.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidatePayloadSchemaAllowsEmbeddedDefinitionAndExistingTypeVersion()
    {
        var versionId = Guid.NewGuid();
        using var provider = CreateProvider(new SnapshotSchemaTypeRepository
        {
            Versions =
            [
                new SchemaTypeVersion { Id = versionId, VersionNumber = "1.0.0", DefinitionJson = "{}" }
            ]
        });
        var validator = provider.GetRequiredService<IContractSnapshotValidationService>();

        var result = await validator.ValidatePayloadSchema($$"""
            {
              "properties": {
                "contact": {
                  "$ref": "#/$defs/Contact@1.0.0",
                  "typeVersionId": "{{versionId}}"
                }
              },
              "$defs": {
                "Contact@1.0.0": { "type": "object", "properties": {} }
              }
            }
            """);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateEventAndCommandVersionsCoverMissingInvalidAndReplyBranches()
    {
        SnapshotEventRepository events = new();
        SnapshotCommandRepository commands = new();
        using var provider = CreateProvider(new SnapshotSchemaTypeRepository(), events, commands);
        var validator = provider.GetRequiredService<IContractSnapshotValidationService>();

        var missingEvent = await validator.ValidateEventVersion(Guid.NewGuid());
        var missingCommand = await validator.ValidateCommandVersion(Guid.NewGuid());

        commands.Version = new CommandVersion { Id = Guid.NewGuid(), PayloadSchemaJson = "{" };
        var invalidJson = await validator.ValidateCommandVersion(commands.Version.Id);

        commands.Version = new CommandVersion { Id = Guid.NewGuid(), PayloadSchemaJson = "[]" };
        var invalidRoot = await validator.ValidateCommandVersion(commands.Version.Id);

        commands.Version = new CommandVersion { Id = Guid.NewGuid(), PayloadSchemaJson = """{"type":"object"}""" };
        var validWithoutReply = await validator.ValidateCommandVersion(commands.Version.Id);

        commands.Version = new CommandVersion
        {
            Id = Guid.NewGuid(),
            PayloadSchemaJson = """{"type":"object"}""",
            ReplyPayloadSchemaJson = """{"properties":{"reply":{"$ref":"#/$defs/Missing"}},"$defs":{}}"""
        };
        var invalidReply = await validator.ValidateCommandVersion(commands.Version.Id);

        events.Version = new EventVersion { Id = Guid.NewGuid(), PayloadSchemaJson = """{"type":"object"}""" };
        var validEvent = await validator.ValidateEventVersion(events.Version.Id);

        Assert.False(missingEvent.IsValid);
        Assert.False(missingCommand.IsValid);
        Assert.False(invalidJson.IsValid);
        Assert.False(invalidRoot.IsValid);
        Assert.True(validWithoutReply.IsValid);
        Assert.False(invalidReply.IsValid);
        Assert.All(invalidReply.Errors, error => Assert.StartsWith("Reply ", error, StringComparison.Ordinal));
        Assert.True(validEvent.IsValid);
    }

    [Fact]
    public async Task ValidatePayloadSchemaTraversesArraysAndIgnoresNonDefinitionRefs()
    {
        var versionId = Guid.NewGuid();
        using var provider = CreateProvider(new SnapshotSchemaTypeRepository
        {
            Versions = [new SchemaTypeVersion { Id = versionId, VersionNumber = "1.0.0", DefinitionJson = "{}" }]
        });
        var validator = provider.GetRequiredService<IContractSnapshotValidationService>();

        var result = await validator.ValidatePayloadSchema($$"""
            {
              "properties": {
                "items": [
                  { "$ref": "#/$defs/Known", "typeVersionId": "{{versionId}}" },
                  { "$ref": "#/components/schemas/External" },
                  { "$ref": "#/$defs/Missing" }
                ]
              },
              "$defs": {
                "Known": { "type": "object" }
              }
            }
            """);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Missing", result.Errors.Single(), StringComparison.Ordinal);
    }

    private static ServiceProvider CreateProvider(
        ISchemaTypeRepository schemaTypes,
        IEventRepository? events = null,
        ICommandRepository? commands = null)
    {
        return new ServiceCollection()
            .AddSingleton(events ?? new SnapshotEventRepository())
            .AddSingleton(commands ?? new SnapshotCommandRepository())
            .AddSingleton(schemaTypes)
            .AddSingleton<IContractFieldMetadataRepository>(new SnapshotMetadataRepository())
            .AddKnOwlControlPlaneApplication()
            .BuildServiceProvider();
    }

    private sealed class SnapshotSchemaTypeRepository : ISchemaTypeRepository
    {
        public List<SchemaTypeVersion> Versions { get; init; } = [];

        public Task<IReadOnlyList<SchemaTypeDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SchemaTypeDefinition>>([]);
        public Task<IReadOnlyList<SchemaTypeVersion>> GetActiveVersionsWithDefinitions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SchemaTypeVersion>>(Versions);
        public Task<SchemaTypeDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult<SchemaTypeDefinition?>(null);
        public Task<SchemaTypeVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(Versions.FirstOrDefault(x => x.Id == versionId));
        public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> VersionExists(Guid typeId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task Create(SchemaTypeDefinition schemaType, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid typeId, SchemaTypeVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetVersionActive(Guid typeId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class SnapshotEventRepository : IEventRepository
    {
        public EventVersion? Version { get; set; }

        public Task<IReadOnlyList<EventDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<EventDefinition>>([]);
        public Task<EventDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult<EventDefinition?>(null);
        public Task<bool> VersionExists(Guid eventId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<EventVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(Version?.Id == versionId ? Version : null);
        public Task Create(EventDefinition eventDefinition, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid eventId, EventVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateVersionStatus(Guid versionId, ContractVersionStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Delete(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class SnapshotCommandRepository : ICommandRepository
    {
        public CommandVersion? Version { get; set; }

        public Task<IReadOnlyList<CommandDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CommandDefinition>>([]);
        public Task<CommandDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult<CommandDefinition?>(null);
        public Task<bool> VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<CommandVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(Version?.Id == versionId ? Version : null);
        public Task Create(CommandDefinition commandDefinition, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid commandId, CommandVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateVersionStatus(Guid versionId, ContractVersionStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Delete(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class SnapshotMetadataRepository : IContractFieldMetadataRepository
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
