using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.WebUI.SchemaTypes;

namespace KnOwl.Tests;

public sealed class SchemaTypeCatalogTests
{
    [Fact]
    public async Task SelectableTypesIncludeSystemTime()
    {
        var schemaTypes = new FakeSchemaTypeInteractionService([
            CreateSystemType("Date", "{\"type\":\"string\",\"pattern\":\"^\\\\d{4}-\\\\d{2}-\\\\d{2}$\"}"),
            CreateSystemType("DateTime", "{\"type\":\"string\",\"pattern\":\"^\\\\d{4}-\\\\d{2}-\\\\d{2}T\"}"),
            CreateSystemType("Time", "{\"type\":\"string\",\"pattern\":\"^\\\\d{2}:\\\\d{2}:\\\\d{2}\"}"),
            CreateSystemType("TimeSpan", "{\"type\":\"string\",\"pattern\":\"^(?:\\\\d+\\\\.)?\\\\d{2}:\\\\d{2}:\\\\d{2}\"}")
        ]);

        var json = await SchemaTypeCatalog.GetSelectableTypesJsonAsync(schemaTypes);
        using var document = JsonDocument.Parse(json);
        var names = document.RootElement
            .EnumerateArray()
            .Select(x => x.GetProperty("name").GetString())
            .ToArray();

        Assert.Contains("Date", names);
        Assert.Contains("DateTime", names);
        Assert.Contains("Time", names);
        Assert.Contains("TimeSpan", names);
    }

    private static SchemaTypeVersion CreateSystemType(string name, string schemaJson)
    {
        SchemaTypeDefinition definition = new()
        {
            Id = Guid.NewGuid(),
            Key = name,
            Name = name,
            IsSystem = true,
            IsActive = true
        };

        return new SchemaTypeVersion
        {
            Id = Guid.NewGuid(),
            SchemaTypeDefinitionId = definition.Id,
            SchemaTypeDefinition = definition,
            VersionNumber = "1.0.0",
            DefinitionJson = $$"""
{"key":"{{name}}","name":"{{name}}","description":"System schema type: {{name}}","version":"1.0.0","baseType":"string","comment":"","schema":{{schemaJson}}}
""",
            IsActive = true
        };
    }

    private sealed class FakeSchemaTypeInteractionService(IReadOnlyList<SchemaTypeVersion> activeVersions) : ISchemaTypeInteractionService
    {
        public Task<IReadOnlyList<SchemaTypeDefinition>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SchemaTypeDefinition>>([]);
        public Task<IReadOnlyList<SchemaTypeVersion>> GetActiveVersions(CancellationToken cancellationToken = default) => Task.FromResult(activeVersions);
        public Task<SchemaTypeDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult<SchemaTypeDefinition?>(null);
        public Task<SchemaTypeVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult<SchemaTypeVersion?>(null);
        public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> VersionExists(Guid typeId, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task Create(SchemaTypeDefinition schemaType, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersion(Guid typeId, SchemaTypeVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetVersionActive(Guid typeId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
