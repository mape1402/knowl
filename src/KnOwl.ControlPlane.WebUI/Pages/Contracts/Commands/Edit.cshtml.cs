using System.ComponentModel.DataAnnotations;
using KnOwl.ControlPlane.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Commands;

public class EditModel(ICommandInteractionService commands) : PageModel
{
    [BindProperty]
    [Required]
    public Guid Id { get; set; }

    [BindProperty]
    public CommandInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await commands.GetById(id, cancellationToken: cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        Id = entity.Id;
        Input = new CommandInput
        {
            Name = entity.Name,
            Topic = entity.Topic,
            Description = entity.Description
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var entity = await commands.GetById(Id, cancellationToken: cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        await commands.UpdateDefinition(
            Id,
            Input.Name.Trim(),
            Input.Topic?.Trim() ?? string.Empty,
            string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
            DateTime.UtcNow,
            cancellationToken);
        return RedirectToPage("/Contracts/Commands/View", new { id = entity.Id });
    }

    public class CommandInput
    {
        [Required]
        [MaxLength(200)]
        [Display(Name = "Command Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(70)]
        [Display(Name = "Topic (optional)")]
        public string? Topic { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }
    }
}
