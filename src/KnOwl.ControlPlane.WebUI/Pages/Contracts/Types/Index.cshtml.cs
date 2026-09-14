using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Types;

public class IndexModel(ISchemaTypeInteractionService schemaTypes) : PageModel
{
    public IReadOnlyList<SchemaTypeDefinition> Types { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Types = (await schemaTypes.GetAll(cancellationToken))
            .OrderByDescending(x => x.IsSystem)
            .ThenBy(x => x.Name)
            .ToList();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await schemaTypes.GetById(id, cancellationToken: cancellationToken);
        if (entity is not null)
        {
            await schemaTypes.UpdateDefinition(
                entity.Id,
                entity.Key,
                entity.Name,
                entity.Description,
                isActive: false,
                DateTime.UtcNow,
                cancellationToken);
        }

        return RedirectToPage();
    }
}
