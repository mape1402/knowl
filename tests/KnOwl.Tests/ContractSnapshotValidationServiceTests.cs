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

    private static ServiceProvider CreateProvider(ISchemaTypeRepository schemaTypes)
    {
        return new ServiceCollection()
            .AddSingleton<IEventRepository>(new SnapshotEventRepository())
            .AddSingleton<ICommandRepository>(new SnapshotCommandRepository())
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

    private sealed class SnapshotCommandRepository : ICommandRepository
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
