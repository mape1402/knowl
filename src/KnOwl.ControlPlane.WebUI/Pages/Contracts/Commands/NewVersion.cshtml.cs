using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.WebUI.ButterMorph;
using KnOwl.ControlPlane.WebUI.ContractMetadata;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.WebUI.SchemaTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Commands;

public class NewVersionModel(
    ICommandInteractionService commands,
    ISchemaTypeInteractionService schemaTypes,
    IContractFieldMetadataInteractionService metadataFields) : PageModel
{
    [BindProperty]
    [Required]
    public Guid CommandId { get; set; }

    [BindProperty]
    public VersionInput Input { get; set; } = new();

    [BindProperty]
    [Required]
    public string PayloadSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{}}";

    public string CommandName { get; private set; } = string.Empty;
    public string SchemaTypesJson { get; private set; } = "[]";
    public string MetadataFieldsJson { get; private set; } = "[]";
    public string ButterMorphContext { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await commands.GetById(id, includeVersions: true, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        CommandId = entity.Id;
        CommandName = entity.Name;
        ButterMorphContext = KnOwlButterMorphContext.CommandVersion(entity.Id);
        var latest = entity.Versions.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();
        Input.Version = NextVersion(latest?.VersionNumber);
        PayloadSchemaJson = latest?.PayloadSchemaJson ?? "{\"type\":\"object\",\"properties\":{}}";
        await LoadCatalogs(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var entity = await commands.GetById(CommandId, cancellationToken: cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        CommandName = entity.Name;
        ButterMorphContext = KnOwlButterMorphContext.CommandVersion(entity.Id);
        if (!ModelState.IsValid)
        {
            await LoadCatalogs(cancellationToken);
            return Page();
        }

        try
        {
            JsonDocument.Parse(PayloadSchemaJson);
        }
        catch (JsonException)
        {
            ModelState.AddModelError(nameof(PayloadSchemaJson), "The payload schema is not valid JSON.");
            await LoadCatalogs(cancellationToken);
            return Page();
        }

        var version = Input.Version.Trim();
        if (await commands.VersionExists(CommandId, version, cancellationToken))
        {
            ModelState.AddModelError(nameof(Input.Version), "This version already exists for this command.");
            await LoadCatalogs(cancellationToken);
            return Page();
        }

        var now = DateTime.UtcNow;
        await commands.AddVersion(CommandId, new CommandVersion
        {
            VersionNumber = version,
            PayloadSchemaJson = PayloadSchemaJson,
            Comment = string.IsNullOrWhiteSpace(Input.Comment) ? null : Input.Comment.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);

        return RedirectToPage("/Contracts/Commands/View", new { id = CommandId, version });
    }

    private async Task LoadCatalogs(CancellationToken cancellationToken)
    {
        SchemaTypesJson = await SchemaTypeCatalog.GetSelectableTypesJsonAsync(schemaTypes);
        MetadataFieldsJson = await ContractFieldMetadataCatalog.GetApplicableMetadataJsonAsync(metadataFields, "commands");
    }

    private static string NextVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return "1.0.0";
        var parts = version.Split('.');
        if (parts.Length == 3 && int.TryParse(parts[0], out var a) && int.TryParse(parts[1], out var b) && int.TryParse(parts[2], out var c))
        {
            return $"{a}.{b}.{c + 1}";
        }

        return $"{version}.1";
    }

    public class VersionInput
    {
        [Required]
        [MaxLength(13)]
        [Display(Name = "Version Number")]
        public string Version { get; set; } = string.Empty;

        [MaxLength(1000)]
        [Display(Name = "Version Comment")]
        public string? Comment { get; set; }
    }
}
