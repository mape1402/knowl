using System.Text.Json;
using System.Text.Json.Nodes;
using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.WebUI.SchemaTypes;

public static class SchemaTypeSchemaBuilder
{
    public static string Build(TypeVersionInput input, SchemaTypeVersion? arrayItemVersion = null)
    {
        var schema = new Dictionary<string, object?>
        {
            ["type"] = input.BaseType
        };

        switch (input.BaseType)
        {
            case "string":
                AddIfValue(schema, "minLength", input.MinLength);
                AddIfValue(schema, "maxLength", input.MaxLength);
                AddIfText(schema, "pattern", input.Pattern);
                AddEnum(schema, input, JsonValueKind.String);
                break;
            case "number":
                AddIfValue(schema, "precision", input.Precision);
                AddIfValue(schema, "scale", input.Scale);
                AddIfValue(schema, "minimum", input.Minimum);
                AddIfValue(schema, "maximum", input.Maximum);
                AddEnum(schema, input, JsonValueKind.Number);
                break;
            case "integer":
                AddIfValue(schema, "minimum", input.Minimum);
                AddIfValue(schema, "maximum", input.Maximum);
                AddEnum(schema, input, JsonValueKind.Number);
                break;
            case "object":
                return NormalizeJson(input.PayloadSchemaJson, "object");
            case "array":
                schema["items"] = BuildArrayItems(input, arrayItemVersion, schema);
                AddIfValue(schema, "minItems", input.MinItems);
                AddIfValue(schema, "maxItems", input.MaxItems);
                break;
        }

        return JsonSerializer.Serialize(schema);
    }

    public static void Hydrate(TypeVersionInput input, string jsonSchema)
    {
        using var document = JsonDocument.Parse(jsonSchema);
        var root = document.RootElement;

        if (root.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String)
        {
            input.BaseType = type.GetString() ?? input.BaseType;
        }

        input.PayloadSchemaJson = input.BaseType == "object" ? jsonSchema : "{\"type\":\"object\",\"properties\":{}}";

        if (root.TryGetProperty("minLength", out var minLength) && minLength.TryGetInt32(out var minLengthValue)) input.MinLength = minLengthValue;
        if (root.TryGetProperty("maxLength", out var maxLength) && maxLength.TryGetInt32(out var maxLengthValue)) input.MaxLength = maxLengthValue;
        if (root.TryGetProperty("pattern", out var pattern) && pattern.ValueKind == JsonValueKind.String) input.Pattern = pattern.GetString();
        if (root.TryGetProperty("enum", out var enumValues) && enumValues.ValueKind == JsonValueKind.Array) input.AllowedValuesJson = enumValues.GetRawText();
        if (root.TryGetProperty("precision", out var precision) && precision.TryGetInt32(out var precisionValue)) input.Precision = precisionValue;
        if (root.TryGetProperty("scale", out var scale) && scale.TryGetInt32(out var scaleValue)) input.Scale = scaleValue;
        if (root.TryGetProperty("minimum", out var minimum) && minimum.TryGetDecimal(out var minimumValue)) input.Minimum = minimumValue;
        if (root.TryGetProperty("maximum", out var maximum) && maximum.TryGetDecimal(out var maximumValue)) input.Maximum = maximumValue;
        if (root.TryGetProperty("minItems", out var minItems) && minItems.TryGetInt32(out var minItemsValue)) input.MinItems = minItemsValue;
        if (root.TryGetProperty("maxItems", out var maxItems) && maxItems.TryGetInt32(out var maxItemsValue)) input.MaxItems = maxItemsValue;
        if (root.TryGetProperty("items", out var items))
        {
            if (items.TryGetProperty("typeVersionId", out var versionId) && versionId.TryGetGuid(out var versionGuid))
            {
                input.ArrayItemTypeVersionId = versionGuid;
                input.ArrayItemType = versionGuid.ToString();
            }
            else if (items.TryGetProperty("type", out var itemType) && itemType.ValueKind == JsonValueKind.String)
            {
                input.ArrayItemType = itemType.GetString() ?? "string";
            }
        }
    }

