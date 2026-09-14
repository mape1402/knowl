using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Tests;

public sealed class InteractionServiceTests
{
    [Fact]
    public async Task EventServiceDelegatesToRepository()
    {
        var eventId = Guid.NewGuid();
        EventDefinition expected = new() { Id = eventId, Name = "Customer Registered", Topic = "customers.registered" };
        FakeEventRepository repository = new() { Entity = expected, VersionExistsResult = true };

        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IEventRepository>(repository)
            .AddKnOwlControlPlaneApplication()
            .BuildServiceProvider();

        var service = provider.GetRequiredService<IEventInteractionService>();
        var result = await service.GetById(eventId, includeVersions: true);
        var versionExists = await service.VersionExists(eventId, "1.0.0");

        Assert.Same(expected, result);
        Assert.True(versionExists);
        Assert.Equal(eventId, repository.LastGetById);
        Assert.True(repository.LastIncludeVersions);
        Assert.Equal((eventId, "1.0.0"), repository.LastVersionExists);
    }

    [Fact]
    public void RegistersAllInteractionServices()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IEventRepository>(new FakeEventRepository())
            .AddSingleton<ICommandRepository>(new FakeCommandRepository())
            .AddSingleton<ISchemaTypeRepository>(new FakeSchemaTypeRepository())
            .AddSingleton<IContractFieldMetadataRepository>(new FakeContractFieldMetadataRepository())
            .AddKnOwlControlPlaneApplication()
            .BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IEventInteractionService>());
        Assert.NotNull(provider.GetRequiredService<ICommandInteractionService>());
        Assert.NotNull(provider.GetRequiredService<ISchemaTypeInteractionService>());
        Assert.NotNull(provider.GetRequiredService<IContractFieldMetadataInteractionService>());
    }

    private sealed class FakeEventRepository : IEventRepository
    {
        public EventDefinition? Entity { get; init; }
        public bool VersionExistsResult { get; init; }
        public Guid LastGetById { get; private set; }
        public bool LastIncludeVersions { get; private set; }
        public (Guid Id, string Version)? LastVersionExists { get; private set; }

        public Task<IReadOnlyList<EventDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EventDefinition>>(Entity is null ? [] : [Entity]);

        public Task<EventDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
        {
            LastGetById = id;
            LastIncludeVersions = includeVersions;
            return Task.FromResult(Entity);
        }

        public Task<bool> VersionExists(Guid eventId, string versionNumber, CancellationToken cancellationToken = default)
        {
            LastVersionExists = (eventId, versionNumber);
            return Task.FromResult(VersionExistsResult);
        }

        public Task<EventVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult<EventVersion?>(null);

        public Task Create(EventDefinition eventDefinition, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task AddVersion(Guid eventId, EventVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateVersionStatus(Guid versionId, ContractVersionStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Delete(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeCommandRepository : ICommandRepository
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

    private sealed class FakeSchemaTypeRepository : ISchemaTypeRepository
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

    private sealed class FakeContractFieldMetadataRepository : IContractFieldMetadataRepository
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
