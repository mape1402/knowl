using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Application.Distribution.ArtifactDelivery;
using KnOwl.ControlPlane.Application.Distribution.ReleaseBundles;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.ContractReleases;

public class ViewModel(
    IContractReleaseRepository releases,
    IArtifactDeliveryInteractionService delivery,
    IContractReleaseExecutionService releaseExecution) : PageModel
{
    public ContractRelease? Release { get; private set; }
    public IReadOnlyList<ReleaseDetailArtifactGroup> ArtifactGroups { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (id is null) return NotFound();
        Release = await releases.GetById(id.Value, includeItems: true, includeTargets: true, cancellationToken: cancellationToken);
        if (Release is not null)
        {
            Search = Normalize(Search);
            var groups = BuildArtifactGroups(Release);
            ArtifactGroups = string.IsNullOrWhiteSpace(Search)
                ? groups
                : groups.Where(MatchesSearch).ToArray();
        }

        return Release is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostPushTargetAsync(Guid id, Guid targetId, CancellationToken cancellationToken)
    {
        var result = await delivery.Push(targetId, cancellationToken: cancellationToken);
        StatusMessage = result.Succeeded ? result.Message : $"Push failed: {result.Message}";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostPushAllAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await releaseExecution.Execute(id, User?.Identity?.Name ?? "web-ui", cancellationToken);
        StatusMessage = $"Distribution completed. Targets: {result.TotalTargets}. Succeeded: {result.Succeeded}. Pending pull: {result.AvailableForPull}. Failed: {result.Failed}.";
        return RedirectToPage(new { id });
    }

    private static IReadOnlyList<ReleaseDetailArtifactGroup> BuildArtifactGroups(ContractRelease release)
    {
        return release.Items
            .GroupBy(item => new
            {
                DefinitionId = item.Artifact?.DefinitionId ?? Guid.Empty,
                VersionId = item.Artifact?.VersionId ?? Guid.Empty,
                Topic = item.Artifact?.Topic ?? string.Empty,
                VersionNumber = item.Artifact?.VersionNumber ?? string.Empty
            })
            .Select(group =>
            {
                var groupItems = group.ToArray();
                var groupItemIds = groupItems.Select(x => x.Id).ToHashSet();
                var groupTargets = release.Targets
                    .Where(target => groupItemIds.Contains(target.ReleaseItemId) || groupItems.Any(item => item.ArtifactId == target.ArtifactId))
                    .OrderBy(target => target.RuntimeNode?.Name)
                    .ThenBy(target => target.Artifact?.ArtifactType)
                    .ToArray();

                return new ReleaseDetailArtifactGroup(
                    group.Key.DefinitionId,
                    group.Key.VersionId,
                    group.Key.Topic,
                    group.Key.VersionNumber,
                    ContractGroupName(groupItems),
                    groupItems.Any(x => x.Artifact?.ArtifactType.ToString().StartsWith("Command", StringComparison.OrdinalIgnoreCase) == true) ? "Command" : "Event",
                    groupItems,
                    groupTargets);
            })
            .OrderBy(group => group.Topic)
            .ThenBy(group => group.VersionNumber)
            .ToArray();
    }

    private static string ContractGroupName(IEnumerable<ContractReleaseItem> items)
    {
        var ordered = items.OrderBy(ArtifactTypeOrder).ToArray();
        var primary = ordered.FirstOrDefault(x => x.Artifact?.ArtifactType == ContractArtifactType.Event)
            ?? ordered.FirstOrDefault(x => x.Artifact?.ArtifactType == ContractArtifactType.Command)
            ?? ordered.FirstOrDefault(x => x.Artifact?.ArtifactType == ContractArtifactType.CommandRequest)
            ?? ordered.FirstOrDefault()
            ?? throw new InvalidOperationException("Release artifact group is empty.");

        var name = primary.Artifact?.Name ?? "Unknown contract";
        if (!ordered.Any(x => x.Artifact?.ArtifactType is ContractArtifactType.Command or ContractArtifactType.CommandRequest or ContractArtifactType.CommandReply))
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

    private static int ArtifactTypeOrder(ContractReleaseItem item)
        => item.Artifact?.ArtifactType switch
        {
            ContractArtifactType.Event => 0,
            ContractArtifactType.Command => 1,
            ContractArtifactType.CommandRequest => 2,
            ContractArtifactType.CommandReply => 3,
            _ => 4
        };

    private bool MatchesSearch(ReleaseDetailArtifactGroup group)
        => Contains(group.ContractKind)
            || Contains(group.ContractName)
            || Contains(group.Topic)
            || Contains(group.VersionNumber)
            || group.Items.Any(item =>
                Contains(item.Artifact?.ArtifactType.ToString())
                || Contains(item.Artifact?.Name)
                || Contains(item.Artifact?.Topic)
                || Contains(item.Artifact?.VersionNumber)
                || Contains(item.Artifact?.ContentHash)
                || Contains(item.Artifact?.SourceStatus))
            || group.Targets.Any(target =>
                Contains(target.RuntimeNode?.Name)
                || Contains(target.RuntimeNode?.Code)
                || Contains(target.RuntimeNode?.DistributionMode.ToString())
                || Contains(target.Artifact?.ArtifactType.ToString())
                || Contains(target.Status.ToString())
                || Contains(target.ActivationStatus.ToString())
                || Contains(target.FailureReason)
                || target.Attempts.Any(attempt =>
                    Contains(attempt.Action)
                    || Contains(attempt.ErrorMessage)
                    || Contains(attempt.Succeeded ? "Succeeded" : "Failed")));

    private bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(Search)
            && !string.IsNullOrWhiteSpace(value)
            && value.Contains(Search, StringComparison.OrdinalIgnoreCase);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record ReleaseDetailArtifactGroup(
    Guid DefinitionId,
    Guid VersionId,
    string Topic,
    string VersionNumber,
    string ContractName,
    string ContractKind,
    IReadOnlyList<ContractReleaseItem> Items,
    IReadOnlyList<ContractReleaseTarget> Targets);
