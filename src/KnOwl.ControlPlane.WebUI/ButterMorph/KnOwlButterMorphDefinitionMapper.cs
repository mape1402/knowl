namespace KnOwl.ControlPlane.WebUI.ButterMorph;

using System.Globalization;
using System.Text.Json;
using global::ButterMorph.SchemaDesign;
using KnOwl.ControlPlane.WebUI.ContractMetadata;
using KnOwl.ControlPlane.Design.Core;
using KnOwlSchemaTypeDefinition = KnOwl.ControlPlane.Design.Core.SchemaTypeDefinition;
using ButterMorphSchemaTypeDefinition = global::ButterMorph.SchemaDesign.SchemaTypeDefinition;

/// <summary>
/// Maps persisted KnOwl contract records to ButterMorph designer definitions and catalog items.
/// </summary>
public static class KnOwlButterMorphDefinitionMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    /// <summary>
    /// Converts a persisted schema type version into a ButterMorph schema type definition.
    /// </summary>
    public static ButterMorphSchemaTypeDefinition ToDefinition(KnOwlSchemaTypeDefinition entity, SchemaTypeVersion version)
    {
        try
        {
            var definition = JsonSerializer.Deserialize<ButterMorphSchemaTypeDefinition>(version.DefinitionJson, JsonOptions);
            if (definition is not null)
            {
                definition.Key = string.IsNullOrWhiteSpace(definition.Key) ? entity.Key : definition.Key;
                definition.Name = string.IsNullOrWhiteSpace(definition.Name) ? entity.Name : definition.Name;
                definition.Description = string.IsNullOrWhiteSpace(definition.Description) ? entity.Description ?? string.Empty : definition.Description;
                definition.Version = string.IsNullOrWhiteSpace(definition.Version) ? version.VersionNumber : definition.Version;
                definition.Comment = string.IsNullOrWhiteSpace(definition.Comment) ? version.Comment ?? string.Empty : definition.Comment;
                definition.JsonSchema = definition.Schema.ValueKind == JsonValueKind.Undefined ? string.Empty : definition.Schema.GetRawText();
                return definition;
            }
        }
        catch (JsonException)
        {
        }

        return new ButterMorphSchemaTypeDefinition
        {
            Key = entity.Key,
            Name = entity.Name,
            Description = entity.Description ?? string.Empty,
            Version = version.VersionNumber,
            BaseType = "string",
            Comment = version.Comment ?? string.Empty,
            Schema = ParseElement("{}"),
            JsonSchema = "{}"
        };
    }

    /// <summary>
    /// Converts a persisted schema type version into a ButterMorph selector catalog item.
    /// </summary>
    public static SchemaTypeCatalogItem ToCatalogItem(KnOwlSchemaTypeDefinition entity, SchemaTypeVersion version, bool isSystem)
    {
        var definition = ToDefinition(entity, version);
        return new SchemaTypeCatalogItem
        {
            TypeId = definition.Key,
            TypeVersionId = $"{definition.Key}@{definition.Version}",
            Name = definition.Name,
            VersionNumber = definition.Version,
            BaseType = definition.BaseType,
            JsonSchema = definition.JsonSchema,
            IsSystem = isSystem
        };
    }

    /// <summary>
    /// Serializes a ButterMorph schema type definition for KnOwl persistence.
    /// </summary>
    public static string SerializeSchemaTypeDefinition(ButterMorphSchemaTypeDefinition definition)
    {
        return JsonSerializer.Serialize(definition, JsonOptions);
    }

    /// <summary>
    /// Converts a metadata field definition into its latest active ButterMorph custom field definition.
    /// </summary>
    public static CustomFieldDefinition ToDefinition(ContractFieldMetadataDefinition entity)
    {
        var latest = entity.Versions
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefault();

        return latest is null ? CreateDefaultFieldDefinition(entity) : ToDefinition(entity, latest);
    }

    /// <summary>
    /// Converts a specific metadata field version into a ButterMorph custom field definition.
    /// </summary>
    public static CustomFieldDefinition ToDefinition(ContractFieldMetadataDefinition entity, ContractFieldMetadataVersion version)
    {
        try
        {
            var definition = JsonSerializer.Deserialize<CustomFieldDefinition>(NormalizeCustomFieldDefinitionJson(version.DefinitionJson), JsonOptions);
            if (definition is not null)
            {
                definition.Key = string.IsNullOrWhiteSpace(definition.Key) ? entity.Key : definition.Key;
                definition.Name = string.IsNullOrWhiteSpace(definition.Name) ? entity.Name : definition.Name;
                definition.Description = string.IsNullOrWhiteSpace(definition.Description) ? entity.Description ?? string.Empty : definition.Description;
                definition.Version = string.IsNullOrWhiteSpace(definition.Version) ? version.VersionNumber : definition.Version;
                definition.VersionComment = string.IsNullOrWhiteSpace(definition.VersionComment) ? version.Comment ?? string.Empty : definition.VersionComment;
                definition.IsActive = entity.IsActive && definition.IsActive;
                return definition;
            }
        }
        catch (JsonException)
        {
        }

        return CreateDefaultFieldDefinition(entity);
    }

    /// <summary>
    /// Converts a metadata field version into a ButterMorph field metadata catalog item.
    /// </summary>
    public static FieldMetadataCatalogItem ToCatalogItem(ContractFieldMetadataDefinition entity, ContractFieldMetadataVersion version)
    {
        var definition = ToDefinition(entity, version);
        return new FieldMetadataCatalogItem
        {
            Id = entity.Id.ToString(),
            Key = definition.Key,
            Name = definition.Name,
            Description = definition.Description,
            Version = definition.Version,
            VersionComment = definition.VersionComment,
            DataType = definition.DataType,
            IsRequired = definition.IsRequired,
            Validation = SerializeDictionary(definition.Validation),
            AppliesToJson = JsonSerializer.Serialize(definition.AppliesTo ?? [], JsonOptions),
            ChildrenDefinitionJson = SerializeElement(definition.ChildrenDefinition),
            ArrayItemDataType = definition.ArrayItemDataType ?? string.Empty,
            ArrayItemDefinitionJson = SerializeElement(definition.ArrayItemDefinition)
        };
    }

    /// <summary>
    /// Serializes a ButterMorph custom field definition for KnOwl persistence.
    /// </summary>
    public static string SerializeCustomFieldDefinition(CustomFieldDefinition definition)
    {
        return NormalizeCustomFieldDefinitionJson(JsonSerializer.Serialize(definition, JsonOptions));
    }

    /// <summary>
    /// Serializes a ButterMorph payload schema definition for event or command version persistence.
    /// </summary>
    public static string GetSchemaJson(PayloadSchemaDefinition definition)
    {
        return JsonSerializer.Serialize(definition, JsonOptions);
    }

    /// <summary>
    /// Converts ButterMorph metadata scopes into KnOwl section keys.
    /// </summary>
    public static IReadOnlyCollection<string> ToKnOwlScopes(IReadOnlyCollection<string> butterMorphScopes)
    {
        List<string> scopes = [];
        if (butterMorphScopes.Contains("Schema", StringComparer.OrdinalIgnoreCase) || butterMorphScopes.Contains("Field", StringComparer.OrdinalIgnoreCase))
        {
            scopes.Add("events");
            scopes.Add("commands");
        }

        if (scopes.Count == 0)
        {
            scopes.Add("events");
            scopes.Add("commands");
        }

        return scopes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Converts KnOwl section keys into ButterMorph metadata scopes.
    /// </summary>
    public static IReadOnlyCollection<string> ToButterMorphScopes(string appliesToJson)
    {
        var knowlScopes = ContractFieldMetadataCatalog.ParseAppliesTo(appliesToJson);
        List<string> scopes = [];

        if (knowlScopes.Contains("events") || knowlScopes.Contains("commands"))
        {
            scopes.Add("Schema");
            scopes.Add("Field");
        }

        return scopes.Count == 0 ? ["Schema", "Field"] : scopes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Normalizes display text into a lowercase catalog key.
    /// </summary>
    public static string NormalizeKey(string value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "schema" : value.Trim();
        Span<char> buffer = stackalloc char[text.Length];
        var index = 0;
        var previousWasDash = false;

        foreach (var character in text)
        {
            if (char.IsLetterOrDigit(character))
            {
                buffer[index++] = char.ToLower(character, CultureInfo.InvariantCulture);
                previousWasDash = false;
            }
            else if (!previousWasDash)
            {
                buffer[index++] = '-';
                previousWasDash = true;
            }
        }

        var result = new string(buffer[..index]).Trim('-');
        return string.IsNullOrWhiteSpace(result) ? "schema" : result;
    }

    /// <summary>
    /// Parses JSON into a detached JSON element for durable mapping results.
    /// </summary>
    public static JsonElement ParseElement(string json)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        return document.RootElement.Clone();
    }

    private static IReadOnlyDictionary<string, JsonElement> ParseDictionary(string json)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, JsonElement>();
        }

        return document.RootElement.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.Clone());
    }

    private static CustomFieldDefinition CreateDefaultFieldDefinition(ContractFieldMetadataDefinition entity)
    {
        return new CustomFieldDefinition
        {
            Key = entity.Key,
            Name = entity.Name,
            Description = entity.Description ?? string.Empty,
            Version = "1.0.0",
            VersionComment = string.Empty,
            DataType = "string",
            AppliesTo = ["Field"],
            IsActive = entity.IsActive
        };
    }

    private static string SerializeDictionary(IReadOnlyDictionary<string, JsonElement> value)
    {
        if (value == null || value.Count == 0)
        {
            return string.Empty;
        }

        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static string SerializeElement(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Undefined ? string.Empty : element.GetRawText();
    }

    private static string NormalizeCustomFieldDefinitionJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "{}";
        }

        using JsonDocument document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return json;
        }

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = false });
        writer.WriteStartObject();
        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            if (ShouldNormalizeJsonStringProperty(property.Name) &&
                property.Value.ValueKind == JsonValueKind.String &&
                TryWriteJsonStringProperty(writer, property))
            {
                continue;
            }

            property.WriteTo(writer);
        }

        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static bool ShouldNormalizeJsonStringProperty(string propertyName)
    {
        return string.Equals(propertyName, "validation", StringComparison.Ordinal) ||
            string.Equals(propertyName, "childrenDefinition", StringComparison.Ordinal) ||
            string.Equals(propertyName, "arrayItemDefinition", StringComparison.Ordinal);
    }

    private static bool TryWriteJsonStringProperty(Utf8JsonWriter writer, JsonProperty property)
    {
        var raw = property.Value.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        try
        {
            using JsonDocument nested = JsonDocument.Parse(raw);
            if (nested.RootElement.ValueKind != JsonValueKind.Object &&
                nested.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            writer.WritePropertyName(property.Name);
            nested.RootElement.WriteTo(writer);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

