using System.ComponentModel.DataAnnotations;
using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.Security;
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
            await Load(cancellationToken);
            return Page();
        }

        try
        {
            await UpsertRuntimeNode(cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await Load(cancellationToken);
            return Page();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGenerateCredentialsAsync(CancellationToken cancellationToken)
    {
        if (CredentialInput.RuntimeNodeId == Guid.Empty)
        {
            return NotFound();
        }

        var issuerBaseUrl = ResolveIssuerBaseUrl(CredentialInput.IssuerBaseUrl);

        try
        {
            var package = await runtimeNodeConnections.GenerateCredentialPackage(
                CredentialInput.RuntimeNodeId,
                issuerBaseUrl,
                cancellationToken);

            return new JsonResult(new
            {
                message = "Control Plane credentials generated.",
                credentialsJson = package.Json,
                credentialsBase64 = package.Base64,
                node = await ToClientNode(CredentialInput.RuntimeNodeId, cancellationToken)
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostImportCredentialsAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (!TryValidateModel(CredentialImportInput, nameof(CredentialImportInput)))
        {
            return BadRequest(new { message = BuildModelStateMessage() });
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

            return new JsonResult(new
            {
                message = "Runtime credentials imported.",
                node = await ToClientNode(CredentialImportInput.RuntimeNodeId, cancellationToken)
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or FormatException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostValidateConnectionAsync(Guid runtimeNodeId, CancellationToken cancellationToken)
    {
        if (runtimeNodeId == Guid.Empty)
        {
            return NotFound();
        }

        try
        {
            var result = await runtimeNodeConnections.ValidateConnection(runtimeNodeId, cancellationToken);
            return new JsonResult(new
            {
                message = result.Message,
                succeeded = result.Succeeded,
                node = await ToClientNode(runtimeNodeId, cancellationToken)
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or HttpRequestException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostWizardUpsertAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (!TryValidateModel(Input, nameof(Input)))
        {
            return BadRequest(new { message = BuildModelStateMessage() });
        }

        try
        {
            var runtimeNode = await UpsertRuntimeNode(cancellationToken);
            return new JsonResult(new
            {
                message = "Runtime node saved.",
                node = ToClientNode(runtimeNode)
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public Task<IActionResult> OnPostWizardImportCredentialsAsync(CancellationToken cancellationToken)
        => OnPostImportCredentialsAsync(cancellationToken);

    public Task<IActionResult> OnPostWizardGenerateCredentialsAsync(CancellationToken cancellationToken)
        => OnPostGenerateCredentialsAsync(cancellationToken);

    public Task<IActionResult> OnPostWizardValidateConnectionAsync(Guid runtimeNodeId, CancellationToken cancellationToken)
        => OnPostValidateConnectionAsync(runtimeNodeId, cancellationToken);

    public async Task<IActionResult> OnPostWizardSetEnabledAsync(Guid runtimeNodeId, bool isEnabled, CancellationToken cancellationToken)
    {
        if (runtimeNodeId == Guid.Empty)
        {
            return NotFound();
        }

        try
        {
            var node = await runtimeNodes.GetById(runtimeNodeId, cancellationToken)
                ?? throw new KeyNotFoundException($"Runtime node '{runtimeNodeId}' was not found.");

            if (isEnabled)
            {
                EnsureNodeReady(node);
            }

            await runtimeNodes.SetIsEnabled(runtimeNodeId, isEnabled, DateTime.UtcNow, cancellationToken);
            return new JsonResult(new
            {
                message = isEnabled ? "Runtime node enabled." : "Runtime node disabled.",
                node = await ToClientNode(runtimeNodeId, cancellationToken)
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<RuntimeNode> UpsertRuntimeNode(CancellationToken cancellationToken)
    {
        var environment = await ResolveEnvironment(cancellationToken);

        RuntimeNode runtimeNode;
        if (Input.Id is null || Input.Id == Guid.Empty)
        {
            runtimeNode = Input.ToEntity(environment);
            runtimeNode.IsEnabled = false;
            await runtimeNodes.Create(runtimeNode, cancellationToken);
            return runtimeNode;
        }

        runtimeNode = await runtimeNodes.GetById(Input.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException($"Runtime node '{Input.Id}' was not found.");
        Input.ApplyTo(runtimeNode, environment);
        await runtimeNodes.Update(runtimeNode, cancellationToken);
        return runtimeNode;
    }

    private async Task<RuntimeEnvironment> ResolveEnvironment(CancellationToken cancellationToken)
    {
        if (Input.EnvironmentId is null || Input.EnvironmentId == Guid.Empty)
        {
            throw new InvalidOperationException("Select a runtime environment.");
        }

        return await environments.GetById(Input.EnvironmentId.Value, cancellationToken)
            ?? throw new InvalidOperationException("Selected environment was not found.");
    }

    private async Task Load(CancellationToken cancellationToken)
    {
        RuntimeNodes = await runtimeNodes.GetAll(cancellationToken);
        Environments = await environments.GetEnabled(cancellationToken);
        ViewData["RuntimeEnvironments"] = Environments;
    }

    private string ResolveIssuerBaseUrl(string configured)
    {
        return string.IsNullOrWhiteSpace(configured)
            ? $"{Request.Scheme}://{Request.Host}".TrimEnd('/')
            : configured.Trim().TrimEnd('/');
    }

    private async Task<RuntimeNodeClientModel> ToClientNode(Guid runtimeNodeId, CancellationToken cancellationToken)
    {
        var node = await runtimeNodes.GetById(runtimeNodeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Runtime node '{runtimeNodeId}' was not found.");

        return ToClientNode(node);
    }

    private static RuntimeNodeClientModel ToClientNode(RuntimeNode node)
        => new(
            node.Id,
            node.Name,
            node.Code,
            node.EnvironmentId,
            node.Environment?.Name ?? node.EnvironmentName,
            node.DistributionMode.ToString(),
            DisplayDistributionMode(node.DistributionMode),
            node.EndpointBaseUri,
            node.Status.ToString(),
            node.IsEnabled,
            node.Description ?? string.Empty,
            node.InboundCredentialStatus.ToString(),
            node.OutboundCredentialStatus.ToString(),
            node.InboundClientId,
            node.InboundKeyId,
            node.InboundAllowedScopes,
            DateOrDash(node.InboundCredentialCreatedAtUtc),
            node.OutboundClientId,
            node.OutboundKeyId,
            node.OutboundRequestedScopes,
            DateOrDash(node.OutboundCredentialImportedAtUtc),
            DateOrDash(node.RegisteredAtUtc),
            DateOrDash(node.LastUpdatedAtUtc));

    private static void EnsureNodeReady(RuntimeNode node)
    {
        if (node.EnvironmentId is null && string.IsNullOrWhiteSpace(node.EnvironmentName))
        {
            throw new InvalidOperationException("Runtime node requires an environment before it can be enabled.");
        }

        if (node.Status != RuntimeNodeStatus.Active)
        {
            throw new InvalidOperationException("Runtime node must be active before it can be enabled.");
        }

        if (node.DistributionMode is DistributionMode.Push or DistributionMode.Hybrid)
        {
            if (string.IsNullOrWhiteSpace(node.EndpointBaseUri))
            {
                throw new InvalidOperationException("Runtime endpoint is required for Push or Hybrid nodes.");
            }

            if (node.OutboundCredentialStatus != ConnectionCredentialStatus.Active)
            {
                throw new InvalidOperationException("Import Runtime credentials before enabling this node.");
            }
        }

        if (node.DistributionMode is DistributionMode.Pull or DistributionMode.Hybrid &&
            node.InboundCredentialStatus != ConnectionCredentialStatus.Active)
        {
            throw new InvalidOperationException("Generate Control Plane credentials before enabling this node.");
        }
    }

    private string BuildModelStateMessage()
    {
        var errors = ModelState.Values
            .SelectMany(x => x.Errors)
            .Select(x => x.ErrorMessage)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        return errors.Length == 0
            ? "Review the runtime node capture."
            : string.Join(Environment.NewLine, errors);
    }

    private static string DisplayDistributionMode(DistributionMode mode)
        => mode switch
        {
            DistributionMode.Push => "Control Plane pushes",
            DistributionMode.Pull => "Runtime pulls",
            DistributionMode.Hybrid => "Both directions",
            _ => mode.ToString()
        };

    private static string DateOrDash(DateTime? value)
        => value.HasValue ? value.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "-";

    private static string DateOrDash(DateTime value)
        => value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}

public sealed class RuntimeNodeCredentialInput
{
    public Guid RuntimeNodeId { get; set; }

    [MaxLength(500)]
    public string IssuerBaseUrl { get; set; } = string.Empty;
}

public sealed record RuntimeNodeClientModel(
    Guid Id,
    string Name,
    string Code,
    Guid? EnvironmentId,
    string? EnvironmentName,
    string DistributionMode,
    string DistributionModeLabel,
    string EndpointBaseUri,
    string Status,
    bool IsEnabled,
    string Description,
    string InboundCredentialStatus,
    string OutboundCredentialStatus,
    string InboundClientId,
    string InboundKeyId,
    string InboundAllowedScopes,
    string InboundCredentialCreatedAt,
    string OutboundClientId,
    string OutboundKeyId,
    string OutboundRequestedScopes,
    string OutboundCredentialImportedAt,
    string RegisteredAt,
    string LastUpdatedAt);

public sealed class RuntimeNodeCredentialImportInput
{
    public Guid RuntimeNodeId { get; set; }

    [Required]
    public string Package { get; set; } = string.Empty;
}
