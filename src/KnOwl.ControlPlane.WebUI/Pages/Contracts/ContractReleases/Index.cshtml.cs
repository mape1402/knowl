using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Application.Distribution.ReleaseBundles;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.ContractReleases;

public class IndexModel(
    IContractReleaseRepository releases,
    IContractArtifactRepository artifacts,
    IRuntimeNodeRepository runtimeNodes,
    IContractReleaseExecutionService releaseExecution) : PageModel
{
    public IReadOnlyList<ContractRelease> Releases { get; private set; } = [];
    public IReadOnlyList<ReleaseCard> ReleaseCards { get; private set; } = [];
    public int TotalReleaseCards { get; private set; }
    public IReadOnlyList<ContractArtifact> Artifacts { get; private set; } = [];
    public IReadOnlyList<ReleaseContractOption> ReleaseOptions { get; private set; } = [];
    public IReadOnlyList<RuntimeNode> RuntimeNodes { get; private set; } = [];
    public IReadOnlyList<ReleaseRuntimeEnvironmentGroup> RuntimeEnvironmentGroups { get; private set; } = [];
    public bool ShowReleaseModal { get; private set; }
    public int InitialReleaseStep { get; private set; } = 1;

    [BindProperty]
    public ReleaseInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await Load(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Input.ArtifactIds = Input.ArtifactIds.Distinct().ToList();
        Input.RuntimeNodeIds = Input.RuntimeNodeIds.Distinct().ToList();

        if (Input.ArtifactIds.Count == 0)
        {
            ModelState.AddModelError(nameof(Input.ArtifactIds), "Select at least one artifact.");
        }
        if (Input.RuntimeNodeIds.Count == 0)
        {
            ModelState.AddModelError(nameof(Input.RuntimeNodeIds), "Select at least one runtime node.");
        }

        if (!ModelState.IsValid)
        {
            ShowReleaseModal = true;
            InitialReleaseStep = Input.ArtifactIds.Count > 0 ? 2 : 1;
            await Load(cancellationToken);
            return Page();
        }

        var releaseName = string.IsNullOrWhiteSpace(Input.Name)
            ? $"Release {DateTime.UtcNow:yyyyMMdd-HHmmss}"
            : Input.Name.Trim();

        var result = await releaseExecution.CreateAndExecute(
            releaseName,
            string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
            Input.ArtifactIds,
            Input.RuntimeNodeIds,
            initiatedBy: User?.Identity?.Name ?? "web-ui",
            cancellationToken: cancellationToken);

        StatusMessage = $"Release created and distributed. Targets: {result.TotalTargets}. Succeeded: {result.Succeeded}. Pending pull: {result.AvailableForPull}. Failed: {result.Failed}.";
        return RedirectToPage();
    }

    private async Task Load(CancellationToken cancellationToken)
    {
        var releaseRows = await releases.GetAll(cancellationToken);
        var detailedReleases = new List<ContractRelease>(releaseRows.Count);
        foreach (var release in releaseRows)
        {
            detailedReleases.Add(
                await releases.GetById(release.Id, includeItems: true, includeTargets: true, cancellationToken)
                ?? release);
        }

        Releases = detailedReleases;
        Search = Normalize(Search);
        var releaseCards = detailedReleases.Select(ReleaseCard.FromRelease).ToArray();
        TotalReleaseCards = releaseCards.Length;
        ReleaseCards = (string.IsNullOrWhiteSpace(Search)
                ? releaseCards
                : releaseCards.Where(MatchesSearch))
            .ToList();
        Artifacts = await artifacts.GetAll(cancellationToken);
        ReleaseOptions = BuildReleaseOptions(Artifacts);
        RuntimeNodes = await runtimeNodes.GetActiveEnabled(cancellationToken);
        RuntimeEnvironmentGroups = RuntimeNodes
            .GroupBy(x => new
            {
                Id = x.EnvironmentId?.ToString("N") ?? "none",
                Name = string.IsNullOrWhiteSpace(x.Environment?.Name ?? x.EnvironmentName)
                    ? "No environment"
                    : x.Environment?.Name ?? x.EnvironmentName
            })
            .Select(group => new ReleaseRuntimeEnvironmentGroup(
                group.Key.Id,
                group.Key.Name,
                group.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray()))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static IReadOnlyList<ReleaseContractOption> BuildReleaseOptions(IReadOnlyList<ContractArtifact> sourceArtifacts)
    {
        return sourceArtifacts
            .GroupBy(x => new { x.DefinitionId, x.VersionId, x.Topic, x.VersionNumber })
            .Select(group =>
            {
                var orderedArtifacts = group
                    .OrderBy(ArtifactTypeOrder)
                    .ToArray();
                var primary = orderedArtifacts.First();
                var isCommand = orderedArtifacts.Any(x => x.ArtifactType is ContractArtifactType.Command or ContractArtifactType.CommandRequest or ContractArtifactType.CommandReply);
                var hasReply = orderedArtifacts.Any(x => x.ArtifactType == ContractArtifactType.CommandReply)
                    || orderedArtifacts
                        .Where(x => x.ArtifactType == ContractArtifactType.Command)
                        .Select(x => CommandArtifactPayloadDocument.Read(x.PayloadSchemaJson).ReplyPayloadSchemaJson)
                        .Any(x => !string.IsNullOrWhiteSpace(x));
                var label = isCommand ? "Command" : "Event";
                var details = isCommand
                    ? hasReply ? "Request and reply" : "Request only"
                    : "Event artifact";

                return new ReleaseContractOption(
                    $"{group.Key.VersionId:N}",
                    primary.DefinitionId,
                    primary.VersionId,
                    label,
                    NormalizeDisplayName(primary.Name, isCommand),
                    group.Key.Topic,
                    group.Key.VersionNumber,
                    details,
                    orderedArtifacts.Max(x => x.CreatedAtUtc),
                    orderedArtifacts.Select(x => x.Id).ToArray(),
                    orderedArtifacts.Select(x => new ReleaseOptionArtifact(x.ArtifactType.ToString(), x.ContentHash)).ToArray());
            })
            .OrderBy(x => x.ContractType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToArray();
    }

    private static int ArtifactTypeOrder(ContractArtifact artifact)
        => artifact.ArtifactType switch
        {
            ContractArtifactType.Event => 0,
            ContractArtifactType.Command => 1,
            ContractArtifactType.CommandRequest => 2,
            ContractArtifactType.CommandReply => 3,
            _ => 4
        };

    private static string NormalizeDisplayName(string name, bool isCommand)
    {
        if (!isCommand)
        {
            return name;
        }

        foreach (var suffix in new[] { " Request", " Reply" })
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return name[..^suffix.Length].TrimEnd();
            }
        }

        return name;
    }

    private bool MatchesSearch(ReleaseCard release)
        => Contains(release.Name)
            || Contains(release.Status)
            || release.Artifacts.Any(artifact =>
                Contains(artifact.ArtifactType)
                || Contains(artifact.Name)
                || Contains(artifact.Topic)
                || Contains(artifact.VersionNumber)
                || Contains(artifact.ContentHash)
                || artifact.Targets.Any(target =>
                    Contains(target.RuntimeNodeName)
                    || Contains(target.RuntimeNodeCode)
                    || Contains(target.DistributionMode)
                    || Contains(target.ArtifactLabel)
                    || Contains(target.Status)
                    || Contains(target.ActivationStatus)))
            || release.RuntimeNodes.Any(node =>
                Contains(node.Name)
                || Contains(node.Code)
                || Contains(node.DistributionMode));

    private bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(Search)
            && !string.IsNullOrWhiteSpace(value)
            && value.Contains(Search, StringComparison.OrdinalIgnoreCase);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class ReleaseInput
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<Guid> ArtifactIds { get; set; } = [];
    public List<Guid> RuntimeNodeIds { get; set; } = [];
}

