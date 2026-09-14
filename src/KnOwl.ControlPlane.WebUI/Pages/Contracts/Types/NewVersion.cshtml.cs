using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.WebUI.ButterMorph;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.WebUI.SchemaTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Types;

public class NewVersionModel(ISchemaTypeInteractionService schemaTypes) : PageModel
{
    [BindProperty]
    [Required]
    public Guid TypeId { get; set; }

    [BindProperty]
    public TypeVersionInput Version { get; set; } = new();

    public string TypeName { get; private set; } = string.Empty;
    public string SchemaTypesJson { get; private set; } = "[]";
    public string ButterMorphContext { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await schemaTypes.GetById(id, includeVersions: true, cancellationToken);

        if (entity is null || entity.IsSystem)
        {
            return NotFound();
        }

        TypeId = entity.Id;
        TypeName = entity.Name;
        ButterMorphContext = KnOwlButterMorphContext.NewTypeVersion(entity.Id);
        SchemaTypesJson = await SchemaTypeCatalog.GetSelectableTypesJsonAsync(schemaTypes);
        var latest = entity.Versions.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();
        Version.VersionNumber = NextVersion(latest?.VersionNumber);
        if (latest is not null)
        {
            Version.Comment = latest.Comment;
            SchemaTypeSchemaBuilder.Hydrate(Version, ReadSchemaJson(latest.DefinitionJson));
            Version.VersionNumber = NextVersion(latest.VersionNumber);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var entity = await schemaTypes.GetById(TypeId, cancellationToken: cancellationToken);
        if (entity is null || entity.IsSystem)
        {
            return NotFound();
        }

        TypeName = entity.Name;
        SchemaTypesJson = await SchemaTypeCatalog.GetSelectableTypesJsonAsync(schemaTypes);

        if (await schemaTypes.VersionExists(TypeId, Version.VersionNumber.Trim(), cancellationToken))
        {
            ModelState.AddModelError(nameof(Version.VersionNumber), "This version already exists for this type.");
        }

        var arrayItemVersion = await ResolveArrayItemVersionAsync(cancellationToken);
        SchemaTypeSchemaBuilder.Validate(Version, message => ModelState.AddModelError(string.Empty, message));
        if (Version.BaseType == "array" && Version.ArrayItemTypeVersionId.HasValue && arrayItemVersion is null)
        {
            ModelState.AddModelError(nameof(Version.ArrayItemTypeVersionId), "The item type version does not exist or is not active.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var now = DateTime.UtcNow;
        await schemaTypes.AddVersion(TypeId, new SchemaTypeVersion
        {
            VersionNumber = Version.VersionNumber.Trim(),
            DefinitionJson = BuildDefinitionJson(entity, Version, arrayItemVersion),
            Comment = string.IsNullOrWhiteSpace(Version.Comment) ? null : Version.Comment.Trim(),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);

        return RedirectToPage("/Contracts/Types/View", new { id = TypeId, version = Version.VersionNumber.Trim() });
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

    private static string ReadSchemaJson(string definitionJson)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(definitionJson) ? "{}" : definitionJson);
            if (document.RootElement.TryGetProperty("schema", out var schema))
            {
                return schema.GetRawText();
            }
        }
        catch (JsonException)
        {
        }

        return "{}";
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

    private async Task<SchemaTypeVersion?> ResolveArrayItemVersionAsync(CancellationToken cancellationToken)
    {
        if (Version.BaseType != "array" || !Version.ArrayItemTypeVersionId.HasValue)
        {
            return null;
        }

        var versions = await schemaTypes.GetActiveVersions(cancellationToken);
        return versions.FirstOrDefault(x => x.Id == Version.ArrayItemTypeVersionId);
    }
}
