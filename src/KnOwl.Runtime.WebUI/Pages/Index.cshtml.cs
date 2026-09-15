using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.Security;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Distribution;
using KnOwl.Runtime.Application.Catalog;
using KnOwl.Runtime.Storage;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Runtime.WebUI.Pages;

/// <summary>
/// Renders the runtime dashboard using the deployed catalog and configured control-plane nodes.
/// </summary>
public sealed class IndexModel(
    IRuntimeContractCatalogService catalog,
    IRuntimeDesignNodeRepository designNodes) : PageModel
{
    /// <summary>
    /// Gets the total number of artifacts stored in runtime.
    /// </summary>
    public int TotalArtifacts { get; private set; }

    /// <summary>
    /// Gets the number of deployed event artifacts.
    /// </summary>
    public int EventArtifacts { get; private set; }

    /// <summary>
    /// Gets the number of deployed command artifacts.
    /// </summary>
    public int CommandArtifacts { get; private set; }

    /// <summary>
    /// Gets the number of enabled and ready control-plane connections.
    /// </summary>
    public int ActiveControlPlanes { get; private set; }

    /// <summary>
    /// Gets the latest deployed artifacts shown in the dashboard.
    /// </summary>
    public IReadOnlyList<RuntimeContractArtifact> LatestArtifacts { get; private set; } = [];

    /// <summary>
    /// Gets the control-plane summaries shown in the dashboard.
    /// </summary>
    public IReadOnlyList<RuntimeControlPlaneSummary> ControlPlanes { get; private set; } = [];

    /// <summary>
    /// Loads runtime dashboard information.
    /// </summary>
    public async Task OnGet(CancellationToken cancellationToken)
    {
        var artifacts = await catalog.GetAll(cancellationToken);
        var nodes = await designNodes.GetAll(cancellationToken);

        TotalArtifacts = artifacts.Count;
        EventArtifacts = artifacts.Count(x => x.ArtifactType == ContractArtifactType.Event);
        CommandArtifacts = artifacts.Count(x => x.ArtifactType is ContractArtifactType.CommandRequest or ContractArtifactType.CommandReply);
        LatestArtifacts = artifacts.Take(5).ToList();
        ControlPlanes = nodes.Select(RuntimeControlPlaneSummary.FromNode).Take(5).ToList();
        ActiveControlPlanes = ControlPlanes.Count(x => x.IsReady);
    }
}

/// <summary>
/// Lightweight summary of a runtime control-plane connection for dashboard rendering.
/// </summary>
public sealed record RuntimeControlPlaneSummary(
    Guid Id,
    string Key,
    string Name,
    DistributionMode DistributionMode,
    RuntimeDesignNodeStatus Status,
    bool IsReady,
    string CredentialSummary)
{
    /// <summary>
    /// Creates a UI summary from a runtime design node.
    /// </summary>
    public static RuntimeControlPlaneSummary FromNode(RuntimeDesignNode node)
    {
        var hasInbound = node.InboundCredentialStatus == ConnectionCredentialStatus.Active;
        var hasOutbound = node.OutboundCredentialStatus == ConnectionCredentialStatus.Active;
        var isReady = node.IsEnabled && node.Status == RuntimeDesignNodeStatus.Enabled && (hasInbound || hasOutbound);
        var credentialSummary = $"Inbound: {node.InboundCredentialStatus} · Outbound: {node.OutboundCredentialStatus}";

        return new RuntimeControlPlaneSummary(
            node.Id,
            node.Key,
            node.Name,
            node.DistributionMode,
            node.Status,
            isReady,
            credentialSummary);
    }
}