public sealed record ReleaseRuntimeEnvironmentGroup(
    string Key,
    string Name,
    IReadOnlyList<RuntimeNode> Nodes);

public sealed record ReleaseContractOption(
    string Key,
    Guid DefinitionId,
    Guid VersionId,
    string ContractType,
    string Name,
    string Topic,
    string VersionNumber,
    string Details,
    DateTime CreatedAtUtc,
    IReadOnlyList<Guid> ArtifactIds,
    IReadOnlyList<ReleaseOptionArtifact> Artifacts);

public sealed record ReleaseOptionArtifact(string ArtifactType, string ContentHash);

public sealed record ReleaseCard(
    Guid Id,
    string Name,
    string Status,
    DateTime CreatedAtUtc,
    int ArtifactCount,
    int TargetCount,
    int ActivatedCount,
    int PendingCount,
    int FailedCount,
    IReadOnlyList<ReleaseArtifactCard> Artifacts,
    IReadOnlyList<ReleaseRuntimeCard> RuntimeNodes)
{
    public static ReleaseCard FromRelease(ContractRelease release)
    {
        var targets = release.Targets.ToArray();
        var items = release.Items.ToArray();
        var artifactCards = items
            .Select(item =>
            {
                var artifactTargets = targets
                    .Where(target => target.ReleaseItemId == item.Id || target.ArtifactId == item.ArtifactId)
                    .Select(ReleaseTargetCard.FromTarget)
                    .ToList();

                return new ReleaseArtifactCard(
                    item.Id,
                    item.ArtifactId,
                    item.Artifact?.ArtifactType.ToString() ?? "Artifact",
                    item.Artifact?.Name ?? "Unknown artifact",
                    item.Artifact?.Topic ?? string.Empty,
                    item.Artifact?.VersionNumber ?? string.Empty,
                    item.Artifact?.ContentHash ?? string.Empty,
                    artifactTargets);
            })
            .ToList();

        var runtimeCards = targets
            .GroupBy(target => target.RuntimeNodeId)
            .Select(group =>
            {
                var first = group.First();
                var nodeName = first.RuntimeNode?.Name ?? first.RuntimeNodeId.ToString("N");
                var nodeCode = first.RuntimeNode?.Code ?? string.Empty;
                var mode = first.RuntimeNode?.DistributionMode.ToString() ?? string.Empty;
                var groupedTargets = group.Select(ReleaseTargetCard.FromTarget).ToList();

                return new ReleaseRuntimeCard(
                    first.RuntimeNodeId,
                    nodeName,
                    nodeCode,
                    mode,
                    groupedTargets.Count,
                    groupedTargets.Count(x => x.IsActivated),
                    groupedTargets.Count(x => x.IsPending),
                    groupedTargets.Count(x => x.IsFailed));
            })
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ReleaseCard(
            release.Id,
            release.Name,
            release.Status.ToString(),
            release.CreatedAtUtc,
            items.Length,
            targets.Length,
            targets.Count(IsActivated),
            targets.Count(IsPending),
            targets.Count(IsFailed),
            artifactCards,
            runtimeCards);
    }

    private static bool IsActivated(ContractReleaseTarget target)
        => target.Status == ContractReleaseTargetStatus.Activated;

    private static bool IsPending(ContractReleaseTarget target)
        => target.Status is ContractReleaseTargetStatus.Pending
            or ContractReleaseTargetStatus.AvailableForPull
            or ContractReleaseTargetStatus.PushScheduled
            or ContractReleaseTargetStatus.InProgress
            or ContractReleaseTargetStatus.Delivered
            or ContractReleaseTargetStatus.Acknowledged;

    private static bool IsFailed(ContractReleaseTarget target)
        => target.Status is ContractReleaseTargetStatus.Failed or ContractReleaseTargetStatus.Cancelled;
}

