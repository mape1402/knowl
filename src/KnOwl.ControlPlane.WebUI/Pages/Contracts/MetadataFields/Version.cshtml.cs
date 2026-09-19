using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.MetadataFields;

public class VersionModel(IContractFieldMetadataInteractionService metadataFields) : PageModel
{
    public ContractFieldMetadataDefinition? Field { get; private set; }
    public ContractFieldMetadataVersion? Version { get; private set; }
    public string FormattedDefinitionJson { get; private set; } = "{}";

    public async Task<IActionResult> OnGetAsync(Guid id, Guid versionId, CancellationToken cancellationToken)
    {
        var result = await Load(id, versionId, cancellationToken);
        return result ?? Page();
    }

    public async Task<IActionResult> OnPostDeactivateVersionAsync(Guid id, Guid versionId, CancellationToken cancellationToken)
    {
        try
        {
            await metadataFields.SetVersionActive(id, versionId, isActive: false, DateTime.UtcNow, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
        }

        return RedirectToPage(new { id, versionId });
    }

    private async Task<IActionResult?> Load(Guid id, Guid versionId, CancellationToken cancellationToken)
    {
        Field = await metadataFields.GetById(id, includeVersions: true, cancellationToken);
        if (Field is null)
        {
            return NotFound();
        }

        Version = Field.Versions.FirstOrDefault(x => x.Id == versionId);
        if (Version is null)
        {
            return NotFound();
        }

        FormattedDefinitionJson = FormatJson(Version.DefinitionJson);
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
