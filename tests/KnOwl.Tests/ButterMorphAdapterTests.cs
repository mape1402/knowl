using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.WebUI.ButterMorph;
using KnOwl.ControlPlane.Design.Core;
using ButterMorph.SchemaDesign;
using KnOwlSchemaTypeDefinition = KnOwl.ControlPlane.Design.Core.SchemaTypeDefinition;
using ButterMorphSchemaTypeDefinition = ButterMorph.SchemaDesign.SchemaTypeDefinition;

namespace KnOwl.Tests;

public sealed class ButterMorphAdapterTests
{
    [Fact]
    public void ContextKeysRoundTripGuidIdentifiers()
    {
        var id = Guid.NewGuid();
        var eventVersionKey = KnOwlButterMorphContext.EventVersion(id);
        var commandVersionKey = KnOwlButterMorphContext.CommandVersion(id);

        Assert.True(KnOwlButterMorphContext.TryReadGuid(eventVersionKey, "event-version:new:", out var eventId));
        Assert.True(KnOwlButterMorphContext.TryReadGuid(commandVersionKey, "command-version:new:", out var commandId));
        Assert.Equal(id, eventId);
        Assert.Equal(id, commandId);
        Assert.False(KnOwlButterMorphContext.TryReadGuid(eventVersionKey, "command-version:new:", out _));
    }

    [Fact]
    public void DraftStoreReturnsEmptyValuesForUnknownContext()
    {
        KnOwlButterMorphDraftStore store = new();

        Assert.Equal(string.Empty, store.GetPayloadSchema("missing"));
        Assert.Null(store.GetCreatedEvent("missing"));
        Assert.Null(store.GetCreatedCommand("missing"));
    }

    [Fact]
    public void DraftStoreReturnsSavedValuesByContext()
    {
        KnOwlButterMorphDraftStore store = new();
        var eventId = Guid.NewGuid();
        var commandId = Guid.NewGuid();

        store.SavePayloadSchema("ctx", "{\"type\":\"object\"}");
        store.SaveCreatedEvent("ctx", eventId);
        store.SaveCreatedCommand("ctx", commandId);

        Assert.Equal("{\"type\":\"object\"}", store.GetPayloadSchema("ctx"));
        Assert.Equal(eventId, store.GetCreatedEvent("ctx"));
        Assert.Equal(commandId, store.GetCreatedCommand("ctx"));
    }

    [Fact]
    public void MapperNormalizesKeysForCatalogIdentifiers()
    {
        Assert.Equal("customer-registered", KnOwlButterMorphDefinitionMapper.NormalizeKey(" Customer Registered! "));
        Assert.Equal("schema", KnOwlButterMorphDefinitionMapper.NormalizeKey("---"));
    }

    [Fact]
    public void MapperConvertsSchemaTypeVersionToCatalogItem()
    {
        ButterMorphSchemaTypeDefinition definition = new()
        {
            Key = "EmailAddress",
            Name = "Email Address",
            Description = "Customer email",
            Version = "1.2.0",
            BaseType = "string",
            Schema = KnOwlButterMorphDefinitionMapper.ParseElement("{\"type\":\"string\",\"pattern\":\"@\"}"),
            JsonSchema = "{\"type\":\"string\",\"pattern\":\"@\"}"
        };

        KnOwlSchemaTypeDefinition entity = new()
        {
            Key = "EmailAddress",
            Name = "Email Address",
            IsSystem = false
        };
        SchemaTypeVersion version = new()
        {
            VersionNumber = "1.2.0",
            DefinitionJson = KnOwlButterMorphDefinitionMapper.SerializeSchemaTypeDefinition(definition)
        };

        var item = KnOwlButterMorphDefinitionMapper.ToCatalogItem(entity, version, isSystem: false);

        Assert.Equal("EmailAddress", item.TypeId);
        Assert.Equal("EmailAddress@1.2.0", item.TypeVersionId);
        Assert.Equal("string", item.BaseType);
        Assert.Contains("pattern", item.JsonSchema, StringComparison.Ordinal);
        Assert.False(item.IsSystem);
    }

    [Fact]
    public void MapperConvertsMetadataScopesBetweenKnOwlAndButterMorph()
    {
        var knowlScopes = KnOwlButterMorphDefinitionMapper.ToKnOwlScopes(["Field"]);
        var butterMorphScopes = KnOwlButterMorphDefinitionMapper.ToButterMorphScopes("[\"events\"]");

        Assert.Contains("events", knowlScopes);
        Assert.Contains("commands", knowlScopes);
        Assert.Contains("Schema", butterMorphScopes);
        Assert.Contains("Field", butterMorphScopes);
    }

