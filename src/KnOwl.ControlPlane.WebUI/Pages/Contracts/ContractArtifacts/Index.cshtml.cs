using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.ContractArtifacts;

public class IndexModel(IContractArtifactRepository artifacts) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? GroupKey { get; set; }

    public IReadOnlyList<ContractArtifactGroup> Groups { get; private set; } = [];

    public ContractArtifactGroup? SelectedGroup { get; private set; }

    public IReadOnlyList<ContractArtifactVersionGroup> SelectedVersions { get; private set; } = [];

    public IReadOnlyList<ContractArtifactVersionDetail> SelectedVersionDetails { get; private set; } = [];

    public IReadOnlyList<ContractArtifactCard> SelectedArtifacts { get; private set; } = [];

    public IReadOnlyList<ContractArtifactBundleDetail> SelectedArtifactDetails { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var rows = await artifacts.GetAll(cancellationToken);
        Groups = BuildGroups(rows);

        SelectedGroup = string.IsNullOrWhiteSpace(GroupKey)
            ? null
            : Groups.FirstOrDefault(x => string.Equals(x.Key, GroupKey, StringComparison.OrdinalIgnoreCase));

        if (SelectedGroup is null)
        {
            SelectedVersions = [];
            SelectedVersionDetails = [];
            SelectedArtifacts = [];
            SelectedArtifactDetails = [];
            return;
        }

        GroupKey = SelectedGroup.Key;
        SelectedArtifacts = BuildArtifactCards(SelectedGroup.Artifacts);
        SelectedVersions = BuildVersionGroups(SelectedGroup.Artifacts);
        SelectedArtifactDetails = SelectedArtifacts
            .Select(artifact => new ContractArtifactBundleDetail(
                artifact.Key,
                artifact.ContractKind,
                artifact.Name,
                artifact.Topic,
                artifact.VersionNumber,
                artifact.SourceStatus,
                artifact.CreatedAtUtc,
                artifact.SchemaSummary,
                artifact.Artifacts
                    .SelectMany(BuildArtifactDetails)
                    .ToArray()))
            .ToArray();
        SelectedVersionDetails = SelectedVersions
            .Select(version => new ContractArtifactVersionDetail(
                version.Key,
                version.VersionNumber,
                version.Name,
                version.Topic,
                version.CreatedAtUtc,
                version.Artifacts
                    .SelectMany(BuildArtifactDetails)
                    .ToArray()))
            .ToArray();
    }

    private static IReadOnlyList<ContractArtifactGroup> BuildGroups(IReadOnlyList<ContractArtifact> source)
    {
        return source
            .GroupBy(x => new
            {
                Kind = GetContractKind(x.ArtifactType),
                x.DefinitionId,
                x.Topic
            })
            .Select(group =>
            {
                var ordered = group.OrderByDescending(x => x.CreatedAtUtc).ToArray();
                var primary = SelectDisplayArtifact(ordered);
                var versionCount = ordered.Select(x => x.VersionId).Distinct().Count();
                var latest = ordered.FirstOrDefault();

                return new ContractArtifactGroup(
                    $"{group.Key.Kind}:{group.Key.DefinitionId:N}",
                    group.Key.Kind,
                    group.Key.DefinitionId,
                    NormalizeDisplayName(primary.Name, group.Key.Kind),
                    group.Key.Topic,
                    primary.Description,
                    versionCount,
                    CountLogicalArtifacts(ordered),
                    latest?.VersionNumber ?? string.Empty,
                    latest?.CreatedAtUtc ?? DateTime.MinValue,
                    ordered.Any(x => x.ArtifactType is ContractArtifactType.Command or ContractArtifactType.CommandRequest),
                    HasReply(ordered),
                    ordered);
            })
            .OrderBy(x => x.ContractKind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<ContractArtifactVersionGroup> BuildVersionGroups(IReadOnlyList<ContractArtifact> source)
    {
        return source
            .GroupBy(x => new { x.VersionId, x.VersionNumber })
            .Select(group =>
            {
                var ordered = group
                    .OrderBy(x => ArtifactTypeOrder(x.ArtifactType))
                    .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var primary = ordered.First();

                return new ContractArtifactVersionGroup(
                    group.Key.VersionId.ToString("N"),
                    NormalizeDisplayName(primary.Name, GetContractKind(primary.ArtifactType)),
                    primary.Topic,
                    group.Key.VersionNumber,
                    ordered.Max(x => x.CreatedAtUtc),
                    ordered.Any(x => x.ArtifactType == ContractArtifactType.Event),
                    ordered.Any(x => x.ArtifactType is ContractArtifactType.Command or ContractArtifactType.CommandRequest),
                    HasReply(ordered),
                    ordered);
            })
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArray();
    }

    private static IReadOnlyList<ContractArtifactCard> BuildArtifactCards(IReadOnlyList<ContractArtifact> source)
    {
        return source
            .GroupBy(x => new
            {
                Kind = GetContractKind(x.ArtifactType),
                x.DefinitionId,
                x.VersionId,
                x.Topic,
                x.VersionNumber
            })
            .Select(group =>
            {
                var ordered = group
                    .OrderBy(x => ArtifactTypeOrder(x.ArtifactType))
                    .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var primary = SelectDisplayArtifact(ordered);
                var hasReply = HasReply(ordered);
                var schemaSummary = group.Key.Kind == "Event"
                    ? "Event schema"
                    : hasReply ? "Request and reply schemas" : "Request schema";

                return new ContractArtifactCard(
                    $"{group.Key.Kind}:{group.Key.VersionId:N}",
                    group.Key.Kind,
                    NormalizeDisplayName(primary.Name, group.Key.Kind),
                    group.Key.Topic,
                    group.Key.VersionNumber,
                    primary.SourceStatus,
                    ordered.Max(x => x.CreatedAtUtc),
                    schemaSummary,
                    ordered);
            })
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.ContractKind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string GetContractKind(ContractArtifactType artifactType)
        => artifactType == ContractArtifactType.Event ? "Event" : "Command";

    private static string GetArtifactPartLabel(ContractArtifactType artifactType)
        => artifactType switch
        {
            ContractArtifactType.Command => "Command",
            ContractArtifactType.CommandRequest => "Request",
            ContractArtifactType.CommandReply => "Reply",
            _ => "Payload"
        };

    private static IReadOnlyList<ContractArtifactDetail> BuildArtifactDetails(ContractArtifact artifact)
    {
        if (artifact.ArtifactType == ContractArtifactType.Command)
        {
            var payload = CommandArtifactPayloadDocument.Read(artifact.PayloadSchemaJson);
            List<ContractArtifactDetail> details =
            [
                CreateArtifactDetail(artifact, "Request", ContractArtifactType.CommandRequest, payload.RequestPayloadSchemaJson)
            ];

            if (!string.IsNullOrWhiteSpace(payload.ReplyPayloadSchemaJson))
            {
                details.Add(CreateArtifactDetail(artifact, "Reply", ContractArtifactType.CommandReply, payload.ReplyPayloadSchemaJson));
            }

            return details;
        }

        return [CreateArtifactDetail(artifact, GetArtifactPartLabel(artifact.ArtifactType), artifact.ArtifactType, artifact.PayloadSchemaJson)];
    }

    private static ContractArtifactDetail CreateArtifactDetail(
        ContractArtifact artifact,
        string label,
        ContractArtifactType artifactType,
        string payloadSchemaJson)
        => new(
            artifact.Id,
            label,
            artifactType.ToString(),
            artifact.Name,
            artifact.Topic,
            artifact.VersionNumber,
            artifact.ContentHash,
            artifact.SourceStatus,
            artifact.CreatedAtUtc,
            payloadSchemaJson);

    private static bool HasReply(IReadOnlyList<ContractArtifact> artifacts)
        => artifacts.Any(x => x.ArtifactType == ContractArtifactType.CommandReply)
            || artifacts
                .Where(x => x.ArtifactType == ContractArtifactType.Command)
                .Select(x => CommandArtifactPayloadDocument.Read(x.PayloadSchemaJson).ReplyPayloadSchemaJson)
                .Any(x => !string.IsNullOrWhiteSpace(x));

    private static int CountLogicalArtifacts(IReadOnlyList<ContractArtifact> artifacts)
        => artifacts
            .GroupBy(x => new
            {
                Kind = GetContractKind(x.ArtifactType),
                x.VersionId
            })
            .Count();

    private static ContractArtifact SelectDisplayArtifact(IEnumerable<ContractArtifact> artifacts)
        => artifacts
            .OrderBy(x => ArtifactTypeOrder(x.ArtifactType))
            .ThenByDescending(x => x.CreatedAtUtc)
            .First();

    private static int ArtifactTypeOrder(ContractArtifactType artifactType)
        => artifactType switch
        {
            ContractArtifactType.Event => 0,
            ContractArtifactType.Command => 1,
            ContractArtifactType.CommandRequest => 2,
            ContractArtifactType.CommandReply => 3,
            _ => 4
        };

    private static string NormalizeDisplayName(string name, string contractKind)
    {
        if (!string.Equals(contractKind, "Command", StringComparison.OrdinalIgnoreCase))
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
}

public sealed record ContractArtifactGroup(
    string Key,
    string ContractKind,
    Guid DefinitionId,
    string Name,
    string Topic,
    string? Description,
    int VersionCount,
    int ArtifactCount,
    string LatestVersion,
    DateTime LatestCreatedAtUtc,
    bool HasRequest,
    bool HasReply,
    IReadOnlyList<ContractArtifact> Artifacts);

public sealed record ContractArtifactVersionGroup(
    string Key,
    string Name,
    string Topic,
    string VersionNumber,
    DateTime CreatedAtUtc,
    bool HasEvent,
    bool HasRequest,
    bool HasReply,
    IReadOnlyList<ContractArtifact> Artifacts);

public sealed record ContractArtifactVersionDetail(
    string Key,
    string VersionNumber,
    string Name,
    string Topic,
    DateTime CreatedAtUtc,
    IReadOnlyList<ContractArtifactDetail> Artifacts);

public sealed record ContractArtifactCard(
    string Key,
    string ContractKind,
    string Name,
    string Topic,
    string VersionNumber,
    string SourceStatus,
    DateTime CreatedAtUtc,
    string SchemaSummary,
    IReadOnlyList<ContractArtifact> Artifacts);

public sealed record ContractArtifactBundleDetail(
    string Key,
    string ContractKind,
    string Name,
    string Topic,
    string VersionNumber,
    string SourceStatus,
    DateTime CreatedAtUtc,
    string SchemaSummary,
    IReadOnlyList<ContractArtifactDetail> Artifacts);

public sealed record ContractArtifactDetail(
    Guid Id,
    string Label,
    string ArtifactType,
    string Name,
    string Topic,
    string VersionNumber,
    string ContentHash,
    string SourceStatus,
    DateTime CreatedAtUtc,
    string PayloadSchemaJson);
