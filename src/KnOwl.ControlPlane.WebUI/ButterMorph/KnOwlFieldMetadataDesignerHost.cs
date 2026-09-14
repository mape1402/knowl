namespace KnOwl.ControlPlane.WebUI.ButterMorph;

using global::ButterMorph.SchemaDesign;
using global::ButterMorph.Web.Razor;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;

/// <inheritdoc />
public sealed class KnOwlFieldMetadataDesignerHost(IContractFieldMetadataInteractionService metadataFields) : IButterMorphFieldMetadataDesignerHost
{
    /// <inheritdoc />
    public async Task<ButterMorphFieldMetadataDesignerLoadResult> Load(ButterMorphFieldMetadataDesignerLoadRequest request)
    {
        var result = new ButterMorphFieldMetadataDesignerLoadResult
        {
            ShowManualActions = false
        };

        if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "metadata-field-version:new:", out var id))
        {
            var entity = await LoadMetadataFieldAsync(id);
            if (entity is not null)
            {
                result.Definition = KnOwlButterMorphDefinitionMapper.ToDefinition(entity);
                result.Definition.Version = NextVersion(result.Definition.Version);
            }
        }
        else if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "metadata-field:", out id))
        {
            var entity = await LoadMetadataFieldAsync(id);
            if (entity is not null)
            {
                result.Definition = KnOwlButterMorphDefinitionMapper.ToDefinition(entity);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ButterMorphFieldMetadataDesignerSaveResult> Save(ButterMorphFieldMetadataDesignerSaveRequest request)
    {
        try
        {
            return await SaveMetadataFieldAsync(request);
        }
        catch (KeyNotFoundException)
        {
            return new ButterMorphFieldMetadataDesignerSaveResult
            {
                Succeeded = false,
                Message = "The metadata field changed or was deleted before it could be saved. Refresh KnOwl and try again."
            };
        }
    }

    private async Task<ButterMorphFieldMetadataDesignerSaveResult> SaveMetadataFieldAsync(ButterMorphFieldMetadataDesignerSaveRequest request)
    {
        var definition = request.Definition;
        var now = DateTime.UtcNow;
        var definitionJson = KnOwlButterMorphDefinitionMapper.SerializeCustomFieldDefinition(definition);

        if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "metadata-field-version:new:", out var id))
        {
            var entity = await LoadMetadataFieldAsync(id);
            if (entity is null)
            {
                return new ButterMorphFieldMetadataDesignerSaveResult { Succeeded = false, Message = "Metadata field was not found." };
            }

            if (await metadataFields.KeyExists(definition.Key, excludingId: id))
            {
                return new ButterMorphFieldMetadataDesignerSaveResult { Succeeded = false, Message = "A metadata field with this key already exists." };
            }

            if (await metadataFields.VersionExists(id, ResolveVersion(definition)))
            {
                return new ButterMorphFieldMetadataDesignerSaveResult { Succeeded = false, Message = "That metadata field version already exists." };
            }

            if (!await UpdateDefinitionAsync(id, definition, now))
            {
                return new ButterMorphFieldMetadataDesignerSaveResult { Succeeded = false, Message = "Metadata field was not found." };
            }

            await metadataFields.UpsertVersion(id, CreateVersion(id, definition, definitionJson, now));
            return new ButterMorphFieldMetadataDesignerSaveResult { Succeeded = true, Message = "Metadata field version saved." };
        }

        if (KnOwlButterMorphContext.TryReadGuid(request.ContextKey, "metadata-field:", out id))
        {
            var entity = await LoadMetadataFieldAsync(id);
            if (entity is null)
            {
                return new ButterMorphFieldMetadataDesignerSaveResult { Succeeded = false, Message = "Metadata field was not found." };
            }

            if (await metadataFields.KeyExists(definition.Key, excludingId: id))
            {
                return new ButterMorphFieldMetadataDesignerSaveResult { Succeeded = false, Message = "A metadata field with this key already exists." };
            }

            if (!await UpdateDefinitionAsync(id, definition, now))
            {
                return new ButterMorphFieldMetadataDesignerSaveResult { Succeeded = false, Message = "Metadata field was not found." };
            }

            await UpsertVersionAsync(id, definition, definitionJson, now);
            return new ButterMorphFieldMetadataDesignerSaveResult { Succeeded = true, Message = "Metadata field saved." };
        }

        if (await metadataFields.KeyExists(definition.Key))
        {
            return new ButterMorphFieldMetadataDesignerSaveResult { Succeeded = false, Message = "A metadata field with this key already exists." };
        }

        var newEntity = new ContractFieldMetadataDefinition
        {
            CreatedAtUtc = now
        };
        Apply(newEntity, definition, now);
        AddVersion(newEntity, definition, definitionJson, now);
        await metadataFields.Create(newEntity);
        return new ButterMorphFieldMetadataDesignerSaveResult { Succeeded = true, Message = "Metadata field saved." };
    }

    private async Task<bool> UpdateDefinitionAsync(Guid id, CustomFieldDefinition definition, DateTime now)
    {
        try
        {
            await metadataFields.UpdateDefinition(
                id,
                definition.Key.Trim(),
                definition.Name.Trim(),
                string.IsNullOrWhiteSpace(definition.Description) ? null : definition.Description.Trim(),
                definition.IsActive,
                now);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    private static void Apply(ContractFieldMetadataDefinition entity, CustomFieldDefinition definition, DateTime now)
    {
        entity.Name = definition.Name.Trim();
        entity.Key = definition.Key.Trim();
        entity.Description = string.IsNullOrWhiteSpace(definition.Description) ? null : definition.Description.Trim();
        entity.IsActive = definition.IsActive;
        entity.UpdatedAtUtc = now;
    }

    private async Task<ContractFieldMetadataDefinition?> LoadMetadataFieldAsync(Guid id)
    {
        return await metadataFields.GetById(id, includeVersions: true);
    }

    private async Task UpsertVersionAsync(Guid metadataFieldId, CustomFieldDefinition definition, string definitionJson, DateTime now)
    {
        var versionNumber = ResolveVersion(definition);
        await metadataFields.UpsertVersion(metadataFieldId, CreateVersion(metadataFieldId, definition, definitionJson, now));
    }

    private static void AddVersion(ContractFieldMetadataDefinition entity, CustomFieldDefinition definition, string definitionJson, DateTime now)
    {
        entity.Versions.Add(CreateVersion(entity.Id, definition, definitionJson, now));
    }

    private static ContractFieldMetadataVersion CreateVersion(Guid metadataFieldId, CustomFieldDefinition definition, string definitionJson, DateTime now)
    {
        return new ContractFieldMetadataVersion
        {
            ContractFieldMetadataDefinitionId = metadataFieldId,
            VersionNumber = ResolveVersion(definition),
            Comment = string.IsNullOrWhiteSpace(definition.VersionComment) ? null : definition.VersionComment.Trim(),
            DefinitionJson = definitionJson,
            IsActive = definition.IsActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    private static string ResolveVersion(CustomFieldDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.Version) ? "1.0.0" : definition.Version.Trim();
    }

    private static string NextVersion(string version)
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