    [Fact]
    public void MapperNormalizesNestedJsonStringsInCustomFieldDefinition()
    {
        CustomFieldDefinition definition = new()
        {
            Key = "retentionDays",
            Name = "Retention Days",
            Description = "Retention policy",
            Version = "1.0.0",
            DataType = "integer",
            IsActive = true,
            Validation = new Dictionary<string, JsonElement>
            {
                ["minimum"] = KnOwlButterMorphDefinitionMapper.ParseElement("1")
            }
        };

        var json = KnOwlButterMorphDefinitionMapper.SerializeCustomFieldDefinition(definition);

        Assert.Contains("\"validation\":{", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\\\"minimum\\\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PayloadSchemaHostAddsOptionalTopicMetadataForCommandSchemas()
    {
        var host = new KnOwlPayloadSchemaDesignerHost(
            new EventInteractionStub(),
            new CommandInteractionStub(),
            new SchemaTypeInteractionStub(),
            new MetadataInteractionStub(),
            new KnOwlButterMorphDraftStore());

        var result = await host.Load(new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerLoadRequest
        {
            ContextKey = KnOwlButterMorphContext.CommandNew()
        });

        var topic = Assert.Single(result.MetadataFields, x => x.Key == "topic");
        Assert.Equal("Topic", topic.Name);
        Assert.False(topic.IsRequired);
        Assert.Equal("[\"Schema\"]", topic.AppliesToJson);
        Assert.Contains("maxLength", topic.Validation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PayloadSchemaHostDoesNotDuplicateExistingTopicMetadata()
    {
        var existingTopic = new ContractFieldMetadataDefinition
        {
            Id = Guid.NewGuid(),
            Key = "topic",
            Name = "Existing Topic",
            IsActive = true,
            Versions =
            [
                new ContractFieldMetadataVersion
                {
                    Id = Guid.NewGuid(),
                    VersionNumber = "1.0.0",
                    IsActive = true,
                    DefinitionJson = KnOwlButterMorphDefinitionMapper.SerializeCustomFieldDefinition(new CustomFieldDefinition
                    {
                        Key = "topic",
                        Name = "Existing Topic",
                        Version = "1.0.0",
                        DataType = "string",
                        AppliesTo = ["Schema"],
                        IsRequired = true
                    })
                }
            ]
        };
        var host = new KnOwlPayloadSchemaDesignerHost(
            new EventInteractionStub(),
            new CommandInteractionStub(),
            new SchemaTypeInteractionStub(),
            new MetadataInteractionStub([existingTopic]),
            new KnOwlButterMorphDraftStore());

        var result = await host.Load(new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerLoadRequest
        {
            ContextKey = KnOwlButterMorphContext.CommandNew()
        });

        var topic = Assert.Single(result.MetadataFields, x => x.Key == "topic");
        Assert.Equal("Existing Topic", topic.Name);
    }

    [Fact]
    public async Task PayloadSchemaHostDoesNotAddTopicMetadataForCommandReplySchemas()
    {
        var command = new CommandDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Customer Register",
            Topic = "customer.register"
        };
        var host = new KnOwlPayloadSchemaDesignerHost(
            new EventInteractionStub(),
            new CommandInteractionStub { Entity = command },
            new SchemaTypeInteractionStub(),
            new MetadataInteractionStub(),
            new KnOwlButterMorphDraftStore());

        var result = await host.Load(new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerLoadRequest
        {
            ContextKey = KnOwlButterMorphContext.CommandVersionReplyDraft(command.Id)
        });

        Assert.DoesNotContain(result.MetadataFields, x => x.Key == "topic");
    }

    [Fact]
    public async Task PayloadSchemaHostCreatesCommandUsingSchemaKeyWhenTopicMetadataIsMissing()
    {
        var commands = new CommandInteractionStub();
        var host = new KnOwlPayloadSchemaDesignerHost(
            new EventInteractionStub(),
            commands,
            new SchemaTypeInteractionStub(),
            new MetadataInteractionStub(),
            new KnOwlButterMorphDraftStore());

        var result = await host.Save(new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerSaveRequest
        {
            ContextKey = KnOwlButterMorphContext.CommandNew(),
            Definition = new PayloadSchemaDefinition
            {
                Key = "customer.register",
                Name = "Customer Register",
                Version = "1.0.0",
                Type = "object"
            }
        });

        Assert.True(result.Succeeded);
        var command = Assert.Single(commands.Created);
        Assert.Equal("customer.register", command.Topic);
    }

    private sealed class EventInteractionStub : IEventInteractionService
    {
        public Task<IReadOnlyList<EventDefinition>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<EventDefinition>>([]);
        public Task<EventDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult<EventDefinition?>(null);
        public Task<bool> VersionExists(Guid eventId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task Create(EventDefinition eventDefinition, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid eventId, EventVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Delete(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class CommandInteractionStub : ICommandInteractionService
    {
        public CommandDefinition? Entity { get; init; }
        public List<CommandDefinition> Created { get; } = [];

        public Task<IReadOnlyList<CommandDefinition>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CommandDefinition>>([]);
        public Task<CommandDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult(Entity?.Id == id ? Entity : null);
        public Task<bool> VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task Create(CommandDefinition commandDefinition, CancellationToken cancellationToken = default)
        {
            Created.Add(commandDefinition);
            return Task.CompletedTask;
        }
        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid commandId, CommandVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Delete(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class SchemaTypeInteractionStub : ISchemaTypeInteractionService
    {
        public Task<IReadOnlyList<KnOwlSchemaTypeDefinition>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<KnOwlSchemaTypeDefinition>>([]);
        public Task<IReadOnlyList<SchemaTypeVersion>> GetActiveVersions(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SchemaTypeVersion>>([]);
        public Task<KnOwlSchemaTypeDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult<KnOwlSchemaTypeDefinition?>(null);
        public Task<SchemaTypeVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult<SchemaTypeVersion?>(null);
        public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> VersionExists(Guid typeId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task Create(KnOwlSchemaTypeDefinition schemaType, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid typeId, SchemaTypeVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetVersionActive(Guid typeId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MetadataInteractionStub(IReadOnlyList<ContractFieldMetadataDefinition>? active = null) : IContractFieldMetadataInteractionService
    {
        private readonly IReadOnlyList<ContractFieldMetadataDefinition> _active = active ?? [];

        public Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractFieldMetadataDefinition>>([]);
        public Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetActive(CancellationToken cancellationToken = default) => Task.FromResult(_active);
        public Task<ContractFieldMetadataDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult<ContractFieldMetadataDefinition?>(null);
        public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> VersionExists(Guid metadataFieldId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task Create(ContractFieldMetadataDefinition metadataField, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpsertVersion(Guid metadataFieldId, ContractFieldMetadataVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetVersionActive(Guid metadataFieldId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }}





