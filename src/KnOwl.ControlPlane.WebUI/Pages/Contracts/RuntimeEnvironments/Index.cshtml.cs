using System.ComponentModel.DataAnnotations;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.RuntimeEnvironments;

public class IndexModel(IRuntimeEnvironmentRepository environments) : PageModel
{
    public IReadOnlyList<RuntimeEnvironment> Environments { get; private set; } = [];
    public int TotalEnvironments { get; private set; }
    public bool ShowEnvironmentModal { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty]
    public RuntimeEnvironmentInput Input { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await Load(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ShowEnvironmentModal = true;
            await Load(cancellationToken);
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

    private async Task Load(CancellationToken cancellationToken)
    {
        var environmentRows = await environments.GetAll(cancellationToken);
        TotalEnvironments = environmentRows.Count;
        Search = Normalize(Search);
        Environments = string.IsNullOrWhiteSpace(Search)
            ? environmentRows
            : environmentRows.Where(MatchesSearch).ToArray();
    }

    private bool MatchesSearch(RuntimeEnvironment environment)
        => Contains(environment.Name)
            || Contains(environment.Code)
            || Contains(environment.Description)
            || Contains(environment.IsEnabled ? "Enabled" : "Disabled");

    private bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(Search)
            && !string.IsNullOrWhiteSpace(value)
            && value.Contains(Search, StringComparison.OrdinalIgnoreCase);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
