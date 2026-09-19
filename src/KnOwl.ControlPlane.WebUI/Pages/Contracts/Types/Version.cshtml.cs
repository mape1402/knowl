using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Types;

public class VersionModel(ISchemaTypeInteractionService schemaTypes) : PageModel
{
    public SchemaTypeDefinition? TypeDefinition { get; private set; }
    public SchemaTypeVersion? Version { get; private set; }
    public ViewModel.TypeDefinitionView SelectedDefinition { get; private set; } = new();
    public string FormattedJsonSchema { get; private set; } = "{}";

    public async Task<IActionResult> OnGetAsync(Guid id, Guid versionId, CancellationToken cancellationToken)
    {
        var result = await Load(id, versionId, cancellationToken);
        return result ?? Page();
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

        return RedirectToPage(new { id, versionId });
    }

    private async Task<IActionResult?> Load(Guid id, Guid versionId, CancellationToken cancellationToken)
    {
        TypeDefinition = await schemaTypes.GetById(id, includeVersions: true, cancellationToken);
        if (TypeDefinition is null)
        {
            return NotFound();
        }

        Version = TypeDefinition.Versions.FirstOrDefault(x => x.Id == versionId);
        if (Version is null)
        {
            return NotFound();
        }

        SelectedDefinition = ViewModel.ReadTypeDefinition(Version.DefinitionJson);
        FormattedJsonSchema = FormatJson(SelectedDefinition.SchemaJson);
        return null;
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
}
