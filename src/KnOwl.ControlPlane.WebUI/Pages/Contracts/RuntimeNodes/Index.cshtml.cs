using System.ComponentModel.DataAnnotations;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Application.Distribution.Security;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.RuntimeNodes;

public class IndexModel(
    IRuntimeNodeRepository runtimeNodes,
    IRuntimeEnvironmentRepository environments,
    IRuntimeNodeConnectionInteractionService runtimeNodeConnections) : PageModel
{
    public IReadOnlyList<RuntimeNode> RuntimeNodes { get; private set; } = [];
    public IReadOnlyList<RuntimeEnvironment> Environments { get; private set; } = [];
    public bool ShowRuntimeNodeModal { get; private set; }
    public bool ShowGeneratedCredentialModal { get; private set; }
    public bool ShowImportCredentialModal { get; private set; }
    public RuntimeNodeCredentialPackageModel? GeneratedCredentialPackage { get; private set; }

    [BindProperty]
    public RuntimeNodeInput Input { get; set; } = new();

    [BindProperty]
    public RuntimeNodeCredentialInput CredentialInput { get; set; } = new();

    [BindProperty]
    public RuntimeNodeCredentialImportInput CredentialImportInput { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await Load(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (!TryValidateModel(Input, nameof(Input)))
        {
            ShowRuntimeNodeModal = true;
            await Load(cancellationToken);
            return Page();
        }

        RuntimeEnvironment? environment = null;
        if (Input.EnvironmentId is not null && Input.EnvironmentId != Guid.Empty)
        {
            environment = await environments.GetById(Input.EnvironmentId.Value, cancellationToken);
            if (environment is null)
            {
                ModelState.AddModelError(nameof(Input.EnvironmentId), "Selected environment was not found.");
                ShowRuntimeNodeModal = true;
                await Load(cancellationToken);
                return Page();
            }
        }

        try
        {
            if (Input.Id is null || Input.Id == Guid.Empty)
            {
                await runtimeNodes.Create(Input.ToEntity(environment), cancellationToken);
            }
            else
            {
                var node = await runtimeNodes.GetById(Input.Id.Value, cancellationToken);
                if (node is null) return NotFound();
                Input.ApplyTo(node, environment);
                await runtimeNodes.Update(node, cancellationToken);
            }
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ShowRuntimeNodeModal = true;
            await Load(cancellationToken);
            return Page();
        }
        catch (DbUpdateException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ShowRuntimeNodeModal = true;
            await Load(cancellationToken);
            return Page();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGenerateCredentialsAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        TryValidateModel(CredentialInput, nameof(CredentialInput));

        if (CredentialInput.RuntimeNodeId == Guid.Empty)
        {
            return NotFound();
        }

        var issuerBaseUrl = string.IsNullOrWhiteSpace(CredentialInput.IssuerBaseUrl)
            ? $"{Request.Scheme}://{Request.Host}".TrimEnd('/')
            : CredentialInput.IssuerBaseUrl.Trim().TrimEnd('/');

        try
        {
            GeneratedCredentialPackage = await runtimeNodeConnections.GenerateCredentialPackage(
                CredentialInput.RuntimeNodeId,
                issuerBaseUrl,
                cancellationToken);
            ShowGeneratedCredentialModal = true;
            await Load(cancellationToken);
            return Page();
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            StatusMessage = ex.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostImportCredentialsAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (!TryValidateModel(CredentialImportInput, nameof(CredentialImportInput)))
        {
            ShowImportCredentialModal = true;
            await Load(cancellationToken);
            return Page();
        }

        if (CredentialImportInput.RuntimeNodeId == Guid.Empty)
        {
            return NotFound();
        }

        try
        {
            await runtimeNodeConnections.ImportCredentialPackage(new ImportRuntimeNodeCredentialPackageInput
            {
                RuntimeNodeId = CredentialImportInput.RuntimeNodeId,
                Package = CredentialImportInput.Package
            }, cancellationToken);
            StatusMessage = "Runtime credentials imported.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or FormatException)
        {
            ModelState.AddModelError(nameof(CredentialImportInput.Package), ex.Message);
            ShowImportCredentialModal = true;
            await Load(cancellationToken);
            return Page();
        }

        return RedirectToPage();
    }

    private async Task Load(CancellationToken cancellationToken)
    {
        RuntimeNodes = await runtimeNodes.GetAll(cancellationToken);
        Environments = await environments.GetEnabled(cancellationToken);
        ViewData["RuntimeEnvironments"] = Environments;
    }
}

public sealed class RuntimeNodeCredentialInput
{
    public Guid RuntimeNodeId { get; set; }

    [MaxLength(500)]
    public string IssuerBaseUrl { get; set; } = string.Empty;
}

public sealed class RuntimeNodeCredentialImportInput
{
    public Guid RuntimeNodeId { get; set; }

    [Required]
    public string Package { get; set; } = string.Empty;
}
