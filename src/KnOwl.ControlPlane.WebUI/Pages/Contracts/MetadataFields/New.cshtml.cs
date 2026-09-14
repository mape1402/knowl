using KnOwl.ControlPlane.WebUI.ButterMorph;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.MetadataFields;

public class NewModel : PageModel
{
    public string ButterMorphContext { get; private set; } = KnOwlButterMorphContext.NewMetadataField();

    public void OnGet()
    {
    }
}
