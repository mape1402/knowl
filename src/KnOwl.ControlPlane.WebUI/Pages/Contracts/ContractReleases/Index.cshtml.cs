using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Application.Distribution.ReleaseBundles;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.ContractReleases;

public class IndexModel(
    IContractReleaseRepository releases,
    IContractArtifactRepository artifacts,
    IRuntimeNodeRepository runtimeNodes,
    IContractReleaseExecutionService releaseExecution) : PageModel
{
    public IReadOnlyList<ContractRelease> Releases { get; private set; } = [];
    public IReadOnlyList<ContractArtifact> Artifacts { get; private set; } = [];
    public IReadOnlyList<RuntimeNode> RuntimeNodes { get; private set; } = [];
    public bool ShowReleaseModal { get; private set; }
    public int InitialReleaseStep { get; private set; } = 1;

    [BindProperty]
    public ReleaseInput Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await Load(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (Input.ArtifactIds.Count == 0)
        {
            ModelState.AddModelError(nameof(Input.ArtifactIds), "Select at least one artifact.");
        }
        if (Input.RuntimeNodeIds.Count == 0)
        {
            ModelState.AddModelError(nameof(Input.RuntimeNodeIds), "Select at least one runtime node.");
        }

        if (!ModelState.IsValid)
        {
            ShowReleaseModal = true;
            InitialReleaseStep = Input.ArtifactIds.Count > 0 ? 2 : 1;
            await Load(cancellationToken);
            return Page();
        }

        var releaseName = $"Release {DateTime.UtcNow:yyyyMMdd-HHmmss}";
        var result = await releaseExecution.CreateAndExecute(
            releaseName,
            description: null,
            Input.ArtifactIds,
            Input.RuntimeNodeIds,
            initiatedBy: User?.Identity?.Name ?? "web-ui",
            cancellationToken: cancellationToken);
        StatusMessage = $"Release created and distributed. Targets: {result.TotalTargets}. Succeeded: {result.Succeeded}. Pending pull: {result.AvailableForPull}. Failed: {result.Failed}.";
        return RedirectToPage();
    }

    private async Task Load(CancellationToken cancellationToken)
    {
        Releases = await releases.GetAll(cancellationToken);
        Artifacts = await artifacts.GetAll(cancellationToken);
        RuntimeNodes = await runtimeNodes.GetActiveEnabled(cancellationToken);
    }
}

public sealed class ReleaseInput
{
    public List<Guid> ArtifactIds { get; set; } = [];
    public List<Guid> RuntimeNodeIds { get; set; } = [];
}

