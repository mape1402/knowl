using System.Text.Json;
using KnOwl.ControlPlane.Application.Distribution.Artifacts;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Events;

public class ViewModel(
    IEventInteractionService events,
    IContractVersionPromotionService promotion,
    IContractArtifactBuilder artifactBuilder) : PageModel
{
    public EventDefinition? Event { get; private set; }
    public EventVersion? SelectedVersion { get; private set; }
    public string FormattedPayloadSchemaJson { get; private set; } = "{}";
    public IReadOnlyCollection<ContractVersionStatus> AllowedTargets { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? id, string? version, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return NotFound();
        }

        Event = await events.GetById(id.Value, includeVersions: true, cancellationToken);

        if (Event is null)
        {
            return NotFound();
        }

        SelectedVersion = string.IsNullOrWhiteSpace(version)
            ? Event.Versions.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault()
            : Event.Versions.FirstOrDefault(x => x.VersionNumber == version);

        if (SelectedVersion is not null)
        {
            FormattedPayloadSchemaJson = FormatJson(SelectedVersion.PayloadSchemaJson);
            AllowedTargets = promotion.GetAllowedTargets(SelectedVersion.Status);
        }

        return Page();
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

        return RedirectToPage(new { id });
    }

    private static string FormatJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "{}";
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
