using System.Text.Encodings.Web;
using System.Text.Json;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Application.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Runtime.WebUI.Pages.Artifacts;

/// <summary>
/// Shows one runtime artifact and its immutable payload schema snapshot.
/// </summary>
public sealed class ViewModel(IRuntimeContractCatalogService catalog) : PageModel
{
    /// <summary>
    /// Gets the artifact being displayed.
    /// </summary>
    public RuntimeContractArtifact? Artifact { get; private set; }

    /// <summary>
    /// Gets the formatted payload schema JSON.
    /// </summary>
    public string PrettyPayloadSchemaJson { get; private set; } = "{}";

    /// <summary>
    /// Gets the REST endpoint for the exact artifact version.
    /// </summary>
    public string ExactEndpoint { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the REST endpoint for the latest artifact version.
    /// </summary>
    public string LatestEndpoint { get; private set; } = string.Empty;

    /// <summary>
    /// Loads artifact details by runtime artifact id.
    /// </summary>
    public async Task<IActionResult> OnGet(Guid id, CancellationToken cancellationToken)
    {
        Artifact = (await catalog.GetAll(cancellationToken)).FirstOrDefault(x => x.Id == id);
        if (Artifact is null)
        {
            return Page();
        }

        PrettyPayloadSchemaJson = FormatJson(Artifact.PayloadSchemaJson);
        var typeSegment = Artifact.ArtifactType.ToString().ToLowerInvariant();
        var topic = Uri.EscapeDataString(Artifact.Topic);
        var version = Uri.EscapeDataString(Artifact.VersionNumber);
        ExactEndpoint = $"/runtime/contracts/{typeSegment}/{topic}/versions/{version}";
        LatestEndpoint = $"/runtime/contracts/{typeSegment}/{topic}/latest";

        return Page();
    }

    private static string FormatJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(
                document.RootElement,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
