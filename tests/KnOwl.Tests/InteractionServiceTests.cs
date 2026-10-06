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
        EventVersion version = new() { Id = Guid.NewGuid(), EventDefinitionId = eventId, VersionNumber = "1.0.0", Status = ContractVersionStatus.Draft };
        FakeEventRepository repository = new() { Entity = expected, Version = version, VersionExistsResult = true };

        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IEventRepository>(repository)
            .AddKnOwlControlPlaneApplication()
            .BuildServiceProvider();

        var service = provider.GetRequiredService<IEventInteractionService>();
        var all = await service.GetAll();
        var result = await service.GetById(eventId, includeVersions: true);
        var versionResult = await service.GetVersionById(version.Id);
        var versionExists = await service.VersionExists(eventId, "1.0.0");
        await service.Create(expected);
        await service.UpdateDefinition(eventId, "Updated", "updated", "Description", DateTime.UtcNow);
        await service.AddVersion(eventId, version);
        await service.UpdateDraftVersion(version.Id, "{}", "Comment", DateTime.UtcNow);
        await service.Delete(eventId);

        Assert.Single(all);
        Assert.Same(expected, result);
        Assert.Same(version, versionResult);
        Assert.True(versionExists);
        Assert.Equal(eventId, repository.LastGetById);
        Assert.True(repository.LastIncludeVersions);
        Assert.Equal((eventId, "1.0.0"), repository.LastVersionExists);
        Assert.True(repository.CreateCalled);
        Assert.True(repository.UpdateDefinitionCalled);
        Assert.True(repository.AddVersionCalled);
        Assert.True(repository.UpdateDraftCalled);
        Assert.True(repository.DeleteCalled);
    }

    [Fact]
    public async Task EventServiceRejectsMissingAndNonDraftVersionUpdates()
    {
        using var missingProvider = new ServiceCollection()
            .AddSingleton<IEventRepository>(new FakeEventRepository())
            .AddKnOwlControlPlaneApplication()
            .BuildServiceProvider();
        var missingService = missingProvider.GetRequiredService<IEventInteractionService>();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            missingService.UpdateDraftVersion(Guid.NewGuid(), "{}", null, DateTime.UtcNow));

        using var deployedProvider = new ServiceCollection()
            .AddSingleton<IEventRepository>(new FakeEventRepository
            {
                Version = new EventVersion { Id = Guid.NewGuid(), Status = ContractVersionStatus.Deployed }
            })
            .AddKnOwlControlPlaneApplication()
            .BuildServiceProvider();
        var deployedService = deployedProvider.GetRequiredService<IEventInteractionService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            deployedService.UpdateDraftVersion(Guid.NewGuid(), "{}", null, DateTime.UtcNow));
    }

    [Fact]
    public async Task CommandServiceDelegatesAndValidatesDraftUpdates()
    {
        var commandId = Guid.NewGuid();
        CommandDefinition command = new() { Id = commandId, Name = "Create Customer", Topic = "customer.create" };
        CommandVersion draft = new() { Id = Guid.NewGuid(), CommandDefinitionId = commandId, VersionNumber = "1.0.0", Status = ContractVersionStatus.Draft };
        FakeCommandRepository repository = new() { Entity = command, Version = draft, VersionExistsResult = true };

        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<ICommandRepository>(repository)
            .AddKnOwlControlPlaneApplication()
            .BuildServiceProvider();

        var service = provider.GetRequiredService<ICommandInteractionService>();
        var all = await service.GetAll();
        var byId = await service.GetById(commandId, includeVersions: true);
        var byVersion = await service.GetVersionById(draft.Id);
        var exists = await service.VersionExists(commandId, "1.0.0");
        await service.Create(command);
        await service.UpdateDefinition(commandId, "Updated", "updated", "Description", DateTime.UtcNow);
        await service.AddVersion(commandId, draft);
        await service.UpdateDraftVersion(draft.Id, "{}", null, "Comment", DateTime.UtcNow);
        await service.Delete(commandId);

        Assert.Single(all);
        Assert.Same(command, byId);
        Assert.Same(draft, byVersion);
        Assert.True(exists);
        Assert.True(repository.CreateCalled);
        Assert.True(repository.UpdateDefinitionCalled);
        Assert.True(repository.AddVersionCalled);
        Assert.True(repository.UpdateDraftCalled);
        Assert.True(repository.DeleteCalled);

        repository.Version = null;
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateDraftVersion(Guid.NewGuid(), "{}", null, null, DateTime.UtcNow));

        repository.Version = new CommandVersion { Id = Guid.NewGuid(), Status = ContractVersionStatus.Deployed };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateDraftVersion(repository.Version.Id, "{}", null, null, DateTime.UtcNow));
    }

    [Fact]
    public async Task SchemaTypeAndMetadataServicesDelegateAllOperations()
    {
        SchemaTypeDefinition schema = new() { Id = Guid.NewGuid(), Key = "customer", Name = "Customer" };
        SchemaTypeVersion schemaVersion = new() { Id = Guid.NewGuid(), SchemaTypeDefinitionId = schema.Id, VersionNumber = "1.0.0" };
        ContractFieldMetadataDefinition metadata = new() { Id = Guid.NewGuid(), Key = "trace", Name = "Trace" };
        ContractFieldMetadataVersion metadataVersion = new() { Id = Guid.NewGuid(), ContractFieldMetadataDefinitionId = metadata.Id, VersionNumber = "1.0.0" };
        FakeSchemaTypeRepository schemaRepository = new()
        {
            Entity = schema,
            Version = schemaVersion,
            KeyExistsResult = true,
            VersionExistsResult = true
        };
        FakeContractFieldMetadataRepository metadataRepository = new()
        {
            Entity = metadata,
            VersionExistsResult = true,
            KeyExistsResult = true
        };

        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<ISchemaTypeRepository>(schemaRepository)
            .AddSingleton<IContractFieldMetadataRepository>(metadataRepository)
            .AddSingleton<IEventRepository>(new FakeEventRepository())
            .AddSingleton<ICommandRepository>(new FakeCommandRepository())
            .AddKnOwlControlPlaneApplication()
            .BuildServiceProvider();

        var schemaService = provider.GetRequiredService<ISchemaTypeInteractionService>();
        Assert.Single(await schemaService.GetAll());
        Assert.Single(await schemaService.GetActiveVersions());
        Assert.Same(schema, await schemaService.GetById(schema.Id, includeVersions: true));
        Assert.Same(schemaVersion, await schemaService.GetVersionById(schemaVersion.Id));
        Assert.True(await schemaService.KeyExists("customer"));
        Assert.True(await schemaService.VersionExists(schema.Id, "1.0.0"));
        await schemaService.Create(schema);
        await schemaService.UpdateDefinition(schema.Id, schema.Key, schema.Name, null, true, DateTime.UtcNow);
        await schemaService.AddVersion(schema.Id, schemaVersion);
        await schemaService.SetVersionActive(schema.Id, schemaVersion.Id, true, DateTime.UtcNow);

        var metadataService = provider.GetRequiredService<IContractFieldMetadataInteractionService>();
        Assert.Single(await metadataService.GetAll());
        Assert.Single(await metadataService.GetActive());
        Assert.Same(metadata, await metadataService.GetById(metadata.Id, includeVersions: true));
        Assert.True(await metadataService.KeyExists("trace"));
        Assert.True(await metadataService.VersionExists(metadata.Id, "1.0.0"));
        await metadataService.Create(metadata);
        await metadataService.UpdateDefinition(metadata.Id, metadata.Key, metadata.Name, null, true, DateTime.UtcNow);
        await metadataService.UpsertVersion(metadata.Id, metadataVersion);
        await metadataService.SetVersionActive(metadata.Id, metadataVersion.Id, true, DateTime.UtcNow);

        Assert.True(schemaRepository.CreateCalled);
        Assert.True(schemaRepository.UpdateDefinitionCalled);
        Assert.True(schemaRepository.AddVersionCalled);
        Assert.True(schemaRepository.SetVersionActiveCalled);
        Assert.True(metadataRepository.CreateCalled);
        Assert.True(metadataRepository.UpdateDefinitionCalled);
        Assert.True(metadataRepository.UpsertVersionCalled);
        Assert.True(metadataRepository.SetVersionActiveCalled);
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
        public EventVersion? Version { get; init; }
        public bool VersionExistsResult { get; init; }
        public bool CreateCalled { get; private set; }
        public bool UpdateDefinitionCalled { get; private set; }
        public bool AddVersionCalled { get; private set; }
        public bool UpdateDraftCalled { get; private set; }
        public bool DeleteCalled { get; private set; }
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
            => Task.FromResult(Version);

        public Task Create(EventDefinition eventDefinition, CancellationToken cancellationToken = default)
        {
            CreateCalled = true;
            return Task.CompletedTask;
        }

        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            UpdateDefinitionCalled = true;
            return Task.CompletedTask;
        }

        public Task AddVersion(Guid eventId, EventVersion version, CancellationToken cancellationToken = default)
        {
            AddVersionCalled = true;
            return Task.CompletedTask;
        }

        public Task UpdateDraftVersion(Guid versionId, string payloadSchemaJson, string? comment, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            UpdateDraftCalled = true;
            return Task.CompletedTask;
        }

        public Task UpdateVersionStatus(Guid versionId, ContractVersionStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Delete(Guid id, CancellationToken cancellationToken = default)
        {
            DeleteCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCommandRepository : ICommandRepository
    {
        public CommandDefinition? Entity { get; init; }
        public CommandVersion? Version { get; set; }
        public bool VersionExistsResult { get; init; }
        public bool CreateCalled { get; private set; }
        public bool UpdateDefinitionCalled { get; private set; }
        public bool AddVersionCalled { get; private set; }
        public bool UpdateDraftCalled { get; private set; }
        public bool DeleteCalled { get; private set; }

        public Task<IReadOnlyList<CommandDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CommandDefinition>>(Entity is null ? [] : [Entity]);
        public Task<CommandDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult(Entity);
        public Task<bool> VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(VersionExistsResult);
        public Task<CommandVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(Version);
        public Task Create(CommandDefinition commandDefinition, CancellationToken cancellationToken = default)
        {
            CreateCalled = true;
            return Task.CompletedTask;
        }

        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            UpdateDefinitionCalled = true;
            return Task.CompletedTask;
        }

        public Task AddVersion(Guid commandId, CommandVersion version, CancellationToken cancellationToken = default)
        {
            AddVersionCalled = true;
            return Task.CompletedTask;
        }

        public Task UpdateDraftVersion(Guid versionId, string requestSchemaJson, string? replySchemaJson, string? comment, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            UpdateDraftCalled = true;
            return Task.CompletedTask;
        }

        public Task UpdateVersionStatus(Guid versionId, ContractVersionStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Delete(Guid id, CancellationToken cancellationToken = default)
        {
            DeleteCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSchemaTypeRepository : ISchemaTypeRepository
    {
        public SchemaTypeDefinition? Entity { get; init; }
        public SchemaTypeVersion? Version { get; init; }
        public bool KeyExistsResult { get; init; }
        public bool VersionExistsResult { get; init; }
        public bool CreateCalled { get; private set; }
        public bool UpdateDefinitionCalled { get; private set; }
        public bool AddVersionCalled { get; private set; }
        public bool SetVersionActiveCalled { get; private set; }

        public Task<IReadOnlyList<SchemaTypeDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SchemaTypeDefinition>>(Entity is null ? [] : [Entity]);
        public Task<IReadOnlyList<SchemaTypeVersion>> GetActiveVersionsWithDefinitions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SchemaTypeVersion>>(Version is null ? [] : [Version]);
        public Task<SchemaTypeDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult(Entity);
        public Task<SchemaTypeVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(Version);
        public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(KeyExistsResult);
        public Task<bool> VersionExists(Guid typeId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(VersionExistsResult);
        public Task Create(SchemaTypeDefinition schemaType, CancellationToken cancellationToken = default)
        {
            CreateCalled = true;
            return Task.CompletedTask;
        }

        public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            UpdateDefinitionCalled = true;
            return Task.CompletedTask;
        }

        public Task AddVersion(Guid typeId, SchemaTypeVersion version, CancellationToken cancellationToken = default)
        {
            AddVersionCalled = true;
            return Task.CompletedTask;
        }

        public Task SetVersionActive(Guid typeId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            SetVersionActiveCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeContractFieldMetadataRepository : IContractFieldMetadataRepository
    {
        public ContractFieldMetadataDefinition? Entity { get; init; }
        public bool KeyExistsResult { get; init; }
        public bool VersionExistsResult { get; init; }
        public bool CreateCalled { get; private set; }
        public bool UpdateDefinitionCalled { get; private set; }
        public bool UpsertVersionCalled { get; private set; }
        public bool SetVersionActiveCalled { get; private set; }

        public Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractFieldMetadataDefinition>>(Entity is null ? [] : [Entity]);
        public Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetActiveWithVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractFieldMetadataDefinition>>(Entity is null ? [] : [Entity]);
        public Task<ContractFieldMetadataDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult(Entity);
        public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(KeyExistsResult);
        public Task<bool> VersionExists(Guid metadataFieldId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(VersionExistsResult);
        public Task Create(ContractFieldMetadataDefinition metadataField, CancellationToken cancellationToken = default)
        {
            CreateCalled = true;
            return Task.CompletedTask;
        }

        public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            UpdateDefinitionCalled = true;
            return Task.CompletedTask;
        }

        public Task UpsertVersion(Guid metadataFieldId, ContractFieldMetadataVersion version, CancellationToken cancellationToken = default)
        {
            UpsertVersionCalled = true;
            return Task.CompletedTask;
        }

        public Task SetVersionActive(Guid metadataFieldId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            SetVersionActiveCalled = true;
            return Task.CompletedTask;
        }
    }
}
