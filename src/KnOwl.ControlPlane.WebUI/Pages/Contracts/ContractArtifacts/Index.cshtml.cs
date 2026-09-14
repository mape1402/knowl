using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.ContractArtifacts;

public class IndexModel(IContractArtifactRepository artifacts) : PageModel
{
    public IReadOnlyList<ContractArtifact> Artifacts { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Artifacts = await artifacts.GetAll(cancellationToken);
    }
}


