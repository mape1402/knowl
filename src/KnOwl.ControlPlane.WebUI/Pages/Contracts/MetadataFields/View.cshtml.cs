using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.WebUI.ButterMorph;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.MetadataFields;

public class ViewModel(IContractFieldMetadataInteractionService metadataFields) : PageModel
{
    public ContractFieldMetadataDefinition? Field { get; private set; }
    public ContractFieldMetadataVersion? SelectedVersion { get; private set; }
    public IReadOnlyList<ContractFieldMetadataVersion> Versions { get; private set; } = [];
    public string FormattedDefinitionJson { get; private set; } = "{}";
    public string ButterMorphContext { get; private set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, string? version = null, CancellationToken cancellationToken = default)
    {
        Field = await metadataFields.GetById(id, includeVersions: true, cancellationToken);

        if (Field is null)
        {
            return NotFound();
        }

        ButterMorphContext = KnOwlButterMorphContext.NewMetadataFieldVersion(Field.Id);

        SelectedVersion = string.IsNullOrWhiteSpace(version)
            ? Field.Versions.Where(x => x.IsActive).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault()
            : Field.Versions.FirstOrDefault(x => x.VersionNumber == version);
        Search = Normalize(Search);
        Versions = (string.IsNullOrWhiteSpace(Search)
                ? Field.Versions
                : Field.Versions.Where(MatchesSearch))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArray();

        if (SelectedVersion is not null)
        {
            FormattedDefinitionJson = FormatJson(SelectedVersion.DefinitionJson);
        }

        return Page();
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

    private bool MatchesSearch(ContractFieldMetadataVersion version)
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
}
