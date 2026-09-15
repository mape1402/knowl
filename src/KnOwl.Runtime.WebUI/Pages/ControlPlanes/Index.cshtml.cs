using System.ComponentModel.DataAnnotations;
using KnOwl.Contracts.Distribution;
using KnOwl.Runtime.Application.Security;
using KnOwl.Runtime.Distribution;
using KnOwl.Runtime.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Runtime.WebUI.Pages.ControlPlanes;

/// <summary>
/// Manages Control Plane connections known by the Runtime host.
/// </summary>
public sealed class IndexModel(
    IRuntimeDesignNodeRepository designNodes,
    IRuntimeDesignNodeConnectionService connections) : PageModel
{
    /// <summary>
    /// Gets the configured Control Plane design nodes.
    /// </summary>
    public IReadOnlyList<RuntimeDesignNode> DesignNodes { get; private set; } = [];

    /// <summary>
    /// Gets whether the connection modal should reopen after validation errors.
    /// </summary>
    public bool ShowConnectionModal { get; private set; }

    /// <summary>
    /// Gets whether the generated package modal should be shown.
    /// </summary>
    public bool ShowGeneratedCredentialModal { get; private set; }

    /// <summary>
    /// Gets whether the import modal should reopen after validation errors.
    /// </summary>
    public bool ShowImportCredentialModal { get; private set; }

    /// <summary>
    /// Gets the generated Runtime credential package.
    /// </summary>
    public RuntimeDesignNodeCredentialPackageModel? GeneratedCredentialPackage { get; private set; }

    /// <summary>
    /// Gets or sets the connection form input.
    /// </summary>
    [BindProperty]
    public ControlPlaneConnectionInput Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the credential package generation input.
    /// </summary>
    [BindProperty]
    public RuntimeCredentialGenerationInput CredentialInput { get; set; } = new();

    /// <summary>
    /// Gets or sets the credential package import input.
    /// </summary>
    [BindProperty]
    public RuntimeCredentialImportInput CredentialImportInput { get; set; } = new();

    /// <summary>
    /// Gets or sets a user-facing status message.
    /// </summary>
    [TempData]
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Loads runtime Control Plane connections.
    /// </summary>
    public async Task OnGet(CancellationToken cancellationToken)
    {
        await Load(cancellationToken);
    }

    /// <summary>
    /// Creates or updates a Control Plane connection.
    /// </summary>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (!TryValidateModel(Input, nameof(Input)))
        {
            ShowConnectionModal = true;
            await Load(cancellationToken);
            return Page();
        }

        try
        {
            await connections.UpsertDesignNode(
                Input.Id,
                Input.Key,
                Input.Name,
                Input.DistributionMode,
                Input.EndpointBaseUri,
                Input.RemoteRuntimeNodeId,
                Input.IsEnabled,
                cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ShowConnectionModal = true;
            await Load(cancellationToken);
            return Page();
        }

        return RedirectToPage();
    }

    /// <summary>
    /// Generates Runtime credentials for Control Plane push flows.
    /// </summary>
    public async Task<IActionResult> OnPostGenerateCredentialsAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (!TryValidateModel(CredentialInput, nameof(CredentialInput)) ||
            CredentialInput.DesignNodeId == Guid.Empty)
        {
            StatusMessage = "Select a Control Plane connection before generating credentials.";
            return RedirectToPage();
        }

        try
        {
            var issuerBaseUrl = string.IsNullOrWhiteSpace(CredentialInput.IssuerBaseUrl)
                ? $"{Request.Scheme}://{Request.Host}".TrimEnd('/')
                : CredentialInput.IssuerBaseUrl.Trim().TrimEnd('/');

            GeneratedCredentialPackage = await connections.GenerateCredentialPackage(
                CredentialInput.DesignNodeId,
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

    /// <summary>
    /// Imports Control Plane credentials for Runtime pull flows.
    /// </summary>
    public async Task<IActionResult> OnPostImportCredentialsAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (!TryValidateModel(CredentialImportInput, nameof(CredentialImportInput)) ||
            CredentialImportInput.DesignNodeId == Guid.Empty)
        {
            ShowImportCredentialModal = true;
            await Load(cancellationToken);
            return Page();
        }

        try
        {
            await connections.ImportCredentialPackage(new ImportRuntimeDesignNodeCredentialPackageInput
            {
                DesignNodeId = CredentialImportInput.DesignNodeId,
                Package = CredentialImportInput.Package
            }, cancellationToken);
            StatusMessage = "Control Plane credentials imported.";
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

    /// <summary>
    /// Validates Runtime outbound connectivity to Control Plane.
    /// </summary>
    public async Task<IActionResult> OnPostValidateConnectionAsync(Guid designNodeId, CancellationToken cancellationToken)
    {
        if (designNodeId == Guid.Empty)
        {
            return NotFound();
        }

        var result = await connections.ValidateConnection(designNodeId, cancellationToken);
        StatusMessage = result.Message;
        return RedirectToPage();
    }

    private async Task Load(CancellationToken cancellationToken)
    {
        DesignNodes = (await designNodes.GetAll(cancellationToken))
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ThenBy(x => x.Name)
            .ToList();
    }
}

/// <summary>
/// Runtime-side Control Plane connection capture model.
/// </summary>
public sealed class ControlPlaneConnectionInput
{
    /// <summary>
    /// Gets or sets the optional connection id when editing.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the Runtime-local Control Plane key.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how this connection exchanges artifacts.
    /// </summary>
    public DistributionMode DistributionMode { get; set; } = DistributionMode.Hybrid;

    /// <summary>
    /// Gets or sets the Control Plane base URL for pull and validation calls.
    /// </summary>
    [MaxLength(500)]
    public string EndpointBaseUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Runtime node id assigned by the Control Plane.
    /// </summary>
    [MaxLength(100)]
    public string RemoteRuntimeNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional operator description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the connection can be used.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Runtime credential package generation input.
/// </summary>
public sealed class RuntimeCredentialGenerationInput
{
    /// <summary>
    /// Gets or sets the Runtime-side Control Plane connection id.
    /// </summary>
    public Guid DesignNodeId { get; set; }

    /// <summary>
    /// Gets or sets the public Runtime base URL included in the generated package.
    /// </summary>
    [MaxLength(500)]
    public string IssuerBaseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Runtime credential package import input.
/// </summary>
public sealed class RuntimeCredentialImportInput
{
    /// <summary>
    /// Gets or sets the Runtime-side Control Plane connection id.
    /// </summary>
    public Guid DesignNodeId { get; set; }

    /// <summary>
    /// Gets or sets the Control Plane credential package JSON or Base64.
    /// </summary>
    [Required]
    public string Package { get; set; } = string.Empty;
}
