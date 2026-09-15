using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.WebUI.SchemaTypes;

public static class SchemaTypeCatalog
{
    public static readonly string[] BasicTypes = ["string", "number", "integer", "boolean", "object", "array"];
    public static readonly string[] SystemSemanticTypes = ["Date", "DateTime", "Time", "TimeSpan"];

    public static async Task<string> GetSelectableTypesJsonAsync(ISchemaTypeInteractionService schemaTypes)
    {
        var versions = await schemaTypes.GetActiveVersions();
        var customVersions = versions
            .Where(x => x.SchemaTypeDefinition is { IsSystem: false })
            .OrderBy(x => x.SchemaTypeDefinition!.Name)
            .ThenByDescending(x => x.CreatedAtUtc);

        var customTypes = customVersions
            .Where(x => x.SchemaTypeDefinition is not null)
            .Select(x => CreateSelectableType(x, false));

        var basicTypes = BasicTypes.Select(type => new SelectableSchemaType(
            null,
            null,
            type,
            "1.0.0",
            type,
            $"{{\"type\":\"{type}\"}}",
            IsSystem: true));

        var systemVersions = versions
            .Where(x => x.SchemaTypeDefinition is { IsSystem: true } &&
                        SystemSemanticTypes.Contains(x.SchemaTypeDefinition.Name))
            .OrderBy(x => x.SchemaTypeDefinition!.Name);

        var systemTypes = systemVersions
            .Where(x => x.SchemaTypeDefinition is not null)
            .Select(x => CreateSelectableType(x, true));

        return JsonSerializer.Serialize(basicTypes.Concat(systemTypes).Concat(customTypes), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public static bool IsBasicType(string value) => BasicTypes.Contains(value);

    private static SelectableSchemaType CreateSelectableType(SchemaTypeVersion version, bool isSystem)
    {
        var definition = ReadDefinition(version.DefinitionJson);
        return new SelectableSchemaType(
            version.SchemaTypeDefinition!.Id,
            version.Id,
            string.IsNullOrWhiteSpace(definition.Name) ? version.SchemaTypeDefinition.Name : definition.Name,
            string.IsNullOrWhiteSpace(definition.Version) ? version.VersionNumber : definition.Version,
            string.IsNullOrWhiteSpace(definition.BaseType) ? "string" : definition.BaseType,
            definition.SchemaJson,
            isSystem);
    }

    private static SchemaTypeDefinitionSnapshot ReadDefinition(string definitionJson)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(definitionJson) ? "{}" : definitionJson);
            var root = document.RootElement;
            var schema = root.TryGetProperty("schema", out var schemaElement) ? schemaElement.GetRawText() : "{}";
            return new SchemaTypeDefinitionSnapshot(
                ReadString(root, "name"),
                ReadString(root, "version"),
                ReadString(root, "baseType"),
                schema);
        }
        catch (JsonException)
        {
            return new SchemaTypeDefinitionSnapshot(string.Empty, string.Empty, "string", "{}");
        }
    }

    private static string ReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) ? element.ToString() : string.Empty;
    }

    public sealed record SelectableSchemaType(
        Guid? TypeId,
        Guid? TypeVersionId,
        string Name,
        string VersionNumber,
        string BaseType,
        string JsonSchema,
        bool IsSystem);

    private sealed record SchemaTypeDefinitionSnapshot(string Name, string Version, string BaseType, string SchemaJson);
}
