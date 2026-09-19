using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlaneHost.Mongo.Sample.Design;

internal static class SampleDesignSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task Initialize(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var schemaTypes = scope.ServiceProvider.GetRequiredService<ISchemaTypeInteractionService>();
        var metadataFields = scope.ServiceProvider.GetRequiredService<IContractFieldMetadataInteractionService>();

        await EnsureSchemaType(
            schemaTypes,
            "sample-customer-reference",
            "Sample Customer Reference",
            "Editable sample type used to validate data type versioning.",
            [
                CreateTypeVersion("1.0.0", "Initial customer reference format.", "string", "{\"type\":\"string\",\"minLength\":6,\"maxLength\":24}"),
                CreateTypeVersion("1.0.1", "Adds the expected customer reference prefix.", "string", "{\"type\":\"string\",\"minLength\":6,\"maxLength\":24,\"pattern\":\"^CUST-[0-9]{4,}$\"}")
            ]);

        await EnsureSchemaType(
            schemaTypes,
            "sample-shipment-window",
            "Sample Shipment Window",
            "Editable object type with multiple versions for UI validation.",
            [
                CreateTypeVersion("1.0.0", "Initial shipment window object.", "object", "{\"type\":\"object\",\"properties\":{\"start\":{\"type\":\"string\",\"format\":\"date-time\"},\"end\":{\"type\":\"string\",\"format\":\"date-time\"}},\"required\":[\"start\",\"end\"]}"),
                CreateTypeVersion("1.1.0", "Adds the optional time zone field.", "object", "{\"type\":\"object\",\"properties\":{\"start\":{\"type\":\"string\",\"format\":\"date-time\"},\"end\":{\"type\":\"string\",\"format\":\"date-time\"},\"timeZone\":{\"type\":\"string\"}},\"required\":[\"start\",\"end\"]}")
            ]);

        await EnsureMetadataField(
            metadataFields,
            "sample-pii-classification",
            "Sample PII Classification",
            "Editable custom field used to validate custom field versioning.",
            [
                CreateMetadataVersion("1.0.0", "Initial classification values.", "string", true, ["Schema", "Field"], "{\"enum\":[\"none\",\"internal\",\"restricted\"]}"),
                CreateMetadataVersion("1.1.0", "Adds confidential as a supported value.", "string", true, ["Schema", "Field"], "{\"enum\":[\"none\",\"internal\",\"restricted\",\"confidential\"]}")
            ]);

        await EnsureMetadataField(
            metadataFields,
            "sample-source-system",
            "Sample Source System",
            "Optional custom field that identifies the upstream system.",
            [
                CreateMetadataVersion("1.0.0", "Initial source system metadata.", "string", false, ["Schema"], "{\"maxLength\":80}"),
                CreateMetadataVersion("1.0.1", "Tightens the source system key length.", "string", false, ["Schema"], "{\"maxLength\":48}")
            ]);
    }

    private static async Task EnsureSchemaType(
        ISchemaTypeInteractionService schemaTypes,
        string key,
        string name,
        string description,
        IReadOnlyCollection<SchemaTypeVersion> versions)
    {
        var existing = (await schemaTypes.GetAll())
            .FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            existing = new SchemaTypeDefinition
            {
                Key = key,
                Name = name,
                Description = description,
                IsActive = true,
                IsSystem = false
            };
            await schemaTypes.Create(existing);
        }

        foreach (var version in versions)
        {
            if (!await schemaTypes.VersionExists(existing.Id, version.VersionNumber))
            {
                await schemaTypes.AddVersion(existing.Id, version);
            }
        }
    }

    private static async Task EnsureMetadataField(
        IContractFieldMetadataInteractionService metadataFields,
        string key,
        string name,
        string description,
        IReadOnlyCollection<ContractFieldMetadataVersion> versions)
    {
        var existing = (await metadataFields.GetAll())
            .FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            existing = new ContractFieldMetadataDefinition
            {
                Key = key,
                Name = name,
                Description = description,
                IsActive = true
            };
            await metadataFields.Create(existing);
        }

        foreach (var version in versions)
        {
            if (!await metadataFields.VersionExists(existing.Id, version.VersionNumber))
            {
                await metadataFields.UpsertVersion(existing.Id, version);
            }
        }
    }

    private static SchemaTypeVersion CreateTypeVersion(string version, string comment, string baseType, string schemaJson)
        => new()
        {
            VersionNumber = version,
            Comment = comment,
            DefinitionJson = CreateTypeDefinitionJson(version, comment, baseType, schemaJson),
            IsActive = true
        };

    private static ContractFieldMetadataVersion CreateMetadataVersion(
        string version,
        string comment,
        string dataType,
        bool isRequired,
        IReadOnlyCollection<string> appliesTo,
        string validationJson)
        => new()
        {
            VersionNumber = version,
            Comment = comment,
            DefinitionJson = CreateMetadataDefinitionJson(version, comment, dataType, isRequired, appliesTo, validationJson),
            IsActive = true
        };

    private static string CreateTypeDefinitionJson(string version, string comment, string baseType, string schemaJson)
    {
        using var schema = JsonDocument.Parse(schemaJson);
        return JsonSerializer.Serialize(new
        {
            version,
            baseType,
            comment,
            schema = schema.RootElement
        }, JsonOptions);
    }

    private static string CreateMetadataDefinitionJson(
        string version,
        string comment,
        string dataType,
        bool isRequired,
        IReadOnlyCollection<string> appliesTo,
        string validationJson)
    {
        using var validation = JsonDocument.Parse(validationJson);
        return JsonSerializer.Serialize(new
        {
            version,
            versionComment = comment,
            dataType,
            isRequired,
            appliesTo,
            validation = validation.RootElement,
            isActive = true
        }, JsonOptions);
    }
}

