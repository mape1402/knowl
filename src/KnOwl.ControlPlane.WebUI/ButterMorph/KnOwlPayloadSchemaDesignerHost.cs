namespace KnOwl.ControlPlane.WebUI.ButterMorph;

using global::ButterMorph.SchemaDesign;
using global::ButterMorph.Web.Razor;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using System.Text.Json;

/// <inheritdoc />
public sealed class KnOwlPayloadSchemaDesignerHost(
    IEventInteractionService events,
    ICommandInteractionService commands,
    ISchemaTypeInteractionService schemaTypes,
    IContractFieldMetadataInteractionService metadataFields,
    KnOwlButterMorphDraftStore draftStore) : IButterMorphPayloadSchemaDesignerHost
{
    /// <inheritdoc />
    public async Task<ButterMorphPayloadSchemaDesignerLoadResult> Load(ButterMorphPayloadSchemaDesignerLoadRequest request)
    {
        var result = new ButterMorphPayloadSchemaDesignerLoadResult
        {
            Version = "1.0.0",
            ShowManualActions = false,
            SchemaTypes = await LoadTypeCatalogAsync(),
            MetadataFields = await LoadMetadataCatalogAsync()
        };

        if (request.ContextKey.StartsWith("event:new:", StringComparison.OrdinalIgnoreCase))
        {
        }
        else if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "event-version:new:", out var eventId))
        {
            var entity = await events.GetById(eventId, includeVersions: true);
            var latest = entity?.Versions.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();
            if (entity is not null)
            {
                ApplyEventLoadResult(result, entity, latest);
            }
        }
        else if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "event-version:edit:", out var eventVersionId))
        {
            var version = await events.GetVersionById(eventVersionId);
            if (version is not null)
            {
                ApplyEventEditLoadResult(result, version);
            }
        }
        else if (request.ContextKey.StartsWith("command:new:", StringComparison.OrdinalIgnoreCase))
        {
        }
        else if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "command-create-request:new:", out _))
        {
        }
        else if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "command-create-reply:new:", out _))
        {
        }
        else if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "command-version:new:", out var commandId))
        {
            var entity = await commands.GetById(commandId, includeVersions: true);
            var latest = entity?.Versions.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();
            if (entity is not null)
            {
                ApplyCommandLoadResult(result, entity, latest);
            }
        }
        else if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "command-version-request:new:", out var commandRequestId))
        {
            var entity = await commands.GetById(commandRequestId, includeVersions: true);
            var latest = entity?.Versions.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();
            if (entity is not null)
            {
                ApplyCommandLoadResult(result, entity, latest);
            }
        }
        else if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "command-version-request:edit:", out var commandRequestVersionId))
        {
            var version = await commands.GetVersionById(commandRequestVersionId);
            if (version is not null)
            {
                ApplyCommandRequestEditLoadResult(result, version);
            }
        }
        else if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "command-version-reply:new:", out var commandReplyId))
        {
            var entity = await commands.GetById(commandReplyId, includeVersions: true);
            var latest = entity?.Versions.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();
            if (entity is not null)
            {
                ApplyCommandReplyLoadResult(result, entity, latest);
            }
        }
        else if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "command-version-reply:edit:", out var commandReplyVersionId))
        {
            var version = await commands.GetVersionById(commandReplyVersionId);
            if (version is not null)
            {
                ApplyCommandReplyEditLoadResult(result, version);
            }
        }

        return result;
    }

    private static void ApplyEventLoadResult(ButterMorphPayloadSchemaDesignerLoadResult result, EventDefinition entity, EventVersion? latest)
    {
        if (latest is not null && TryReadPayloadDefinition(latest.PayloadSchemaJson, out var definition))
        {
            definition.Version = NextVersion(definition.Version);
            result.Definition = definition;
            return;
        }

        result.Key = entity.Key;
        result.Name = entity.Name;
        result.Description = entity.Description ?? string.Empty;
        result.Version = NextVersion(latest?.VersionNumber);
        result.JsonSchema = latest?.PayloadSchemaJson ?? string.Empty;
    }

    private static void ApplyEventEditLoadResult(ButterMorphPayloadSchemaDesignerLoadResult result, EventVersion version)
    {
        if (TryReadPayloadDefinition(version.PayloadSchemaJson, out var definition))
        {
            definition.Version = version.VersionNumber;
            definition.VersionComment = version.Comment ?? definition.VersionComment;
            result.Definition = definition;
            return;
        }

        var entity = version.EventDefinition;
        result.Key = entity?.Key ?? string.Empty;
        result.Name = entity?.Name ?? string.Empty;
        result.Description = entity?.Description ?? string.Empty;
        result.Version = version.VersionNumber;
        result.JsonSchema = version.PayloadSchemaJson;
    }

    private static void ApplyCommandLoadResult(ButterMorphPayloadSchemaDesignerLoadResult result, CommandDefinition entity, CommandVersion? latest)
    {
        if (latest is not null && TryReadPayloadDefinition(latest.PayloadSchemaJson, out var definition))
        {
            definition.Version = NextVersion(definition.Version);
            result.Definition = definition;
            return;
        }

        result.Key = entity.Key;
        result.Name = entity.Name;
        result.Description = entity.Description ?? string.Empty;
        result.Version = NextVersion(latest?.VersionNumber);
        result.JsonSchema = latest?.PayloadSchemaJson ?? string.Empty;
    }

    private static void ApplyCommandRequestEditLoadResult(ButterMorphPayloadSchemaDesignerLoadResult result, CommandVersion version)
    {
        if (TryReadPayloadDefinition(version.PayloadSchemaJson, out var definition))
        {
            definition.Version = version.VersionNumber;
            definition.VersionComment = version.Comment ?? definition.VersionComment;
            result.Definition = definition;
            return;
        }

        var entity = version.CommandDefinition;
        result.Key = entity?.Key ?? string.Empty;
        result.Name = entity?.Name ?? string.Empty;
        result.Description = entity?.Description ?? string.Empty;
        result.Version = version.VersionNumber;
        result.JsonSchema = version.PayloadSchemaJson;
    }

    private static void ApplyCommandReplyLoadResult(ButterMorphPayloadSchemaDesignerLoadResult result, CommandDefinition entity, CommandVersion? latest)
    {
        result.Key = $"{entity.Key}.reply";
        result.Name = $"{entity.Name} Reply";
        result.Description = entity.Description ?? string.Empty;
        result.Version = NextVersion(latest?.VersionNumber);
        result.JsonSchema = latest?.ReplyPayloadSchemaJson ?? string.Empty;
    }

    private static void ApplyCommandReplyEditLoadResult(ButterMorphPayloadSchemaDesignerLoadResult result, CommandVersion version)
    {
        if (!string.IsNullOrWhiteSpace(version.ReplyPayloadSchemaJson) &&
            TryReadPayloadDefinition(version.ReplyPayloadSchemaJson, out var definition))
        {
            definition.Version = version.VersionNumber;
            definition.VersionComment = version.Comment ?? definition.VersionComment;
            result.Definition = definition;
            return;
        }

        var entity = version.CommandDefinition;
        result.Key = entity is null ? string.Empty : $"{entity.Key}.reply";
        result.Name = entity is null ? string.Empty : $"{entity.Name} Reply";
        result.Description = entity?.Description ?? string.Empty;
        result.Version = version.VersionNumber;
        result.JsonSchema = version.ReplyPayloadSchemaJson ?? string.Empty;
    }

    private static bool TryReadPayloadDefinition(string json, out PayloadSchemaDefinition definition)
    {
        definition = new PayloadSchemaDefinition();
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("key", out _) ||
                !document.RootElement.TryGetProperty("properties", out _))
            {
                return false;
            }

            var parsed = JsonSerializer.Deserialize<PayloadSchemaDefinition>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (parsed is null)
            {
                return false;
            }

            definition = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ButterMorphPayloadSchemaDesignerSaveResult> Save(ButterMorphPayloadSchemaDesignerSaveRequest request)
    {
        var schemaJson = KnOwlButterMorphDefinitionMapper.GetSchemaJson(request.Definition);
        if (request.ContextKey.StartsWith("event:new:", StringComparison.OrdinalIgnoreCase))
        {
            return await CreateEvent(request, schemaJson);
        }

        if (request.ContextKey.StartsWith("command:new:", StringComparison.OrdinalIgnoreCase))
        {
            return await CreateCommand(request, schemaJson);
        }

        if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "event-version:new:", out var eventId))
        {
            var entity = await events.GetById(eventId);
            if (entity is null)
            {
                return new ButterMorphPayloadSchemaDesignerSaveResult
                {
                    Succeeded = false,
                    Message = "Event not found."
                };
            }

            var version = string.IsNullOrWhiteSpace(request.Definition.Version)
                ? "1.0.0"
                : request.Definition.Version.Trim();
            if (await events.VersionExists(eventId, version))
            {
                return new ButterMorphPayloadSchemaDesignerSaveResult
                {
                    Succeeded = false,
                    Message = $"Event version {version} already exists."
                };
            }

            var now = DateTime.UtcNow;
            await events.AddVersion(eventId, new EventVersion
            {
                VersionNumber = version,
                PayloadSchemaJson = schemaJson,
                Comment = string.IsNullOrWhiteSpace(request.Definition.VersionComment) ? null : request.Definition.VersionComment.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

            return new ButterMorphPayloadSchemaDesignerSaveResult
            {
                Succeeded = true,
                Message = "Event version created."
            };
        }

        if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "event-version:edit:", out var eventVersionId))
        {
            try
            {
                await events.UpdateDraftVersion(
                    eventVersionId,
                    schemaJson,
                    NormalizeComment(request.Definition.VersionComment),
                    DateTime.UtcNow);
                return new ButterMorphPayloadSchemaDesignerSaveResult
                {
                    Succeeded = true,
                    Message = "Draft event version updated."
                };
            }
            catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
            {
                return Failed(ex.Message);
            }
        }

        if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "command-version:new:", out var commandId))
        {
            var entity = await commands.GetById(commandId);
            if (entity is null)
            {
                return Failed("Command not found.");
            }

            var version = string.IsNullOrWhiteSpace(request.Definition.Version)
                ? "1.0.0"
                : request.Definition.Version.Trim();
            if (await commands.VersionExists(commandId, version))
            {
                return Failed($"Command version {version} already exists.");
            }

            var now = DateTime.UtcNow;
            await commands.AddVersion(commandId, new CommandVersion
            {
                VersionNumber = version,
                PayloadSchemaJson = schemaJson,
                Comment = string.IsNullOrWhiteSpace(request.Definition.VersionComment) ? null : request.Definition.VersionComment.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

            return new ButterMorphPayloadSchemaDesignerSaveResult
            {
                Succeeded = true,
                Message = "Command version created."
            };
        }

        if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "command-version-request:edit:", out var requestVersionId))
        {
            var version = await commands.GetVersionById(requestVersionId);
            if (version is null)
            {
                return Failed("Command version not found.");
            }

            try
            {
                await commands.UpdateDraftVersion(
                    requestVersionId,
                    schemaJson,
                    version.ReplyPayloadSchemaJson,
                    NormalizeComment(request.Definition.VersionComment, version.Comment),
                    DateTime.UtcNow);
                return new ButterMorphPayloadSchemaDesignerSaveResult
                {
                    Succeeded = true,
                    Message = "Draft command request updated."
                };
            }
            catch (InvalidOperationException ex)
            {
                return Failed(ex.Message);
            }
        }

        if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "command-version-reply:edit:", out var replyVersionId))
        {
            var version = await commands.GetVersionById(replyVersionId);
            if (version is null)
            {
                return Failed("Command version not found.");
            }

            try
            {
                await commands.UpdateDraftVersion(
                    replyVersionId,
                    version.PayloadSchemaJson,
                    schemaJson,
                    NormalizeComment(request.Definition.VersionComment, version.Comment),
                    DateTime.UtcNow);
                return new ButterMorphPayloadSchemaDesignerSaveResult
                {
                    Succeeded = true,
                    Message = "Draft command reply updated."
                };
            }
            catch (InvalidOperationException ex)
            {
                return Failed(ex.Message);
            }
        }

        draftStore.SavePayloadSchema(request.ContextKey, schemaJson);
        return new ButterMorphPayloadSchemaDesignerSaveResult
        {
            Succeeded = true,
            Message = "Payload schema captured. Save the KnOwl form to persist it."
        };
    }

    private async Task<ButterMorphPayloadSchemaDesignerSaveResult> CreateEvent(ButterMorphPayloadSchemaDesignerSaveRequest request, string schemaJson)
    {
        if (string.IsNullOrWhiteSpace(request.Definition.Name))
        {
            return Failed("Event name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Definition.Version))
        {
            return Failed("Event version is required.");
        }

        if (!TryReadDefinitionKey(request.Definition, out var key))
        {
            return Failed("Event schema key is required.");
        }

        if (key.Length > 100)
        {
            return Failed("Event schema key must be 100 characters or fewer.");
        }

        var now = DateTime.UtcNow;
        EventDefinition eventDefinition = new()
        {
            Name = request.Definition.Name.Trim(),
            Key = key,
            Description = string.IsNullOrWhiteSpace(request.Definition.Description) ? null : request.Definition.Description.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        eventDefinition.Versions.Add(new EventVersion
        {
            VersionNumber = request.Definition.Version.Trim(),
            PayloadSchemaJson = schemaJson,
            Comment = string.IsNullOrWhiteSpace(request.Definition.VersionComment) ? null : request.Definition.VersionComment.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        await events.Create(eventDefinition);
        draftStore.SaveCreatedEvent(request.ContextKey, eventDefinition.Id);

        return new ButterMorphPayloadSchemaDesignerSaveResult
        {
            Succeeded = true,
            Message = "Event created."
        };
    }

    private async Task<ButterMorphPayloadSchemaDesignerSaveResult> CreateCommand(ButterMorphPayloadSchemaDesignerSaveRequest request, string schemaJson)
    {
        if (string.IsNullOrWhiteSpace(request.Definition.Name))
        {
            return Failed("Command name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Definition.Version))
        {
            return Failed("Command version is required.");
        }

        if (!TryReadDefinitionKey(request.Definition, out var key))
        {
            return Failed("Command schema key is required.");
        }

        if (key.Length > 100)
        {
            return Failed("Command schema key must be 100 characters or fewer.");
        }

        var now = DateTime.UtcNow;
        CommandDefinition commandDefinition = new()
        {
            Name = request.Definition.Name.Trim(),
            Key = key,
            Description = string.IsNullOrWhiteSpace(request.Definition.Description) ? null : request.Definition.Description.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        commandDefinition.Versions.Add(new CommandVersion
        {
            VersionNumber = request.Definition.Version.Trim(),
            PayloadSchemaJson = schemaJson,
            Comment = string.IsNullOrWhiteSpace(request.Definition.VersionComment) ? null : request.Definition.VersionComment.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        await commands.Create(commandDefinition);
        draftStore.SaveCreatedCommand(request.ContextKey, commandDefinition.Id);

        return new ButterMorphPayloadSchemaDesignerSaveResult
        {
            Succeeded = true,
            Message = "Command created."
        };
    }

    private static ButterMorphPayloadSchemaDesignerSaveResult Failed(string message)
    {
        return new ButterMorphPayloadSchemaDesignerSaveResult
        {
            Succeeded = false,
            Message = message
        };
    }

    private static string? NormalizeComment(string? comment, string? fallback = null)
        => string.IsNullOrWhiteSpace(comment)
            ? string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim()
            : comment.Trim();

    private static bool TryReadDefinitionKey(PayloadSchemaDefinition definition, out string key)
    {
        key = definition.Key?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(key);
    }

    private async Task<IReadOnlyCollection<SchemaTypeCatalogItem>> LoadTypeCatalogAsync()
    {
        var versions = await schemaTypes.GetActiveVersions();

        var persistedItems = versions
            .Where(x => x.SchemaTypeDefinition is not null)
            .Select(x => KnOwlButterMorphDefinitionMapper.ToCatalogItem(
                x.SchemaTypeDefinition!,
                x,
                x.SchemaTypeDefinition!.IsSystem && !IsTemporalSystemType(x.SchemaTypeDefinition.Name)))
            .ToList();

        foreach (var systemItem in CreateTemporalSystemCatalogItems())
        {
            if (persistedItems.Any(x =>
                    string.Equals(x.Name, systemItem.Name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.TypeVersionId, systemItem.TypeVersionId, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            persistedItems.Add(systemItem);
        }

        return persistedItems
            .OrderBy(x => x.IsSystem ? 0 : 1)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<IReadOnlyCollection<FieldMetadataCatalogItem>> LoadMetadataCatalogAsync()
    {
        var entities = await metadataFields.GetActive();

        var items = entities
            .Select(x => new
            {
                Entity = x,
                Version = x.Versions
                    .Where(version => version.IsActive)
                    .OrderByDescending(version => version.CreatedAtUtc)
                    .FirstOrDefault()
            })
            .Where(x => x.Version is not null)
            .Select(x => KnOwlButterMorphDefinitionMapper.ToCatalogItem(x.Entity, x.Version!))
            .ToList();

        return items;
    }

    private static IReadOnlyCollection<SchemaTypeCatalogItem> CreateTemporalSystemCatalogItems()
    {
        return
        [
            CreateTemporalSystemCatalogItem("Date", "{\"type\":\"string\",\"format\":\"date\",\"pattern\":\"^\\\\d{4}-\\\\d{2}-\\\\d{2}$\"}"),
            CreateTemporalSystemCatalogItem("DateTime", "{\"type\":\"string\",\"format\":\"date-time\",\"pattern\":\"^\\\\d{4}-\\\\d{2}-\\\\d{2}T\\\\d{2}:\\\\d{2}:\\\\d{2}(?:\\\\.\\\\d{1,7})?(?:Z|[+-]\\\\d{2}:\\\\d{2})?$\"}"),
            CreateTemporalSystemCatalogItem("Time", "{\"type\":\"string\",\"format\":\"time\",\"pattern\":\"^\\\\d{2}:\\\\d{2}:\\\\d{2}(?:\\\\.\\\\d{1,7})?$\"}"),
            CreateTemporalSystemCatalogItem("TimeSpan", "{\"type\":\"string\",\"pattern\":\"^(?:\\\\d+\\\\.)?\\\\d{2}:\\\\d{2}:\\\\d{2}(?:\\\\.\\\\d{1,7})?$\"}")
        ];
    }

    private static SchemaTypeCatalogItem CreateTemporalSystemCatalogItem(string name, string jsonSchema)
    {
        return new SchemaTypeCatalogItem
        {
            TypeId = name,
            TypeVersionId = $"{name}@1.0.0",
            Name = name,
            VersionNumber = "1.0.0",
            BaseType = "string",
            JsonSchema = jsonSchema,
            IsSystem = false
        };
    }

    private static bool IsTemporalSystemType(string name)
    {
        return string.Equals(name, "Date", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "DateTime", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "Time", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "TimeSpan", StringComparison.OrdinalIgnoreCase);
    }

    private static string NextVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return "1.0.0";
        }

        var parts = version.Split('.');
        if (parts.Length == 3 && int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor) && int.TryParse(parts[2], out var patch))
        {
            return $"{major}.{minor}.{patch + 1}";
        }

        return $"{version}.1";
    }
}


