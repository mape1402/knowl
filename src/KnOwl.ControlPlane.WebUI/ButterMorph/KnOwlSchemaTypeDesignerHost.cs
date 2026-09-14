namespace KnOwl.ControlPlane.WebUI.ButterMorph;

using global::ButterMorph.SchemaDesign;
using global::ButterMorph.Web.Razor;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using KnOwlSchemaTypeDefinition = KnOwl.ControlPlane.Design.Core.SchemaTypeDefinition;
using ButterMorphSchemaTypeDefinition = global::ButterMorph.SchemaDesign.SchemaTypeDefinition;

/// <inheritdoc />
public sealed class KnOwlSchemaTypeDesignerHost(ISchemaTypeInteractionService schemaTypes) : IButterMorphSchemaTypeDesignerHost
{
    /// <inheritdoc />
    public async Task<ButterMorphSchemaTypeDesignerLoadResult> Load(ButterMorphSchemaTypeDesignerLoadRequest request)
    {
        var result = new ButterMorphSchemaTypeDesignerLoadResult
        {
            SchemaTypes = await LoadCatalogAsync(),
            ShowManualActions = false
        };

        if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "type-version:new:", out var typeId))
        {
            var type = await LoadTypeAsync(typeId);
            var latest = type?.Versions.Where(x => x.IsActive).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();
            if (type is not null && latest is not null)
            {
                result.Definition = KnOwlButterMorphDefinitionMapper.ToDefinition(type, latest);
                result.Definition.Version = NextVersion(result.Definition.Version);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ButterMorphSchemaTypeDesignerSaveResult> Save(ButterMorphSchemaTypeDesignerSaveRequest request)
    {
        var definition = request.Definition;
        var now = DateTime.UtcNow;
        var definitionJson = KnOwlButterMorphDefinitionMapper.SerializeSchemaTypeDefinition(definition);

        if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "type-version:new:", out var typeId))
        {
            var type = await LoadTypeAsync(typeId);
            if (type is null)
            {
                return new ButterMorphSchemaTypeDesignerSaveResult { Succeeded = false, Message = "Schema type was not found." };
            }

            if (await schemaTypes.KeyExists(definition.Key, excludingId: typeId))
            {
                return new ButterMorphSchemaTypeDesignerSaveResult { Succeeded = false, Message = "A schema type with this key already exists." };
            }

            if (await schemaTypes.VersionExists(typeId, ResolveVersion(definition)))
            {
                return new ButterMorphSchemaTypeDesignerSaveResult { Succeeded = false, Message = "That schema type version already exists." };
            }

            if (!await UpdateTypeAsync(typeId, definition, now))
            {
                return new ButterMorphSchemaTypeDesignerSaveResult { Succeeded = false, Message = "Schema type was not found." };
            }

            await schemaTypes.AddVersion(typeId, CreateVersion(typeId, definition, definitionJson, now));
            return new ButterMorphSchemaTypeDesignerSaveResult { Succeeded = true, Message = "Schema type version saved." };
        }

        if (await schemaTypes.KeyExists(definition.Key))
        {
            return new ButterMorphSchemaTypeDesignerSaveResult { Succeeded = false, Message = "A schema type with this key already exists." };
        }

        var entity = new KnOwlSchemaTypeDefinition
        {
            CreatedAtUtc = now
        };
        Apply(entity, definition, now);
        AddVersion(entity, definition, definitionJson, now);
        await schemaTypes.Create(entity);
        return new ButterMorphSchemaTypeDesignerSaveResult { Succeeded = true, Message = "Schema type saved." };
    }

    private async Task<KnOwlSchemaTypeDefinition?> LoadTypeAsync(Guid id)
    {
        var type = await schemaTypes.GetById(id, includeVersions: true);
        return type is { IsSystem: false } ? type : null;
    }

    private async Task<bool> UpdateTypeAsync(Guid id, ButterMorphSchemaTypeDefinition definition, DateTime now)
    {
        try
        {
            await schemaTypes.UpdateDefinition(
                id,
                definition.Key.Trim(),
                definition.Name.Trim(),
                string.IsNullOrWhiteSpace(definition.Description) ? null : definition.Description.Trim(),
                isActive: true,
                now);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    private static void Apply(KnOwlSchemaTypeDefinition entity, ButterMorphSchemaTypeDefinition definition, DateTime now)
    {
        entity.Name = definition.Name.Trim();
        entity.Key = definition.Key.Trim();
        entity.Description = string.IsNullOrWhiteSpace(definition.Description) ? null : definition.Description.Trim();
        entity.IsSystem = false;
        entity.IsActive = true;
        entity.UpdatedAtUtc = now;
    }

    private static void AddVersion(KnOwlSchemaTypeDefinition entity, ButterMorphSchemaTypeDefinition definition, string definitionJson, DateTime now)
    {
        entity.Versions.Add(CreateVersion(entity.Id, definition, definitionJson, now));
    }

    private static SchemaTypeVersion CreateVersion(Guid typeId, ButterMorphSchemaTypeDefinition definition, string definitionJson, DateTime now)
    {
        return new SchemaTypeVersion
        {
            SchemaTypeDefinitionId = typeId,
            VersionNumber = ResolveVersion(definition),
            DefinitionJson = definitionJson,
            Comment = string.IsNullOrWhiteSpace(definition.Comment) ? null : definition.Comment.Trim(),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    private async Task<IReadOnlyCollection<SchemaTypeCatalogItem>> LoadCatalogAsync()
    {
        var versions = await schemaTypes.GetActiveVersions();

        return versions
            .Where(x => x.SchemaTypeDefinition is not null)
            .Select(x => KnOwlButterMorphDefinitionMapper.ToCatalogItem(x.SchemaTypeDefinition!, x, x.SchemaTypeDefinition!.IsSystem))
            .ToList();
    }

    private static string ResolveVersion(ButterMorphSchemaTypeDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.Version) ? "1.0.0" : definition.Version.Trim();
    }

    private static string NextVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length == 3 && int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor) && int.TryParse(parts[2], out var patch))
        {
            return $"{major}.{minor}.{patch + 1}";
        }

        return string.IsNullOrWhiteSpace(version) ? "1.0.0" : $"{version}.1";
    }
}

