using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.WebUI.ButterMorph;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.WebUI.SchemaTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Types;

public class NewModel(ISchemaTypeInteractionService schemaTypes) : PageModel
{
    [BindProperty]
    public TypeInput Input { get; set; } = new();

    [BindProperty]
    public TypeVersionInput Version { get; set; } = new();

    public string SchemaTypesJson { get; private set; } = "[]";
    public string ButterMorphContext { get; private set; } = KnOwlButterMorphContext.NewType();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        SchemaTypesJson = await SchemaTypeCatalog.GetSelectableTypesJsonAsync(schemaTypes);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (await schemaTypes.KeyExists(Input.Name.Trim(), cancellationToken: cancellationToken))
        {
            ModelState.AddModelError(nameof(Input.Name), "A type with this name already exists.");
        }

        var arrayItemVersion = await ResolveArrayItemVersionAsync(cancellationToken);
        SchemaTypeSchemaBuilder.Validate(Version, message => ModelState.AddModelError(string.Empty, message));
        if (Version.BaseType == "array" && Version.ArrayItemTypeVersionId.HasValue && arrayItemVersion is null)
        {
            ModelState.AddModelError(nameof(Version.ArrayItemTypeVersionId), "The item type version does not exist or is not active.");
        }

        if (!ModelState.IsValid)
        {
            SchemaTypesJson = await SchemaTypeCatalog.GetSelectableTypesJsonAsync(schemaTypes);
            return Page();
        }

        var now = DateTime.UtcNow;
        var entity = new SchemaTypeDefinition
        {
            Key = Input.Name.Trim(),
            Name = Input.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
            IsSystem = false,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        entity.Versions.Add(new SchemaTypeVersion
        {
            VersionNumber = Version.VersionNumber.Trim(),
            DefinitionJson = BuildDefinitionJson(entity, Version, arrayItemVersion),
            Comment = string.IsNullOrWhiteSpace(Version.Comment) ? null : Version.Comment.Trim(),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        await schemaTypes.Create(entity, cancellationToken);
        return RedirectToPage("/Contracts/Types/View", new { id = entity.Id });
    }

    private static string BuildDefinitionJson(SchemaTypeDefinition entity, TypeVersionInput version, SchemaTypeVersion? arrayItemVersion)
    {
        var schema = SchemaTypeSchemaBuilder.Build(version, arrayItemVersion);
        return JsonSerializer.Serialize(new
        {
            key = entity.Key,
            name = entity.Name,
            description = entity.Description ?? string.Empty,
            version = version.VersionNumber.Trim(),
            baseType = version.BaseType,
            comment = version.Comment ?? string.Empty,
            schema = JsonDocument.Parse(schema).RootElement
        });
    }

    private async Task<SchemaTypeVersion?> ResolveArrayItemVersionAsync(CancellationToken cancellationToken)
    {
        if (Version.BaseType != "array" || !Version.ArrayItemTypeVersionId.HasValue)
        {
            return null;
        }

        var versions = await schemaTypes.GetActiveVersions(cancellationToken);
        return versions.FirstOrDefault(x => x.Id == Version.ArrayItemTypeVersionId);
    }

    public class TypeInput
    {
        [Required]
        [MaxLength(100)]
        [RegularExpression(@"^[A-Za-z][A-Za-z0-9_-]*$", ErrorMessage = "Usa letras, numeros, _ o -, comenzando con una letra.")]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }
    }
}
