using System.ComponentModel.DataAnnotations;
using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.Security;
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
    /// Gets the total connection count before search is applied.
    /// </summary>
    public int TotalDesignNodes { get; private set; }

    /// <summary>
    /// Gets or sets the free-text connection search term.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

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
    /// Creates or updates a Control Plane connection from the wizard.
    /// </summary>
    public async Task<IActionResult> OnPostWizardUpsertAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (!TryValidateModel(Input, nameof(Input)))
        {
            return BadRequest(new { message = BuildModelStateMessage() });
        }

        try
        {
            var node = await UpsertDesignNode(cancellationToken);
            return new JsonResult(new
            {
                message = "Control Plane connection saved.",
                designNodeId = node.Id,
                node = ToClientNode(node)
            });
        }
        catch (Exception ex) when ((ex is ArgumentException or InvalidOperationException or KeyNotFoundException) || IsPersistenceException(ex))
        {
            return BadRequest(new { message = BuildPersistenceMessage(ex) });
        }
    }

    /// <summary>
    /// Generates Runtime credentials for Control Plane push flows.
    /// </summary>
    public async Task<IActionResult> OnPostWizardGenerateCredentialsAsync(Guid designNodeId, CancellationToken cancellationToken)
    {
        if (designNodeId == Guid.Empty)
        {
            return BadRequest(new { message = "Select a Control Plane connection." });
        }

        try
        {
            var package = await connections.GenerateCredentialPackage(
                designNodeId,
                ResolveCurrentBaseUrl(),
                cancellationToken);

            return new JsonResult(new
            {
                message = "Runtime credentials generated.",
                designNodeId,
                credentialsJson = package.Json,
                credentialsBase64 = package.Base64,
                node = await ToClientNode(designNodeId, cancellationToken)
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Imports Control Plane credentials for Runtime pull flows.
    /// </summary>
    public async Task<IActionResult> OnPostWizardImportCredentialsAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (!TryValidateModel(CredentialImportInput, nameof(CredentialImportInput)) ||
            CredentialImportInput.DesignNodeId == Guid.Empty)
        {
            return BadRequest(new { message = BuildModelStateMessage() });
        }

        try
        {
            await connections.ImportCredentialPackage(new ImportRuntimeDesignNodeCredentialPackageInput
            {
                DesignNodeId = CredentialImportInput.DesignNodeId,
                Package = CredentialImportInput.Package
            }, cancellationToken);

            return new JsonResult(new
            {
                message = "Control Plane credentials imported.",
                designNodeId = CredentialImportInput.DesignNodeId,
                node = await ToClientNode(CredentialImportInput.DesignNodeId, cancellationToken)
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or FormatException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Validates Runtime outbound connectivity to Control Plane.
    /// </summary>
    public async Task<IActionResult> OnPostWizardValidateConnectionAsync(Guid designNodeId, CancellationToken cancellationToken)
        => await ValidateConnection(designNodeId, redirect: false, cancellationToken);

    /// <summary>
    /// Validates a Control Plane connection from a card action.
    /// </summary>
    public async Task<IActionResult> OnPostValidateConnectionAsync(Guid designNodeId, CancellationToken cancellationToken)
        => await ValidateConnection(designNodeId, redirect: true, cancellationToken);

    private async Task<IActionResult> ValidateConnection(Guid designNodeId, bool redirect, CancellationToken cancellationToken)
    {
        if (designNodeId == Guid.Empty)
        {
            return BadRequest(new { message = "Select a Control Plane connection." });
        }

        try
        {
            var result = await connections.ValidateConnection(designNodeId, cancellationToken);
            if (redirect)
            {
                StatusMessage = result.Message;
                return RedirectToPage();
            }

            return new JsonResult(new
            {
                message = result.Message,
                succeeded = result.Succeeded,
                designNodeId,
                node = await ToClientNode(designNodeId, cancellationToken)
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or HttpRequestException)
        {
            if (redirect)
            {
                StatusMessage = ex.Message;
                return RedirectToPage();
            }

            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Enables or suspends a Runtime-side Control Plane connection.
    /// </summary>
    public async Task<IActionResult> OnPostWizardSetEnabledAsync(Guid designNodeId, bool isEnabled, CancellationToken cancellationToken)
        => await SetEnabledState(designNodeId, isEnabled, redirect: false, cancellationToken);

    /// <summary>
    /// Enables or suspends a Runtime-side Control Plane connection from a card action.
    /// </summary>
    public async Task<IActionResult> OnPostSetEnabledAsync(Guid designNodeId, bool isEnabled, CancellationToken cancellationToken)
        => await SetEnabledState(designNodeId, isEnabled, redirect: true, cancellationToken);

    private async Task<IActionResult> SetEnabledState(Guid designNodeId, bool isEnabled, bool redirect, CancellationToken cancellationToken)
    {
        if (designNodeId == Guid.Empty)
        {
            return BadRequest(new { message = "Select a Control Plane connection." });
        }

        try
        {
            var node = await GetDesignNode(designNodeId, cancellationToken);
            if (isEnabled)
            {
                EnsureConfigured(node);
                node.Status = RuntimeDesignNodeStatus.Enabled;
                node.IsEnabled = true;
            }
            else
            {
                node.Status = RuntimeDesignNodeStatus.Suspended;
                node.IsEnabled = false;
            }

            await designNodes.Upsert(node, cancellationToken);
            if (redirect)
            {
                StatusMessage = isEnabled ? "Control Plane connection enabled." : "Control Plane connection suspended.";
                return RedirectToPage();
            }

            return new JsonResult(new
            {
                message = isEnabled ? "Control Plane connection enabled." : "Control Plane connection suspended.",
                designNodeId,
                node = ToClientNode(node)
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            if (redirect)
            {
                StatusMessage = ex.Message;
                return RedirectToPage();
            }

            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<RuntimeDesignNode> UpsertDesignNode(CancellationToken cancellationToken)
    {
        RuntimeDesignNode? current = null;
        if (Input.Id is not null && Input.Id.Value != Guid.Empty)
        {
            current = await designNodes.GetById(Input.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Control Plane connection '{Input.Id}' was not found.");
        }

        await EnsureKeyAvailable(cancellationToken);

        var node = current ?? new RuntimeDesignNode
        {
            Id = Input.Id.GetValueOrDefault(Guid.NewGuid()),
            CreatedAtUtc = DateTime.UtcNow,
            Status = RuntimeDesignNodeStatus.Pending,
            IsEnabled = false
        };

        node.Key = Input.Key.Trim();
        node.Name = Input.Name.Trim();
        node.DistributionMode = Input.DistributionMode;
        node.EndpointBaseUri = current?.EndpointBaseUri ?? Input.EndpointBaseUri?.Trim().TrimEnd('/') ?? string.Empty;
        node.RemoteRuntimeNodeId = current?.RemoteRuntimeNodeId ?? Input.RemoteRuntimeNodeId?.Trim() ?? string.Empty;
        node.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();

        if (node.IsEnabled && !IsConfigured(node))
        {
            node.IsEnabled = false;
            node.Status = RuntimeDesignNodeStatus.Pending;
        }

        await designNodes.Upsert(node, cancellationToken);
        return node;
    }

    private async Task EnsureKeyAvailable(CancellationToken cancellationToken)
    {
        var key = Input.Key.Trim();
        var existing = await designNodes.GetByKey(key, cancellationToken);
        if (existing is not null && existing.Id != Input.Id.GetValueOrDefault())
        {
            throw new InvalidOperationException($"Control Plane connection key '{key}' already exists.");
        }
    }

    private async Task Load(CancellationToken cancellationToken)
    {
        var nodes = await designNodes.GetAll(cancellationToken);
        TotalDesignNodes = nodes.Count;
        Search = Normalize(Search);
        DesignNodes = (string.IsNullOrWhiteSpace(Search)
                ? nodes
                : nodes.Where(MatchesSearch))
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ThenBy(x => x.Name)
            .ToList();
    }

    private async Task<RuntimeDesignNode> GetDesignNode(Guid designNodeId, CancellationToken cancellationToken)
        => await designNodes.GetById(designNodeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Control Plane connection '{designNodeId}' was not found.");

    private async Task<RuntimeDesignNodeClientModel> ToClientNode(Guid designNodeId, CancellationToken cancellationToken)
        => ToClientNode(await GetDesignNode(designNodeId, cancellationToken));

    private static RuntimeDesignNodeClientModel ToClientNode(RuntimeDesignNode node)
        => new(
            node.Id,
            node.Key,
            node.Name,
            DisplayDistributionMode(node.DistributionMode),
            node.DistributionMode.ToString(),
            node.EndpointBaseUri,
            node.RemoteRuntimeNodeId,
            node.Status.ToString(),
            DisplayStatus(node),
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
            DateOrDash(node.CreatedAtUtc),
            DateOrDash(node.UpdatedAtUtc),
            IsConfigured(node),
            CanCheckConnection(node));

    private static void EnsureConfigured(RuntimeDesignNode node)
    {
        if (!IsConfigured(node))
        {
            throw new InvalidOperationException("Complete the required credentials before enabling this Control Plane connection.");
        }
    }

    private static bool IsConfigured(RuntimeDesignNode node)
    {
        var needsRuntimeCredentials = node.DistributionMode is DistributionMode.Push or DistributionMode.Hybrid;
        var needsControlPlaneCredentials = node.DistributionMode is DistributionMode.Pull or DistributionMode.Hybrid;
        var hasRuntimeCredentials = node.InboundCredentialStatus == ConnectionCredentialStatus.Active;
        var hasControlPlaneCredentials = node.OutboundCredentialStatus == ConnectionCredentialStatus.Active;
        var hasControlPlaneEndpoint = !string.IsNullOrWhiteSpace(node.EndpointBaseUri);
        var hasRemoteRuntimeNodeId = !string.IsNullOrWhiteSpace(node.RemoteRuntimeNodeId);

        return (!needsRuntimeCredentials || hasRuntimeCredentials) &&
            (!needsControlPlaneCredentials || (hasControlPlaneCredentials && hasControlPlaneEndpoint && hasRemoteRuntimeNodeId));
    }

    private static bool CanCheckConnection(RuntimeDesignNode node)
        => node.IsEnabled &&
           node.Status == RuntimeDesignNodeStatus.Enabled &&
           node.DistributionMode is DistributionMode.Pull or DistributionMode.Hybrid;

    private static string DisplayDistributionMode(DistributionMode mode)
        => mode switch
        {
            DistributionMode.Push => "Control Plane pushes",
            DistributionMode.Pull => "Runtime pulls",
            DistributionMode.Hybrid => "Both directions",
            _ => mode.ToString()
        };

    private static string DisplayStatus(RuntimeDesignNode node)
        => node.IsEnabled && node.Status == RuntimeDesignNodeStatus.Enabled
            ? "Enabled"
            : node.Status.ToString();

    private bool MatchesSearch(RuntimeDesignNode node)
        => Contains(node.Name)
            || Contains(node.Key)
            || Contains(node.Description)
            || Contains(node.EndpointBaseUri)
            || Contains(node.RemoteRuntimeNodeId)
            || Contains(node.Status.ToString())
            || Contains(DisplayStatus(node))
            || Contains(DisplayDistributionMode(node.DistributionMode))
            || Contains(node.DistributionMode.ToString())
            || Contains(RuntimeCredentialReadiness(node))
            || Contains(ControlPlaneCredentialReadiness(node))
            || Contains(EndpointReadiness(node))
            || Contains(node.InboundCredentialStatus.ToString())
            || Contains(node.OutboundCredentialStatus.ToString());

    private bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(Search)
            && !string.IsNullOrWhiteSpace(value)
            && value.Contains(Search, StringComparison.OrdinalIgnoreCase);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string RuntimeCredentialReadiness(RuntimeDesignNode node)
    {
        if (node.DistributionMode is not (DistributionMode.Push or DistributionMode.Hybrid))
        {
            return "Not required";
        }

        return node.InboundCredentialStatus == ConnectionCredentialStatus.Active
            ? "Ready"
            : "Missing";
    }

    private static string ControlPlaneCredentialReadiness(RuntimeDesignNode node)
    {
        if (node.DistributionMode is not (DistributionMode.Pull or DistributionMode.Hybrid))
        {
            return "Not required";
        }

        return node.OutboundCredentialStatus == ConnectionCredentialStatus.Active
            ? "Ready"
            : "Missing";
    }

    private static string EndpointReadiness(RuntimeDesignNode node)
    {
        if (node.DistributionMode is not (DistributionMode.Pull or DistributionMode.Hybrid))
        {
            return "Not required";
        }

        return string.IsNullOrWhiteSpace(node.EndpointBaseUri) ? "Missing" : "Ready";
    }

    private static string DateOrDash(DateTime? value)
        => value.HasValue ? value.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "-";

    private static string DateOrDash(DateTime value)
        => value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    private string ResolveCurrentBaseUrl()
        => $"{Request.Scheme}://{Request.Host}".TrimEnd('/');

    private string BuildModelStateMessage()
    {
        var errors = ModelState.Values
            .SelectMany(x => x.Errors)
            .Select(x => x.ErrorMessage)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        return errors.Length == 0
            ? "Review the Control Plane connection capture."
            : string.Join(Environment.NewLine, errors);
    }

    private string BuildPersistenceMessage(Exception exception)
    {
        if (IsPersistenceException(exception))
        {
            var key = Input.Key.Trim();
            return string.IsNullOrWhiteSpace(key)
                ? "Unable to save the Control Plane connection."
                : $"Control Plane connection key '{key}' already exists.";
        }

        return exception.Message;
    }

    private static bool IsPersistenceException(Exception exception)
        => exception.GetType().Name == "DbUpdateException";
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
    /// Gets or sets the Control Plane endpoint imported from the credential package.
    /// </summary>
    [MaxLength(500)]
    public string? EndpointBaseUri { get; set; }

    /// <summary>
    /// Gets or sets the Runtime node id assigned by the Control Plane.
    /// </summary>
    [MaxLength(100)]
    public string? RemoteRuntimeNodeId { get; set; }

    /// <summary>
    /// Gets or sets whether the connection can be used.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the optional operator description.
    /// </summary>
    public string? Description { get; set; }
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

public sealed record RuntimeDesignNodeClientModel(
    Guid Id,
    string Key,
    string Name,
    string DistributionModeLabel,
    string DistributionMode,
    string EndpointBaseUri,
    string RemoteRuntimeNodeId,
    string Status,
    string StatusLabel,
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
    string CreatedAt,
    string UpdatedAt,
    bool IsConfigured,
    bool CanCheckConnection);
