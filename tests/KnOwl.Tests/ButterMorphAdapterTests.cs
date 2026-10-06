using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.WebUI.ButterMorph;
using KnOwl.ControlPlane.WebUI.ContractMetadata;
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
        var eventEditKey = KnOwlButterMorphContext.EditEventVersion(id);
        var commandVersionKey = KnOwlButterMorphContext.CommandVersion(id);
        var commandRequestEditKey = KnOwlButterMorphContext.EditCommandVersionRequest(id);
        var commandReplyEditKey = KnOwlButterMorphContext.EditCommandVersionReply(id);

        Assert.True(KnOwlButterMorphContext.TryReadGuid(eventVersionKey, "event-version:new:", out var eventId));
        Assert.True(KnOwlButterMorphContext.TryReadGuid(eventEditKey, "event-version:edit:", out var eventEditId));
        Assert.True(KnOwlButterMorphContext.TryReadGuid(commandVersionKey, "command-version:new:", out var commandId));
        Assert.True(KnOwlButterMorphContext.TryReadGuid(commandRequestEditKey, "command-version-request:edit:", out var commandRequestEditId));
        Assert.True(KnOwlButterMorphContext.TryReadGuid(commandReplyEditKey, "command-version-reply:edit:", out var commandReplyEditId));
        Assert.Equal(id, eventId);
        Assert.Equal(id, eventEditId);
        Assert.Equal(id, commandId);
        Assert.Equal(id, commandRequestEditId);
        Assert.Equal(id, commandReplyEditId);
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
        var schemaOnlyScopes = KnOwlButterMorphDefinitionMapper.ToButterMorphScopes("[\"Schema\"]");

        Assert.Contains("events", knowlScopes);
        Assert.Contains("commands", knowlScopes);
        Assert.Contains("Schema", butterMorphScopes);
        Assert.DoesNotContain("Field", butterMorphScopes);
        Assert.Equal(["Schema"], schemaOnlyScopes);
    }

    [Fact]
    public void MapperNormalizesRequiredMetadataCatalogIdentityFromPersistedEntity()
    {
        ContractFieldMetadataDefinition entity = new()
        {
            Id = Guid.NewGuid(),
            Key = "sample-pii-classification",
            Name = "Sample PII Classification",
            Description = "Governance classification",
            IsActive = true
        };
        ContractFieldMetadataVersion version = new()
        {
            VersionNumber = "1.1.0",
            Comment = "Adds confidential.",
            IsActive = true,
            DefinitionJson = """
                {
                  "key": "stale-key",
                  "name": "PII",
                  "version": "0.1.0",
                  "versionComment": "",
                  "dataType": "string",
                  "appliesTo": ["Schema", "Field"],
                  "isRequired": true,
                  "isActive": true,
                  "validation": { "enum": ["none", "internal", "confidential"] }
                }
                """
        };

        var item = KnOwlButterMorphDefinitionMapper.ToCatalogItem(entity, version);

        Assert.Equal("sample-pii-classification", item.Key);
        Assert.Equal("1.1.0", item.Version);
        Assert.True(item.IsRequired);
        Assert.Equal("[\"Schema\"]", item.AppliesToJson);
        Assert.DoesNotContain("Field", item.AppliesToJson, StringComparison.Ordinal);
        Assert.Contains("confidential", item.Validation, StringComparison.Ordinal);
    }

    [Fact]
    public void MapperPreservesOptionalDualScopeMetadata()
    {
        ContractFieldMetadataDefinition entity = new()
        {
            Id = Guid.NewGuid(),
            Key = "classification",
            Name = "Classification",
            IsActive = true
        };
        ContractFieldMetadataVersion version = new()
        {
            VersionNumber = "1.0.0",
            IsActive = true,
            DefinitionJson = """
                {
                  "dataType": "string",
                  "appliesTo": ["Schema", "Field"],
                  "isRequired": false,
                  "isActive": true
                }
                """
        };

        var item = KnOwlButterMorphDefinitionMapper.ToCatalogItem(entity, version);

        Assert.Equal("[\"Schema\",\"Field\"]", item.AppliesToJson);
    }

    [Fact]
    public void MapperKeepsSchemaOnlyMetadataOutOfFieldRequiredValidation()
    {
        ContractFieldMetadataDefinition entity = new()
        {
            Id = Guid.NewGuid(),
            Key = "source-system",
            Name = "Source System",
            IsActive = true
        };
        ContractFieldMetadataVersion version = new()
        {
            VersionNumber = "1.0.0",
            IsActive = true,
            DefinitionJson = """
                {
                  "dataType": "string",
                  "appliesTo": ["Schema"],
                  "isRequired": true,
                  "isActive": true
                }
                """
        };

        var item = KnOwlButterMorphDefinitionMapper.ToCatalogItem(entity, version);

        Assert.Equal("[\"Schema\"]", item.AppliesToJson);
        Assert.DoesNotContain("Field", item.AppliesToJson, StringComparison.Ordinal);
    }

    [Fact]
    public void MapperPreservesExplicitFieldMetadataScope()
    {
        ContractFieldMetadataDefinition entity = new()
        {
            Id = Guid.NewGuid(),
            Key = "security-classification",
            Name = "Security Classification",
            IsActive = true
        };
        ContractFieldMetadataVersion version = new()
        {
            VersionNumber = "1.0.0",
            IsActive = true,
            DefinitionJson = """
                {
                  "dataType": "string",
                  "appliesTo": ["Field"],
                  "isRequired": false,
                  "isActive": true
                }
                """
        };

        var item = KnOwlButterMorphDefinitionMapper.ToCatalogItem(entity, version);

        Assert.Equal("[\"Field\"]", item.AppliesToJson);
    }

    [Fact]
    public async Task LegacyMetadataCatalogIncludesExplicitFieldScopedCustomFields()
    {
        ContractFieldMetadataDefinition entity = new()
        {
            Id = Guid.NewGuid(),
            Key = "sample-pii-classification",
            Name = "Sample PII Classification",
            IsActive = true,
            Versions =
            [
                new ContractFieldMetadataVersion
                {
                    VersionNumber = "1.0.0",
                    IsActive = true,
                    DefinitionJson = """
                        {
                          "dataType": "string",
                          "appliesTo": ["Field"],
                          "isRequired": true,
                          "isActive": true
                        }
                        """
                }
            ]
        };

        var json = await ContractFieldMetadataCatalog.GetApplicableMetadataJsonAsync(new MetadataInteractionStub([entity]), "events");

        Assert.Contains("sample-pii-classification", json, StringComparison.Ordinal);
        Assert.Contains("\"isRequired\":true", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LegacyMetadataCatalogDoesNotTreatSchemaOnlyMetadataAsFieldMetadata()
    {
        ContractFieldMetadataDefinition entity = new()
        {
            Id = Guid.NewGuid(),
            Key = "source-system",
            Name = "Source System",
            IsActive = true,
            Versions =
            [
                new ContractFieldMetadataVersion
                {
                    VersionNumber = "1.0.0",
                    IsActive = true,
                    DefinitionJson = """
                        {
                          "dataType": "string",
                          "appliesTo": ["Schema"],
                          "isRequired": true,
                          "isActive": true
                        }
                        """
                }
            ]
        };

        var json = await ContractFieldMetadataCatalog.GetApplicableMetadataJsonAsync(new MetadataInteractionStub([entity]), "events");

        Assert.DoesNotContain("source-system", json, StringComparison.Ordinal);
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
    public async Task PayloadSchemaHostAddsOptionalTopicMetadataForNewCommandRequestSchemas()
    {
        var host = new KnOwlPayloadSchemaDesignerHost(
            new EventInteractionStub(),
            new CommandInteractionStub(),
            new SchemaTypeInteractionStub(),
            new MetadataInteractionStub(),
            new KnOwlButterMorphDraftStore());

        var result = await host.Load(new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerLoadRequest
        {
            ContextKey = KnOwlButterMorphContext.CommandCreateRequestDraft(Guid.NewGuid())
        });

        Assert.Contains(result.MetadataFields, x => x.Key == "topic");
    }

    [Fact]
    public async Task PayloadSchemaHostDoesNotAddTopicMetadataForNewCommandReplySchemas()
    {
        var host = new KnOwlPayloadSchemaDesignerHost(
            new EventInteractionStub(),
            new CommandInteractionStub(),
            new SchemaTypeInteractionStub(),
            new MetadataInteractionStub(),
            new KnOwlButterMorphDraftStore());

        var result = await host.Load(new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerLoadRequest
        {
            ContextKey = KnOwlButterMorphContext.CommandCreateReplyDraft(Guid.NewGuid())
        });

        Assert.DoesNotContain(result.MetadataFields, x => x.Key == "topic");
    }

    [Fact]
    public async Task PayloadSchemaHostInjectsRequiredCustomFieldsWithNormalizedKeys()
    {
        ContractFieldMetadataDefinition entity = new()
        {
            Id = Guid.NewGuid(),
            Key = "sample-pii-classification",
            Name = "Sample PII Classification",
            IsActive = true,
            Versions =
            [
                new ContractFieldMetadataVersion
                {
                    VersionNumber = "1.1.0",
                    IsActive = true,
                    DefinitionJson = """
                        {
                          "key": "old-pii-key",
                          "version": "0.9.0",
                          "dataType": "string",
                          "appliesTo": ["events"],
                          "isRequired": true,
                          "isActive": true,
                          "validation": { "enum": ["none", "internal", "confidential"] }
                        }
                        """
                }
            ]
        };
        var host = new KnOwlPayloadSchemaDesignerHost(
            new EventInteractionStub(),
            new CommandInteractionStub(),
            new SchemaTypeInteractionStub(),
            new MetadataInteractionStub([entity]),
            new KnOwlButterMorphDraftStore());

        var result = await host.Load(new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerLoadRequest
        {
            ContextKey = KnOwlButterMorphContext.CommandCreateReplyDraft(Guid.NewGuid())
        });

        var field = Assert.Single(result.MetadataFields);
        Assert.Equal("sample-pii-classification", field.Key);
        Assert.Equal("1.1.0", field.Version);
        Assert.True(field.IsRequired);
        Assert.Equal("[\"Schema\"]", field.AppliesToJson);
    }

    [Fact]
    public async Task PayloadSchemaHostIncludesTemporalSystemTypesForDesigner()
    {
        var host = new KnOwlPayloadSchemaDesignerHost(
            new EventInteractionStub(),
            new CommandInteractionStub(),
            new SchemaTypeInteractionStub(),
            new MetadataInteractionStub(),
            new KnOwlButterMorphDraftStore());

        var result = await host.Load(new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerLoadRequest
        {
            ContextKey = KnOwlButterMorphContext.CommandCreateRequestDraft(Guid.NewGuid())
        });

        Assert.Contains(result.SchemaTypes, x => x.Name == "Date");
        Assert.Contains(result.SchemaTypes, x => x.Name == "DateTime");
        Assert.Contains(result.SchemaTypes, x => x.Name == "Time");
        Assert.Contains(result.SchemaTypes, x => x.Name == "TimeSpan");
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

    [Fact]
    public async Task PayloadSchemaHostUpdatesDraftEventVersionFromEditContext()
    {
        var versionId = Guid.NewGuid();
        var events = new EventInteractionStub
        {
            Version = new EventVersion
            {
                Id = versionId,
                Status = ContractVersionStatus.Draft,
                PayloadSchemaJson = "{\"key\":\"customer.created\",\"name\":\"Customer Created\",\"version\":\"1.0.0\",\"type\":\"object\",\"properties\":{}}",
                EventDefinition = new EventDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = "Customer Created",
                    Topic = "customer.created"
                }
            }
        };
        var host = new KnOwlPayloadSchemaDesignerHost(
            events,
            new CommandInteractionStub(),
            new SchemaTypeInteractionStub(),
            new MetadataInteractionStub(),
            new KnOwlButterMorphDraftStore());

        var result = await host.Save(new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerSaveRequest
        {
            ContextKey = KnOwlButterMorphContext.EditEventVersion(versionId),
            Definition = new PayloadSchemaDefinition
            {
                Key = "customer.updated",
                Name = "Customer Updated",
                Version = "1.0.0",
                VersionComment = "Adjusted draft",
                Type = "object"
            }
        });

        Assert.True(result.Succeeded);
        Assert.Equal(versionId, events.UpdatedVersionId);
        Assert.Contains("customer.updated", events.UpdatedPayloadSchemaJson, StringComparison.Ordinal);
        Assert.Equal("Adjusted draft", events.UpdatedComment);
    }

    [Fact]
    public async Task PayloadSchemaHostBlocksCommandEditWhenVersionIsNotDraft()
    {
        var versionId = Guid.NewGuid();
        var commands = new CommandInteractionStub
        {
            Version = new CommandVersion
            {
                Id = versionId,
                Status = ContractVersionStatus.Approved,
                PayloadSchemaJson = "{\"key\":\"customer.reserve\",\"name\":\"Reserve Customer\",\"version\":\"1.0.0\",\"type\":\"object\",\"properties\":{}}",
                ReplyPayloadSchemaJson = "{\"key\":\"customer.reserve.reply\",\"name\":\"Reserve Customer Reply\",\"version\":\"1.0.0\",\"type\":\"object\",\"properties\":{}}",
                CommandDefinition = new CommandDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = "Reserve Customer",
                    Topic = "customer.reserve"
                }
            }
        };
        var host = new KnOwlPayloadSchemaDesignerHost(
            new EventInteractionStub(),
            commands,
            new SchemaTypeInteractionStub(),
            new MetadataInteractionStub(),
            new KnOwlButterMorphDraftStore());

        var result = await host.Save(new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerSaveRequest
        {
            ContextKey = KnOwlButterMorphContext.EditCommandVersionRequest(versionId),
            Definition = new PayloadSchemaDefinition
            {
                Key = "customer.reserve",
                Name = "Reserve Customer",
                Version = "1.0.0",
                Type = "object"
            }
        });

        Assert.False(result.Succeeded);
        Assert.Contains("Only draft command versions can be edited", result.Message, StringComparison.Ordinal);
        Assert.Null(commands.UpdatedVersionId);
    }

    [Fact]
    public async Task PayloadSchemaHostLoadsExistingEventAndCommandVersions()
    {
        var eventId = Guid.NewGuid();
        var eventVersionId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        var commandVersionId = Guid.NewGuid();
        var payloadDefinition = new PayloadSchemaDefinition
        {
            Key = "customer.created",
            Name = "Customer Created",
            Description = "Created",
            Version = "1.0.0",
            Type = "object"
        };
        var payloadJson = KnOwlButterMorphDefinitionMapper.GetSchemaJson(payloadDefinition);
        var events = new EventInteractionStub
        {
            Entity = new EventDefinition
            {
                Id = eventId,
                Name = "Customer Created",
                Topic = "customer.created",
                Description = "Created event",
                Versions =
                [
                    new EventVersion
                    {
                        Id = eventVersionId,
                        VersionNumber = "1.0.0",
                        PayloadSchemaJson = payloadJson,
                        EventDefinition = new EventDefinition { Id = eventId, Name = "Customer Created", Topic = "customer.created" }
                    }
                ]
            },
            Version = new EventVersion
            {
                Id = eventVersionId,
                VersionNumber = "1.0.0",
                Comment = "Draft edit",
                PayloadSchemaJson = payloadJson,
                EventDefinition = new EventDefinition { Id = eventId, Name = "Customer Created", Topic = "customer.created" }
            }
        };
        var commands = new CommandInteractionStub
        {
            Entity = new CommandDefinition
            {
                Id = commandId,
                Name = "Create Customer",
                Topic = "customer.create",
                Description = "Create command",
                Versions =
                [
                    new CommandVersion
                    {
                        Id = commandVersionId,
                        VersionNumber = "2.3.4",
                        PayloadSchemaJson = payloadJson,
                        ReplyPayloadSchemaJson = payloadJson,
                        CommandDefinition = new CommandDefinition { Id = commandId, Name = "Create Customer", Topic = "customer.create" }
                    }
                ]
            },
            Version = new CommandVersion
            {
                Id = commandVersionId,
                VersionNumber = "2.3.4",
                Comment = "Command edit",
                PayloadSchemaJson = payloadJson,
                ReplyPayloadSchemaJson = payloadJson,
                CommandDefinition = new CommandDefinition { Id = commandId, Name = "Create Customer", Topic = "customer.create" }
            }
        };
        var host = new KnOwlPayloadSchemaDesignerHost(
            events,
            commands,
            new SchemaTypeInteractionStub(),
            new MetadataInteractionStub(),
            new KnOwlButterMorphDraftStore());

        var newEvent = await host.Load(new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerLoadRequest { ContextKey = KnOwlButterMorphContext.EventVersion(eventId) });
        var editEvent = await host.Load(new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerLoadRequest { ContextKey = KnOwlButterMorphContext.EditEventVersion(eventVersionId) });
        var newCommand = await host.Load(new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerLoadRequest { ContextKey = KnOwlButterMorphContext.CommandVersion(commandId) });
        var editCommandRequest = await host.Load(new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerLoadRequest { ContextKey = KnOwlButterMorphContext.EditCommandVersionRequest(commandVersionId) });
        var newCommandReply = await host.Load(new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerLoadRequest { ContextKey = KnOwlButterMorphContext.CommandVersionReplyDraft(commandId) });
        var editCommandReply = await host.Load(new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerLoadRequest { ContextKey = KnOwlButterMorphContext.EditCommandVersionReply(commandVersionId) });

        Assert.Equal("1.0.1", newEvent.Definition.Version);
        Assert.Equal("Draft edit", editEvent.Definition.VersionComment);
        Assert.Equal("1.0.1", newCommand.Definition.Version);
        Assert.Equal("Command edit", editCommandRequest.Definition.VersionComment);
        Assert.Equal("2.3.5", newCommandReply.Version);
        Assert.Equal("2.3.4", editCommandReply.Definition.Version);
    }

    [Fact]
    public async Task PayloadSchemaHostSavesEventAndCommandVersionBranches()
    {
        var eventId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        var eventVersionId = Guid.NewGuid();
        var commandVersionId = Guid.NewGuid();
        var events = new EventInteractionStub
        {
            Entity = new EventDefinition { Id = eventId, Name = "Customer Created", Topic = "customer.created" },
            Version = new EventVersion { Id = eventVersionId, Status = ContractVersionStatus.Draft }
        };
        var commands = new CommandInteractionStub
        {
            Entity = new CommandDefinition { Id = commandId, Name = "Create Customer", Topic = "customer.create" },
            Version = new CommandVersion
            {
                Id = commandVersionId,
                Status = ContractVersionStatus.Draft,
                PayloadSchemaJson = "{}",
                ReplyPayloadSchemaJson = "{}"
            }
        };
        var store = new KnOwlButterMorphDraftStore();
        var host = new KnOwlPayloadSchemaDesignerHost(
            events,
            commands,
            new SchemaTypeInteractionStub(),
            new MetadataInteractionStub(),
            store);

        var missingEvent = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.EventVersion(Guid.NewGuid()), "1.0.0"));
        events.VersionExistsResult = true;
        var duplicateEvent = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.EventVersion(eventId), "1.0.0"));
        events.VersionExistsResult = false;
        var savedEvent = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.EventVersion(eventId), "1.0.1"));
        var editedEvent = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.EditEventVersion(eventVersionId), "1.0.1", "Edited"));

        var missingCommand = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.CommandVersion(Guid.NewGuid()), "1.0.0"));
        commands.VersionExistsResult = true;
        var duplicateCommand = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.CommandVersion(commandId), "1.0.0"));
        commands.VersionExistsResult = false;
        var savedCommand = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.CommandVersion(commandId), "1.0.1"));
        var editedCommandRequest = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.EditCommandVersionRequest(commandVersionId), "1.0.1", "Request"));
        Assert.True(editedCommandRequest.Succeeded);
        Assert.Contains("customer.created", commands.UpdatedRequestSchemaJson, StringComparison.Ordinal);
        var editedCommandReply = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.EditCommandVersionReply(commandVersionId), "1.0.1", "Reply"));
        var capturedDraft = await host.Save(CreatePayloadSave("scratch-context", "1.0.0"));

        Assert.False(missingEvent.Succeeded);
        Assert.False(duplicateEvent.Succeeded);
        Assert.True(savedEvent.Succeeded);
        Assert.Single(events.AddedVersions);
        Assert.True(editedEvent.Succeeded);
        Assert.Equal("Edited", events.UpdatedComment);
        Assert.False(missingCommand.Succeeded);
        Assert.False(duplicateCommand.Succeeded);
        Assert.True(savedCommand.Succeeded);
        Assert.Single(commands.AddedVersions);
        Assert.True(editedCommandReply.Succeeded);
        Assert.Contains("customer.created", commands.UpdatedReplySchemaJson, StringComparison.Ordinal);
        Assert.True(capturedDraft.Succeeded);
        Assert.Contains("customer.created", store.GetPayloadSchema("scratch-context"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PayloadSchemaHostValidatesNewEventAndCommandRequiredFields()
    {
        var events = new EventInteractionStub();
        var commands = new CommandInteractionStub();
        var host = new KnOwlPayloadSchemaDesignerHost(
            events,
            commands,
            new SchemaTypeInteractionStub(),
            new MetadataInteractionStub(),
            new KnOwlButterMorphDraftStore());

        var eventMissingName = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.EventNew(), "1.0.0", name: string.Empty));
        var eventMissingVersion = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.EventNew(), string.Empty));
        var eventMissingTopic = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.EventNew(), "1.0.0", key: "   "));
        var eventLongTopic = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.EventNew(), "1.0.0", key: new string('a', 71)));
        var eventSaved = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.EventNew(), "1.0.0", topicMetadata: "customer.created"));

        var commandMissingName = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.CommandNew(), "1.0.0", name: string.Empty));
        var commandMissingVersion = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.CommandNew(), string.Empty));
        var commandLongTopic = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.CommandNew(), "1.0.0", key: new string('b', 71)));
        var commandSaved = await host.Save(CreatePayloadSave(KnOwlButterMorphContext.CommandNew(), "1.0.0", topicMetadataObjectValue: "customer.create"));

        Assert.False(eventMissingName.Succeeded);
        Assert.False(eventMissingVersion.Succeeded);
        Assert.False(eventMissingTopic.Succeeded);
        Assert.False(eventLongTopic.Succeeded);
        Assert.True(eventSaved.Succeeded);
        Assert.Single(events.Created);
        Assert.False(commandMissingName.Succeeded);
        Assert.False(commandMissingVersion.Succeeded);
        Assert.False(commandLongTopic.Succeeded);
        Assert.True(commandSaved.Succeeded);
        Assert.Single(commands.Created);
        Assert.Equal("customer.create", commands.Created.Single().Topic);
    }

    [Fact]
    public void MapperHandlesFallbackAndLegacyJsonBranches()
    {
        var schemaEntity = new KnOwlSchemaTypeDefinition
        {
            Key = "legacy",
            Name = "Legacy Type",
            Description = "Legacy",
            IsSystem = false
        };
        var schemaFallback = KnOwlButterMorphDefinitionMapper.ToDefinition(schemaEntity, new SchemaTypeVersion
        {
            VersionNumber = "1.0.0",
            Comment = "Legacy",
            DefinitionJson = "{not-json"
        });
        var metadataEntity = new ContractFieldMetadataDefinition
        {
            Key = "metadata",
            Name = "Metadata",
            Description = "Metadata",
            IsActive = true
        };
        var metadataFallback = KnOwlButterMorphDefinitionMapper.ToDefinition(metadataEntity);
        var metadataInvalid = KnOwlButterMorphDefinitionMapper.ToDefinition(metadataEntity, new ContractFieldMetadataVersion
        {
            VersionNumber = "2.0.0",
            DefinitionJson = "{not-json",
            IsActive = true
        });
        var customJson = KnOwlButterMorphDefinitionMapper.SerializeCustomFieldDefinition(new CustomFieldDefinition
        {
            Key = "node",
            Name = "Node",
            Version = "1.0.0",
            DataType = "object",
            IsActive = true,
            ChildrenDefinition = KnOwlButterMorphDefinitionMapper.ParseElement("""[{"key":"child"}]"""),
            ArrayItemDefinition = KnOwlButterMorphDefinitionMapper.ParseElement("""{"type":"string"}""")
        });

        Assert.Equal("legacy", schemaFallback.Key);
        Assert.Equal("{}", schemaFallback.JsonSchema);
        Assert.Equal("metadata", metadataFallback.Key);
        Assert.Equal("1.0.0", metadataFallback.Version);
        Assert.Equal("metadata", metadataInvalid.Key);
        Assert.Equal(["events", "commands"], KnOwlButterMorphDefinitionMapper.ToKnOwlScopes([]));
        Assert.Equal(["Schema"], KnOwlButterMorphDefinitionMapper.ToButterMorphScopes("[]"));
        Assert.Contains("\"childrenDefinition\":[", customJson, StringComparison.Ordinal);
        Assert.Contains("\"arrayItemDefinition\":{", customJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SchemaTypeDesignerHostExercisesCreateAndVersionBranches()
    {
        var typeId = Guid.NewGuid();
        var schemaTypes = new SchemaTypeInteractionStub
        {
            Types =
            [
                new KnOwlSchemaTypeDefinition
                {
                    Id = typeId,
                    Key = "customer",
                    Name = "Customer",
                    IsSystem = false,
                    Versions =
                    [
                        new SchemaTypeVersion
                        {
                            VersionNumber = "1.0.0",
                            IsActive = true,
                            DefinitionJson = KnOwlButterMorphDefinitionMapper.SerializeSchemaTypeDefinition(CreateSchemaTypeDefinition("customer", "1.0.0"))
                        }
                    ]
                }
            ]
        };
        schemaTypes.ActiveVersions = schemaTypes.Types.Single().Versions.ToList();
        var host = new KnOwlSchemaTypeDesignerHost(schemaTypes);

        var loaded = await host.Load(new global::ButterMorph.Web.Razor.ButterMorphSchemaTypeDesignerLoadRequest { ContextKey = KnOwlButterMorphContext.NewTypeVersion(typeId) });
        schemaTypes.KeyExistsResult = true;
        var duplicate = await host.Save(CreateSchemaTypeSave(KnOwlButterMorphContext.NewType(), "customer", "1.0.0"));
        schemaTypes.KeyExistsResult = false;
        var created = await host.Save(CreateSchemaTypeSave(KnOwlButterMorphContext.NewType(), "address", string.Empty));
        schemaTypes.VersionExistsResult = true;
        var duplicateVersion = await host.Save(CreateSchemaTypeSave(KnOwlButterMorphContext.NewTypeVersion(typeId), "customer", "1.0.1"));
        schemaTypes.VersionExistsResult = false;
        var addedVersion = await host.Save(CreateSchemaTypeSave(KnOwlButterMorphContext.NewTypeVersion(typeId), "customer", "1.0.1"));

        Assert.Equal("1.0.1", loaded.Definition.Version);
        Assert.False(duplicate.Succeeded);
        Assert.True(created.Succeeded);
        Assert.Single(schemaTypes.Created);
        Assert.False(duplicateVersion.Succeeded);
        Assert.True(addedVersion.Succeeded);
        Assert.Single(schemaTypes.AddedVersions);
    }

    [Fact]
    public async Task FieldMetadataDesignerHostExercisesCreateEditAndVersionBranches()
    {
        var fieldId = Guid.NewGuid();
        var metadata = new ContractFieldMetadataDefinition
        {
            Id = fieldId,
            Key = "trace-id",
            Name = "Trace Id",
            Description = "Trace",
            IsActive = true,
            Versions =
            [
                new ContractFieldMetadataVersion
                {
                    VersionNumber = "1.0.0",
                    IsActive = true,
                    DefinitionJson = KnOwlButterMorphDefinitionMapper.SerializeCustomFieldDefinition(CreateCustomFieldDefinition("trace-id", "1.0.0"))
                }
            ]
        };
        var fields = new MetadataInteractionStub([metadata]) { Entity = metadata };
        var host = new KnOwlFieldMetadataDesignerHost(fields);

        var loadedEdit = await host.Load(new global::ButterMorph.Web.Razor.ButterMorphFieldMetadataDesignerLoadRequest { ContextKey = KnOwlButterMorphContext.EditMetadataField(fieldId) });
        var loadedNewVersion = await host.Load(new global::ButterMorph.Web.Razor.ButterMorphFieldMetadataDesignerLoadRequest { ContextKey = KnOwlButterMorphContext.NewMetadataFieldVersion(fieldId) });
        fields.KeyExistsResult = true;
        var duplicateCreate = await host.Save(CreateFieldSave(KnOwlButterMorphContext.NewMetadataField(), "trace-id", "1.0.0"));
        fields.KeyExistsResult = false;
        var created = await host.Save(CreateFieldSave(KnOwlButterMorphContext.NewMetadataField(), "correlation-id", string.Empty));
        var edited = await host.Save(CreateFieldSave(KnOwlButterMorphContext.EditMetadataField(fieldId), "trace-id", "1.0.0"));
        fields.VersionExistsResult = true;
        var duplicateVersion = await host.Save(CreateFieldSave(KnOwlButterMorphContext.NewMetadataFieldVersion(fieldId), "trace-id", "1.0.1"));
        fields.VersionExistsResult = false;
        var addedVersion = await host.Save(CreateFieldSave(KnOwlButterMorphContext.NewMetadataFieldVersion(fieldId), "trace-id", "1.0.1"));

        Assert.Equal("trace-id", loadedEdit.Definition.Key);
        Assert.Equal("1.0.1", loadedNewVersion.Definition.Version);
        Assert.False(duplicateCreate.Succeeded);
        Assert.True(created.Succeeded);
        Assert.Single(fields.Created);
        Assert.True(edited.Succeeded);
        Assert.False(duplicateVersion.Succeeded);
        Assert.True(addedVersion.Succeeded);
        Assert.Equal(2, fields.UpsertedVersions.Count);
    }

    private static global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerSaveRequest CreatePayloadSave(
        string contextKey,
        string version,
        string? comment = null,
        string key = "customer.created",
        string name = "Customer Created",
        string? topicMetadata = null,
        string? topicMetadataObjectValue = null)
    {
        PayloadSchemaDefinition definition = new()
        {
            Key = key,
            Name = name,
            Description = "Payload",
            Version = version,
            VersionComment = comment ?? string.Empty,
            Type = "object"
        };
        Dictionary<string, JsonElement> metadata = [];
        if (topicMetadata is not null)
        {
            metadata["topic"] = KnOwlButterMorphDefinitionMapper.ParseElement(JsonSerializer.Serialize(topicMetadata));
        }

        if (topicMetadataObjectValue is not null)
        {
            metadata["topic"] = KnOwlButterMorphDefinitionMapper.ParseElement($$"""{"value":{{JsonSerializer.Serialize(topicMetadataObjectValue)}}}""");
        }

        if (metadata.Count > 0)
        {
            definition.Metadata = metadata;
        }

        return new global::ButterMorph.Web.Razor.ButterMorphPayloadSchemaDesignerSaveRequest
        {
            ContextKey = contextKey,
            Definition = definition
        };
    }

    private static ButterMorphSchemaTypeDefinition CreateSchemaTypeDefinition(string key, string version)
        => new()
        {
            Key = key,
            Name = key,
            Description = "Type",
            Version = version,
            BaseType = "object",
            Schema = KnOwlButterMorphDefinitionMapper.ParseElement("""{"type":"object"}"""),
            JsonSchema = """{"type":"object"}"""
        };

    private static global::ButterMorph.Web.Razor.ButterMorphSchemaTypeDesignerSaveRequest CreateSchemaTypeSave(string contextKey, string key, string version)
        => new()
        {
            ContextKey = contextKey,
            Definition = CreateSchemaTypeDefinition(key, version)
        };

    private static CustomFieldDefinition CreateCustomFieldDefinition(string key, string version)
        => new()
        {
            Key = key,
            Name = key,
            Description = "Field",
            Version = version,
            VersionComment = "Field version",
            DataType = "string",
            AppliesTo = ["Schema"],
            IsActive = true
        };

    private static global::ButterMorph.Web.Razor.ButterMorphFieldMetadataDesignerSaveRequest CreateFieldSave(string contextKey, string key, string version)
        => new()
        {
            ContextKey = contextKey,
            Definition = CreateCustomFieldDefinition(key, version)
        };

    private sealed class EventInteractionStub : IEventInteractionService
    {
        public EventDefinition? Entity { get; init; }
        public EventVersion? Version { get; init; }
        public bool VersionExistsResult { get; set; }
        public List<EventDefinition> Created { get; } = [];
        public List<EventVersion> AddedVersions { get; } = [];
        public Guid? UpdatedVersionId { get; private set; }
        public string? UpdatedPayloadSchemaJson { get; private set; }
        public string? UpdatedComment { get; private set; }

        public Task<IReadOnlyList<EventDefinition>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<EventDefinition>>([]);
        public Task<EventDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult(Entity?.Id == id ? Entity : null);
        public Task<EventVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(Version?.Id == versionId ? Version : null);
        public Task<bool> VersionExists(Guid eventId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(VersionExistsResult);
        public Task Create(EventDefinition eventDefinition, CancellationToken cancellationToken = default) { Created.Add(eventDefinition); return Task.CompletedTask; }
        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid eventId, EventVersion version, CancellationToken cancellationToken = default) { AddedVersions.Add(version); return Task.CompletedTask; }
        public Task UpdateDraftVersion(Guid versionId, string payloadSchemaJson, string? comment, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            if (Version is null || Version.Id != versionId)
            {
                throw new KeyNotFoundException($"Event version '{versionId}' was not found.");
            }

            if (Version.Status != ContractVersionStatus.Draft)
            {
                throw new InvalidOperationException("Only draft event versions can be edited.");
            }

            UpdatedVersionId = versionId;
            UpdatedPayloadSchemaJson = payloadSchemaJson;
            UpdatedComment = comment;
            return Task.CompletedTask;
        }

        public Task Delete(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class CommandInteractionStub : ICommandInteractionService
    {
        public CommandDefinition? Entity { get; init; }
        public CommandVersion? Version { get; init; }
        public bool VersionExistsResult { get; set; }
        public List<CommandDefinition> Created { get; } = [];
        public List<CommandVersion> AddedVersions { get; } = [];
        public Guid? UpdatedVersionId { get; private set; }
        public string? UpdatedRequestSchemaJson { get; private set; }
        public string? UpdatedReplySchemaJson { get; private set; }
        public string? UpdatedComment { get; private set; }

        public Task<IReadOnlyList<CommandDefinition>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CommandDefinition>>([]);
        public Task<CommandDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult(Entity?.Id == id ? Entity : null);
        public Task<CommandVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(Version?.Id == versionId ? Version : null);
        public Task<bool> VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(VersionExistsResult);
        public Task Create(CommandDefinition commandDefinition, CancellationToken cancellationToken = default)
        {
            Created.Add(commandDefinition);
            return Task.CompletedTask;
        }
        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid commandId, CommandVersion version, CancellationToken cancellationToken = default) { AddedVersions.Add(version); return Task.CompletedTask; }
        public Task UpdateDraftVersion(Guid versionId, string requestSchemaJson, string? replySchemaJson, string? comment, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            if (Version is null || Version.Id != versionId)
            {
                throw new KeyNotFoundException($"Command version '{versionId}' was not found.");
            }

            if (Version.Status != ContractVersionStatus.Draft)
            {
                throw new InvalidOperationException("Only draft command versions can be edited.");
            }

            UpdatedVersionId = versionId;
            UpdatedRequestSchemaJson = requestSchemaJson;
            UpdatedReplySchemaJson = replySchemaJson;
            UpdatedComment = comment;
            return Task.CompletedTask;
        }

        public Task Delete(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class SchemaTypeInteractionStub : ISchemaTypeInteractionService
    {
        public IReadOnlyList<KnOwlSchemaTypeDefinition> Types { get; init; } = [];
        public IReadOnlyList<SchemaTypeVersion> ActiveVersions { get; set; } = [];
        public bool KeyExistsResult { get; set; }
        public bool VersionExistsResult { get; set; }
        public List<KnOwlSchemaTypeDefinition> Created { get; } = [];
        public List<SchemaTypeVersion> AddedVersions { get; } = [];

        public Task<IReadOnlyList<KnOwlSchemaTypeDefinition>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult(Types);
        public Task<IReadOnlyList<SchemaTypeVersion>> GetActiveVersions(CancellationToken cancellationToken = default) => Task.FromResult(ActiveVersions);
        public Task<KnOwlSchemaTypeDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult(Types.FirstOrDefault(x => x.Id == id));
        public Task<SchemaTypeVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(Types.SelectMany(x => x.Versions).FirstOrDefault(x => x.Id == versionId));
        public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(KeyExistsResult);
        public Task<bool> VersionExists(Guid typeId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(VersionExistsResult);
        public Task Create(KnOwlSchemaTypeDefinition schemaType, CancellationToken cancellationToken = default) { Created.Add(schemaType); return Task.CompletedTask; }
        public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid typeId, SchemaTypeVersion version, CancellationToken cancellationToken = default) { AddedVersions.Add(version); return Task.CompletedTask; }
        public Task SetVersionActive(Guid typeId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MetadataInteractionStub(IReadOnlyList<ContractFieldMetadataDefinition>? active = null) : IContractFieldMetadataInteractionService
    {
        private readonly IReadOnlyList<ContractFieldMetadataDefinition> _active = active ?? [];
        public ContractFieldMetadataDefinition? Entity { get; init; }
        public bool KeyExistsResult { get; set; }
        public bool VersionExistsResult { get; set; }
        public List<ContractFieldMetadataDefinition> Created { get; } = [];
        public List<ContractFieldMetadataVersion> UpsertedVersions { get; } = [];

        public Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractFieldMetadataDefinition>>([]);
        public Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetActive(CancellationToken cancellationToken = default) => Task.FromResult(_active);
        public Task<ContractFieldMetadataDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult(Entity?.Id == id ? Entity : _active.FirstOrDefault(x => x.Id == id));
        public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(KeyExistsResult);
        public Task<bool> VersionExists(Guid metadataFieldId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(VersionExistsResult);
        public Task Create(ContractFieldMetadataDefinition metadataField, CancellationToken cancellationToken = default) { Created.Add(metadataField); return Task.CompletedTask; }
        public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpsertVersion(Guid metadataFieldId, ContractFieldMetadataVersion version, CancellationToken cancellationToken = default) { UpsertedVersions.Add(version); return Task.CompletedTask; }
        public Task SetVersionActive(Guid metadataFieldId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }}





