using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.WebUI.ButterMorph;

namespace KnOwl.ControlPlane.WebUI.ContractMetadata;

public static class ContractFieldMetadataCatalog
{
    public static readonly string[] DataTypes = ["string", "number", "integer", "boolean", "date"];
    public static readonly string[] Sections = ["events", "commands"];

    public static async Task<string> GetApplicableMetadataJsonAsync(IContractFieldMetadataInteractionService metadataFields, string section)
    {
        var normalizedSection = section.Trim().ToLowerInvariant();
        var definitions = await metadataFields.GetActive();

        var applicable = definitions
            .Select(x => new
            {
                Entity = x,
                Version = x.Versions
                    .Where(version => version.IsActive)
                    .OrderByDescending(version => version.CreatedAtUtc)
                    .FirstOrDefault()
            })
            .Where(x => x.Version is not null)
            .Select(x => new
            {
                x.Entity,
                Definition = KnOwlButterMorphDefinitionMapper.ToDefinition(x.Entity, x.Version!)
            })
            .Where(x => x.Definition.AppliesTo.Count == 0 || x.Definition.AppliesTo.Contains(normalizedSection, StringComparer.OrdinalIgnoreCase))
            .Select(x => new SelectableMetadataField(
                x.Entity.Id,
                x.Definition.Name,
                x.Definition.Key,
                x.Definition.Description,
                x.Definition.DataType,
                x.Definition.IsRequired,
                x.Definition.Validation ?? new Dictionary<string, JsonElement>()))
            .ToList();

        return JsonSerializer.Serialize(applicable, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public static IReadOnlyList<string> ParseAppliesTo(string? appliesToJson)
    {
        try
        {
            var values = JsonSerializer.Deserialize<List<string>>(appliesToJson ?? "[]") ?? [];
            return values
                .Select(x => x.Trim().ToLowerInvariant())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public sealed record SelectableMetadataField(
        Guid Id,
        string Name,
        string Key,
        string? Description,
        string DataType,
        bool IsRequired,
        object Validation);
}
