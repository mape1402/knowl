using System.ComponentModel.DataAnnotations;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.WebUI.ButterMorph;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.MetadataFields;

public class EditModel(IContractFieldMetadataInteractionService metadataFields) : PageModel
{
    [BindProperty]
    [Required]
    public Guid Id { get; set; }

    public string ButterMorphContext => KnOwlButterMorphContext.EditMetadataField(Id);

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var exists = await metadataFields.GetById(id, cancellationToken: cancellationToken) is not null;
        if (!exists)
        {
            return NotFound();
        }

        Id = id;
        return Page();
    }
}
