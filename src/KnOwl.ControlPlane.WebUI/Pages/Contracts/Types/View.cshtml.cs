using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Types;

public class ViewModel(ISchemaTypeInteractionService schemaTypes) : PageModel
{
    public SchemaTypeDefinition? TypeDefinition { get; private set; }
    public SchemaTypeVersion? SelectedVersion { get; private set; }
    public IReadOnlyList<SchemaTypeVersion> Versions { get; private set; } = [];
    public string FormattedJsonSchema { get; private set; } = "{}";
    public TypeDefinitionView SelectedDefinition { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, string? version = null, CancellationToken cancellationToken = default)
    {
        TypeDefinition = await schemaTypes.GetById(id, includeVersions: true, cancellationToken);

        if (TypeDefinition is null)
        {
            return NotFound();
        }

        SelectedVersion = string.IsNullOrWhiteSpace(version)
            ? TypeDefinition.Versions.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault()
            : TypeDefinition.Versions.FirstOrDefault(x => x.VersionNumber == version);
        Search = Normalize(Search);
        Versions = (string.IsNullOrWhiteSpace(Search)
                ? TypeDefinition.Versions
                : TypeDefinition.Versions.Where(MatchesSearch))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArray();

        if (SelectedVersion is not null)
        {
            SelectedDefinition = ReadTypeDefinition(SelectedVersion.DefinitionJson);
            FormattedJsonSchema = FormatJson(SelectedDefinition.SchemaJson);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDeactivateVersionAsync(Guid id, Guid versionId, CancellationToken cancellationToken)
    {
        try
        {
            await schemaTypes.SetVersionActive(id, versionId, isActive: false, DateTime.UtcNow, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
        }

        return RedirectToPage(new { id });
    }

    private static string FormatJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return json;
        }
    }

    public static TypeDefinitionView ReadTypeDefinition(string definitionJson)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(definitionJson) ? "{}" : definitionJson);
            var root = document.RootElement;
            return new TypeDefinitionView
            {
                BaseType = ReadString(root, "baseType"),
                SchemaJson = root.TryGetProperty("schema", out var schema) ? schema.GetRawText() : "{}"
            };
        }
        catch (JsonException)
        {
            return new TypeDefinitionView();
        }
    }

    private static string ReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) ? element.ToString() : string.Empty;
    }

    private bool MatchesSearch(SchemaTypeVersion version)
        => Contains(version.VersionNumber)
            || Contains(version.Comment)
            || Contains(version.IsActive ? "Active" : "Inactive")
            || Contains(version.DefinitionJson);

    private bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(Search)
            && !string.IsNullOrWhiteSpace(value)
            && value.Contains(Search, StringComparison.OrdinalIgnoreCase);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed class TypeDefinitionView
    {
        public string BaseType { get; set; } = string.Empty;

        public string SchemaJson { get; set; } = "{}";
    }
}
