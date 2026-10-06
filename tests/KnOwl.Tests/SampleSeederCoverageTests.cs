using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.Tests;

public sealed class SampleSeederCoverageTests
{
    [Fact]
    public async Task DocumentationSeederBuildsExpectedZipPackage()
    {
        var type = Type.GetType("KnOwl.ControlPlaneHost.Sample.Documentation.SampleDocumentationSeeder, KnOwl.ControlPlaneHost.Sample")
            ?? throw new InvalidOperationException("Sample documentation seeder type was not found.");
        var method = type.GetMethod("CreateDocumentationPackage", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("CreateDocumentationPackage method was not found.");

        await using var stream = Assert.IsType<MemoryStream>(method.Invoke(null, null));
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        var markdown = zip.GetEntry("docs/index.md");
        var asset = zip.GetEntry("docs/assets/flow.svg");

        Assert.NotNull(markdown);
        Assert.NotNull(asset);
        using var reader = new StreamReader(markdown.Open());
        var content = await reader.ReadToEndAsync();
        Assert.Contains("Control Plane Guide", content);
        Assert.Contains("```mermaid", content);
    }

    [Fact]
    public async Task DesignSeederCreatesAndSkipsExistingSampleDefinitions()
    {
        var type = Type.GetType("KnOwl.ControlPlaneHost.Sample.Design.SampleDesignSeeder, KnOwl.ControlPlaneHost.Sample")
            ?? throw new InvalidOperationException("Sample design seeder type was not found.");
        var ensureSchemaType = type.GetMethod("EnsureSchemaType", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("EnsureSchemaType method was not found.");
        var ensureMetadataField = type.GetMethod("EnsureMetadataField", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("EnsureMetadataField method was not found.");
        var createTypeVersion = type.GetMethod("CreateTypeVersion", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("CreateTypeVersion method was not found.");
        var createMetadataVersion = type.GetMethod("CreateMetadataVersion", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("CreateMetadataVersion method was not found.");

        var schemaService = new SchemaTypeServiceFake();
        var metadataService = new MetadataFieldServiceFake();
        var typeVersions = new[]
        {
            (SchemaTypeVersion)createTypeVersion.Invoke(null, ["1.0.0", "Initial", "string", "{\"type\":\"string\"}"])!,
            (SchemaTypeVersion)createTypeVersion.Invoke(null, ["1.1.0", "Array item", "array", "{\"type\":\"array\",\"items\":{\"type\":\"string\"}}"])!
        };
        var metadataVersions = new[]
        {
            (ContractFieldMetadataVersion)createMetadataVersion.Invoke(null, ["1.0.0", "Initial", "string", true, new[] { "Schema" }, "{\"maxLength\":20}"])!
        };

        await InvokeTask(ensureSchemaType, schemaService, "customer-reference", "Customer Reference", "Reference", typeVersions);
        await InvokeTask(ensureSchemaType, schemaService, "customer-reference", "Customer Reference", "Reference", typeVersions);
        await InvokeTask(ensureMetadataField, metadataService, "classification", "Classification", "PII", metadataVersions, true);
        await InvokeTask(ensureMetadataField, metadataService, "classification", "Classification", "PII", metadataVersions, false);
        await InvokeTask(ensureMetadataField, metadataService, "classification", "Classification", "PII", metadataVersions, true);

        var schema = Assert.Single(schemaService.Definitions);
        Assert.Equal(2, schema.Versions.Count);
        using var schemaJson = JsonDocument.Parse(schema.Versions.First().DefinitionJson);
        Assert.Equal("string", schemaJson.RootElement.GetProperty("baseType").GetString());

        var metadata = Assert.Single(metadataService.Definitions);
        Assert.Single(metadata.Versions);
        using var metadataJson = JsonDocument.Parse(metadata.Versions.First().DefinitionJson);
        Assert.True(metadataJson.RootElement.GetProperty("isRequired").GetBoolean());
        Assert.Equal(2, metadataService.UpsertCalls);
    }

    private static async Task InvokeTask(MethodInfo method, params object?[] arguments)
    {
        var task = (Task?)method.Invoke(null, arguments)
            ?? throw new InvalidOperationException($"{method.Name} did not return a task.");
        await task;
    }

    private sealed class SchemaTypeServiceFake : ISchemaTypeInteractionService
    {
        public List<SchemaTypeDefinition> Definitions { get; } = [];

        public Task<IReadOnlyList<SchemaTypeDefinition>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SchemaTypeDefinition>>(Definitions);

        public Task<IReadOnlyList<SchemaTypeVersion>> GetActiveVersions(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SchemaTypeVersion>>(Definitions.SelectMany(x => x.Versions).Where(x => x.IsActive).ToArray());

        public Task<SchemaTypeDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
            => Task.FromResult(Definitions.FirstOrDefault(x => x.Id == id));

        public Task<SchemaTypeVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult(Definitions.SelectMany(x => x.Versions).FirstOrDefault(x => x.Id == versionId));

        public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Definitions.Any(x => x.Key == key && (!excludingId.HasValue || x.Id != excludingId)));

        public Task<bool> VersionExists(Guid typeId, string versionNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(Definitions.Single(x => x.Id == typeId).Versions.Any(x => x.VersionNumber == versionNumber));

        public Task Create(SchemaTypeDefinition schemaType, CancellationToken cancellationToken = default)
        {
            Definitions.Add(schemaType);
            return Task.CompletedTask;
        }

        public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddVersion(Guid typeId, SchemaTypeVersion version, CancellationToken cancellationToken = default)
        {
            var definition = Definitions.Single(x => x.Id == typeId);
            version.SchemaTypeDefinitionId = typeId;
            definition.Versions.Add(version);
            return Task.CompletedTask;
        }

        public Task SetVersionActive(Guid typeId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class MetadataFieldServiceFake : IContractFieldMetadataInteractionService
    {
        public List<ContractFieldMetadataDefinition> Definitions { get; } = [];
        public int UpsertCalls { get; private set; }

        public Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ContractFieldMetadataDefinition>>(Definitions);

        public Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetActive(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ContractFieldMetadataDefinition>>(Definitions.Where(x => x.IsActive).ToArray());

        public Task<ContractFieldMetadataDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
            => Task.FromResult(Definitions.FirstOrDefault(x => x.Id == id));

        public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Definitions.Any(x => x.Key == key && (!excludingId.HasValue || x.Id != excludingId)));

        public Task<bool> VersionExists(Guid metadataFieldId, string versionNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(Definitions.Single(x => x.Id == metadataFieldId).Versions.Any(x => x.VersionNumber == versionNumber));

        public Task Create(ContractFieldMetadataDefinition metadataField, CancellationToken cancellationToken = default)
        {
            Definitions.Add(metadataField);
            return Task.CompletedTask;
        }

        public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpsertVersion(Guid metadataFieldId, ContractFieldMetadataVersion version, CancellationToken cancellationToken = default)
        {
            UpsertCalls++;
            var definition = Definitions.Single(x => x.Id == metadataFieldId);
            version.ContractFieldMetadataDefinitionId = metadataFieldId;
            var index = definition.Versions.ToList().FindIndex(x => x.VersionNumber == version.VersionNumber);
            if (index >= 0)
            {
                definition.Versions.Remove(definition.Versions.ElementAt(index));
            }

            definition.Versions.Add(version);
            return Task.CompletedTask;
        }

        public Task SetVersionActive(Guid metadataFieldId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