    public static void Validate(TypeVersionInput input, Action<string> addError)
    {
        if (!SchemaTypeCatalog.BasicTypes.Contains(input.BaseType))
        {
            addError("Invalid base type.");
        }

        if (input.MinLength.HasValue && input.MinLength < 0) addError("MinLength no puede ser negativo.");
        if (input.MaxLength.HasValue && input.MaxLength < 0) addError("MaxLength no puede ser negativo.");
        if (input.MinLength.HasValue && input.MaxLength.HasValue && input.MinLength > input.MaxLength) addError("MinLength no puede ser mayor que MaxLength.");
        ValidateEnum(input, addError);
        if (input.Precision.HasValue && input.Precision <= 0) addError("Precision debe ser mayor que cero.");
        if (input.Scale.HasValue && input.Scale < 0) addError("Scale no puede ser negativo.");
        if (input.Precision.HasValue && input.Scale.HasValue && input.Scale > input.Precision) addError("Scale no puede ser mayor que Precision.");
        if (input.MinItems.HasValue && input.MinItems < 0) addError("MinItems no puede ser negativo.");
        if (input.MaxItems.HasValue && input.MaxItems < 0) addError("MaxItems no puede ser negativo.");
        if (input.MinItems.HasValue && input.MaxItems.HasValue && input.MinItems > input.MaxItems) addError("MinItems no puede ser mayor que MaxItems.");
        if (input.BaseType == "array" && string.IsNullOrWhiteSpace(input.ArrayItemType)) addError("Select the array item type.");
        if (input.BaseType == "array" && !input.ArrayItemTypeVersionId.HasValue && !SchemaTypeCatalog.BasicTypes.Contains(input.ArrayItemType))
        {
            addError("Select a valid array item type.");
        }

        if (input.BaseType == "object")
        {
            try
            {
                Build(input);
            }
            catch (JsonException)
            {
                addError("The JSON schema is not valid.");
            }
            catch (InvalidOperationException ex)
            {
                addError(ex.Message);
            }
        }
    }

    private static object BuildArrayItems(TypeVersionInput input, SchemaTypeVersion? itemVersion, IDictionary<string, object?> parentSchema)
    {
        if (itemVersion?.SchemaTypeDefinition is not null)
        {
            var refName = $"{itemVersion.SchemaTypeDefinition.Name}@{itemVersion.VersionNumber}";
            var item = new Dictionary<string, object?>
            {
                ["$ref"] = $"#/$defs/{refName}",
                ["typeId"] = itemVersion.SchemaTypeDefinitionId,
                ["typeVersionId"] = itemVersion.Id
            };

            var defs = new Dictionary<string, object?>();
            using var document = JsonDocument.Parse(ReadSchemaJson(itemVersion.DefinitionJson));
            AddDefinition(defs, refName, document.RootElement);
            parentSchema["$defs"] = defs;
            return item;
        }

        return new Dictionary<string, object?>
        {
            ["type"] = input.ArrayItemType
        };
    }