public sealed record ReleaseArtifactCard(
    Guid ReleaseItemId,
    Guid ArtifactId,
    string ArtifactType,
    string Name,
    string Topic,
    string VersionNumber,
    string ContentHash,
    IReadOnlyList<ReleaseTargetCard> Targets);

public sealed record ReleaseRuntimeCard(
    Guid RuntimeNodeId,
    string Name,
    string Code,
    string DistributionMode,
    int TargetCount,
    int ActivatedCount,
    int PendingCount,
    int FailedCount);

public sealed record ReleaseTargetCard(
    Guid Id,
    string RuntimeNodeName,
    string RuntimeNodeCode,
    string DistributionMode,
    string ArtifactLabel,
    string Status,
    string ActivationStatus,
    bool IsActivated,
    bool IsPending,
    bool IsFailed)
{
    public static ReleaseTargetCard FromTarget(ContractReleaseTarget target)
        => new(
            target.Id,
            target.RuntimeNode?.Name ?? target.RuntimeNodeId.ToString("N"),
            target.RuntimeNode?.Code ?? string.Empty,
            target.RuntimeNode?.DistributionMode.ToString() ?? string.Empty,
            target.Artifact is null
                ? target.ArtifactId.ToString("N")
                : $"{target.Artifact.Topic}@{target.Artifact.VersionNumber}",
            target.Status.ToString(),
            target.ActivationStatus.ToString(),
            target.Status == ContractReleaseTargetStatus.Activated,
            target.Status is ContractReleaseTargetStatus.Pending
                or ContractReleaseTargetStatus.AvailableForPull
                or ContractReleaseTargetStatus.PushScheduled
                or ContractReleaseTargetStatus.InProgress
                or ContractReleaseTargetStatus.Delivered
                or ContractReleaseTargetStatus.Acknowledged,
            target.Status is ContractReleaseTargetStatus.Failed or ContractReleaseTargetStatus.Cancelled);
}
