using System.ComponentModel.DataAnnotations;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.RuntimeEnvironments;

public class IndexModel(IRuntimeEnvironmentRepository environments) : PageModel
{
    public IReadOnlyList<RuntimeEnvironment> Environments { get; private set; } = [];
    public bool ShowEnvironmentModal { get; private set; }

    [BindProperty]
    public RuntimeEnvironmentInput Input { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Environments = await environments.GetAll(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ShowEnvironmentModal = true;
            Environments = await environments.GetAll(cancellationToken);
            return Page();
        }

        if (Input.Id is null || Input.Id == Guid.Empty)
        {
            await environments.Create(Input.ToEntity(), cancellationToken);
        }
        else
        {
            var environment = await environments.GetById(Input.Id.Value, cancellationToken);
            if (environment is null) return NotFound();
            Input.ApplyTo(environment);
            await environments.Update(environment, cancellationToken);
        }

        return RedirectToPage();
    }
}

public sealed class RuntimeEnvironmentInput
{
    public Guid? Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Display(Name = "Environment name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Display(Name = "Environment code")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; } = true;

    [Display(Name = "Description")]
    public string? Description { get; set; }

    public RuntimeEnvironment ToEntity() => new()
    {
        Name = Name.Trim(),
        Code = Code.Trim(),
        IsEnabled = IsEnabled,
        Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
        CreatedAtUtc = DateTime.UtcNow
    };

    public void ApplyTo(RuntimeEnvironment environment)
    {
        environment.Name = Name.Trim();
        environment.Code = Code.Trim();
        environment.IsEnabled = IsEnabled;
        environment.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
    }
}
