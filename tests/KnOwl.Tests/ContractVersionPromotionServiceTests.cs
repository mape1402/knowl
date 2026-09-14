using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Tests;

public sealed class ContractVersionPromotionServiceTests
{
    [Fact]
    public void TransitionPolicyAllowsOrchestratorLifecycleFlow()
    {
        using var provider = CreateProvider(new PromotionEventRepository(), new PromotionCommandRepository());
        var policy = provider.GetRequiredService<IContractVersionTransitionPolicy>();

        Assert.True(policy.CanTransition(ContractVersionStatus.Draft, ContractVersionStatus.InReview));
        Assert.True(policy.CanTransition(ContractVersionStatus.InReview, ContractVersionStatus.Approved));
        Assert.True(policy.CanTransition(ContractVersionStatus.Approved, ContractVersionStatus.Deployed));
        Assert.True(policy.CanTransition(ContractVersionStatus.Deployed, ContractVersionStatus.Deprecated));
        Assert.True(policy.CanTransition(ContractVersionStatus.Deprecated, ContractVersionStatus.Archived));
    }

    [Fact]
    public async Task TransitionEventVersionPersistsAllowedTarget()
    {
        var versionId = Guid.NewGuid();
        PromotionEventRepository events = new()
        {
            Version = new EventVersion { Id = versionId, Status = ContractVersionStatus.Draft }
        };

        using var provider = CreateProvider(events, new PromotionCommandRepository());
        var service = provider.GetRequiredService<IContractVersionPromotionService>();

        await service.TransitionEventVersion(versionId, ContractVersionStatus.InReview);

        Assert.Equal((versionId, ContractVersionStatus.InReview), events.LastStatusUpdate);
    }

    [Fact]
    public async Task TransitionCommandVersionBlocksInvalidTarget()
    {
        var versionId = Guid.NewGuid();
        PromotionCommandRepository commands = new()
        {
            Version = new CommandVersion { Id = versionId, Status = ContractVersionStatus.Draft }
        };

        using var provider = CreateProvider(new PromotionEventRepository(), commands);
        var service = provider.GetRequiredService<IContractVersionPromotionService>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.TransitionCommandVersion(versionId, ContractVersionStatus.Deployed));

        Assert.Null(commands.LastStatusUpdate);
    }

    private static ServiceProvider CreateProvider(IEventRepository events, ICommandRepository commands)
    {
        return new ServiceCollection()
            .AddSingleton(events)
            .AddSingleton(commands)
            .AddSingleton<ISchemaTypeRepository>(new PromotionSchemaTypeRepository())
            .AddSingleton<IContractFieldMetadataRepository>(new PromotionMetadataRepository())
            .AddKnOwlControlPlaneApplication()
            .BuildServiceProvider();
    }

    private sealed class PromotionEventRepository : IEventRepository
    {
        public EventVersion? Version { get; init; }
        public (Guid VersionId, ContractVersionStatus Status)? LastStatusUpdate { get; private set; }

        public Task<IReadOnlyList<EventDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<EventDefinition>>([]);
        public Task<EventDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult<EventDefinition?>(null);
        public Task<bool> VersionExists(Guid eventId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<EventVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(Version);
        public Task Create(EventDefinition eventDefinition, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid eventId, EventVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Delete(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateVersionStatus(Guid versionId, ContractVersionStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default)
        {
            LastStatusUpdate = (versionId, status);
            return Task.CompletedTask;
        }
    }

    private sealed class PromotionCommandRepository : ICommandRepository
    {
        public CommandVersion? Version { get; init; }
        public (Guid VersionId, ContractVersionStatus Status)? LastStatusUpdate { get; private set; }

        public Task<IReadOnlyList<CommandDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CommandDefinition>>([]);
        public Task<CommandDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult<CommandDefinition?>(null);
        public Task<bool> VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<CommandVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(Version);
        public Task Create(CommandDefinition commandDefinition, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid commandId, CommandVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Delete(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateVersionStatus(Guid versionId, ContractVersionStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default)
        {
            LastStatusUpdate = (versionId, status);
            return Task.CompletedTask;
        }
    }

    private sealed class PromotionSchemaTypeRepository : ISchemaTypeRepository
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

    private sealed class PromotionMetadataRepository : IContractFieldMetadataRepository
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
