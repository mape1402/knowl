using System.ComponentModel.DataAnnotations;
using KnOwl.ControlPlane.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Types;

public class EditModel(ISchemaTypeInteractionService schemaTypes) : PageModel
{
    [BindProperty]
    [Required]
    public Guid Id { get; set; }

    [BindProperty]
    public TypeInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await schemaTypes.GetById(id, cancellationToken: cancellationToken);
        if (entity is null || entity.IsSystem)
        {
            return NotFound();
        }

        Id = entity.Id;
        Input = new TypeInput
        {
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var entity = await schemaTypes.GetById(Id, cancellationToken: cancellationToken);
        if (entity is null || entity.IsSystem)
        {
            return NotFound();
        }

        var name = Input.Name.Trim();
        if (await schemaTypes.KeyExists(name, excludingId: Id, cancellationToken))
        {
            ModelState.AddModelError(nameof(Input.Name), "A type with this name already exists.");
            return Page();
        }

        await schemaTypes.UpdateDefinition(
            Id,
            name,
            name,
            string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
            Input.IsActive,
            DateTime.UtcNow,
            cancellationToken);
        return RedirectToPage("/Contracts/Types/View", new { id = entity.Id });
    }

    public class TypeInput
    {
        [Required]
        [MaxLength(100)]
        [RegularExpression(@"^[A-Za-z][A-Za-z0-9_-]*$", ErrorMessage = "Usa letras, numeros, _ o -, comenzando con una letra.")]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
