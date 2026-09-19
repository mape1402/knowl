using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Application.Distribution.Artifacts;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Events;

public class VersionModel(
    IEventInteractionService events,
    IContractVersionPromotionService promotion,
    IContractArtifactBuilder artifactBuilder) : PageModel
{
    public EventDefinition? Event { get; private set; }
    public EventVersion? Version { get; private set; }
    public string FormattedPayloadSchemaJson { get; private set; } = "{}";
    public IReadOnlyCollection<ContractVersionStatus> AllowedTargets { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, Guid versionId, CancellationToken cancellationToken)
    {
        var result = await Load(id, versionId, cancellationToken);
        return result ?? Page();
    }

    public async Task<IActionResult> OnPostTransitionAsync(Guid id, Guid versionId, ContractVersionStatus targetStatus, CancellationToken cancellationToken)
    {
        try
        {
            await promotion.TransitionEventVersion(versionId, targetStatus, cancellationToken);
            if (targetStatus == ContractVersionStatus.Deployed)
            {
                var artifact = await artifactBuilder.BuildEventArtifact(versionId, cancellationToken);
                StatusMessage = $"Version transitioned to {targetStatus}. Artifact generated: {artifact.Topic}@{artifact.VersionNumber}.";
            }
            else
            {
                StatusMessage = $"Version transitioned to {targetStatus}.";
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            StatusMessage = ex.Message;
        }

        return RedirectToPage(new { id, versionId });
    }

    private async Task<IActionResult?> Load(Guid id, Guid versionId, CancellationToken cancellationToken)
    {
        Event = await events.GetById(id, includeVersions: true, cancellationToken);
        if (Event is null)
        {
            return NotFound();
        }

        Version = Event.Versions.FirstOrDefault(x => x.Id == versionId);
        if (Version is null)
        {
            return NotFound();
        }

        FormattedPayloadSchemaJson = FormatJson(Version.PayloadSchemaJson);
        AllowedTargets = promotion.GetAllowedTargets(Version.Status);
        return null;
    }

    public static string FormatJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "{}";
        }

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