    private static string ReadSchemaJson(string definitionJson)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(definitionJson) ? "{}" : definitionJson);
            if (document.RootElement.TryGetProperty("schema", out var schema))
            {
                return schema.GetRawText();
            }
        }
        catch (JsonException)
        {
        }

        return "{}";
    }

    private static void AddDefinition(IDictionary<string, object?> defs, string refName, JsonElement schema)
    {
        if (!defs.ContainsKey(refName))
        {
            defs[refName] = CloneSchemaWithoutDefs(schema);
        }

        if (!schema.TryGetProperty("$defs", out var nestedDefs) || nestedDefs.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var nestedDef in nestedDefs.EnumerateObject())
        {
            AddDefinition(defs, nestedDef.Name, nestedDef.Value);
        }
    }

    private static JsonElement CloneSchemaWithoutDefs(JsonElement schema)
    {
        var node = JsonNode.Parse(schema.GetRawText());
        NormalizeRequiredFields(node);

        if (node is JsonObject schemaObject)
        {
            schemaObject.Remove("$defs");
        }

        using var document = JsonDocument.Parse(node?.ToJsonString() ?? "{}");
        return document.RootElement.Clone();
    }

    private static void NormalizeRequiredFields(JsonNode? node)
    {
        if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                NormalizeRequiredFields(item);
            }

            return;
        }

        if (node is not JsonObject schemaObject)
        {
            return;
        }

        foreach (var property in schemaObject.ToList())
        {
            NormalizeRequiredFields(property.Value);
        }

        if (schemaObject["properties"] is not JsonObject properties ||
            schemaObject["required"] is not JsonArray requiredFields)
        {
            return;
        }

        foreach (var requiredField in requiredFields)
        {
            var fieldName = requiredField?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(fieldName) && properties[fieldName] is JsonObject fieldDefinition)
            {
                fieldDefinition["required"] = true;
            }
        }

        schemaObject.Remove("required");
    }

    private static void AddIfValue<T>(IDictionary<string, object?> schema, string key, T? value) where T : struct
    {
        if (value.HasValue)
        {
            schema[key] = value.Value;
        }
    }

    private static void AddIfText(IDictionary<string, object?> schema, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            schema[key] = value.Trim();
        }
    }

    private static void AddEnum(IDictionary<string, object?> schema, TypeVersionInput input, JsonValueKind expectedKind)
    {
        var values = ParseEnum(input, expectedKind);
        if (values.Count > 0)
        {
            schema["enum"] = values;
        }
    }

    private static List<object> ParseEnum(TypeVersionInput input, JsonValueKind expectedKind)
    {
        if (string.IsNullOrWhiteSpace(input.AllowedValuesJson))
        {
            return [];
        }

        using var document = JsonDocument.Parse(input.AllowedValuesJson);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var values = new List<object>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (expectedKind == JsonValueKind.String && item.ValueKind == JsonValueKind.String)
            {
                var value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value.Trim());
                }
            }

            if (expectedKind == JsonValueKind.Number && item.ValueKind == JsonValueKind.Number)
            {
                values.Add(input.BaseType == "integer" && item.TryGetInt32(out var intValue)
                    ? intValue
                    : item.GetDecimal());
            }
        }

        return values.Distinct().ToList();
    }

    private static void ValidateEnum(TypeVersionInput input, Action<string> addError)
    {
        if (string.IsNullOrWhiteSpace(input.AllowedValuesJson) || input.AllowedValuesJson == "[]")
        {
            return;
        }

        if (input.BaseType is not ("string" or "number" or "integer"))
        {
            addError("La lista de valores solo aplica para string, number o integer.");
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(input.AllowedValuesJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                addError("La lista de valores debe ser un arreglo JSON.");
                return;
            }

            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (input.BaseType == "string" && item.ValueKind != JsonValueKind.String)
                {
                    addError("La lista de valores para string solo acepta texto.");
                    return;
                }

                if (input.BaseType is "number" or "integer" && item.ValueKind != JsonValueKind.Number)
                {
                    addError("La lista de valores para number/integer solo acepta numeros.");
                    return;
                }

                if (input.BaseType == "integer" && !item.TryGetInt32(out _))
                {
                    addError("La lista de valores para integer solo acepta enteros.");
                    return;
                }
            }
        }
        catch (JsonException)
        {
            addError("The values list is not valid JSON.");
        }
    }

    private static string NormalizeJson(string? json, string expectedType)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        if (!document.RootElement.TryGetProperty("type", out var type) || type.GetString() != expectedType)
        {
            throw new InvalidOperationException($"El schema debe tener type '{expectedType}'.");
        }

        return JsonSerializer.Serialize(document.RootElement);
    }
}

