using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Application.Distribution.ArtifactDelivery;
using KnOwl.ControlPlane.Application.Distribution.ReleaseBundles;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.ContractReleases;

public class ViewModel(
    IContractReleaseRepository releases,
    IArtifactDeliveryInteractionService delivery,
    IContractReleaseExecutionService releaseExecution) : PageModel
{
    public ContractRelease? Release { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (id is null) return NotFound();
        Release = await releases.GetById(id.Value, includeItems: true, includeTargets: true, cancellationToken: cancellationToken);
        return Release is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostPushTargetAsync(Guid id, Guid targetId, CancellationToken cancellationToken)
    {
        var result = await delivery.Push(targetId, cancellationToken: cancellationToken);
        StatusMessage = result.Succeeded ? result.Message : $"Push failed: {result.Message}";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostPushAllAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await releaseExecution.Execute(id, User?.Identity?.Name ?? "web-ui", cancellationToken);
        StatusMessage = $"Distribution completed. Targets: {result.TotalTargets}. Succeeded: {result.Succeeded}. Pending pull: {result.AvailableForPull}. Failed: {result.Failed}.";
        return RedirectToPage(new { id });
    }
}
