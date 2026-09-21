using KnOwl.Contracts.Artifacts;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Application.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Runtime.WebUI.Pages.Artifacts;

/// <summary>
/// Lists contract artifacts currently stored in KnOwl Runtime.
/// </summary>
public sealed class IndexModel(IRuntimeContractCatalogService catalog) : PageModel
{
    /// <summary>
    /// Gets the selected artifact type filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Type { get; set; }

    /// <summary>
    /// Gets the free-text artifact search term.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    /// <summary>
    /// Gets the supported artifact type names.
    /// </summary>
    public IReadOnlyList<string> ArtifactTypes { get; } = ["Event", "Command"];

    /// <summary>
    /// Gets the total artifact count before filters are applied.
    /// </summary>
    public int TotalArtifacts { get; private set; }

    /// <summary>
    /// Gets the artifacts matching the active filters.
    /// </summary>
    public IReadOnlyList<RuntimeArtifactListItem> FilteredArtifacts { get; private set; } = [];

    /// <summary>
    /// Loads artifacts and applies user-selected filters.
    /// </summary>
    public async Task OnGet(CancellationToken cancellationToken)
    {
        var artifacts = ToLogicalArtifacts(await catalog.GetAll(cancellationToken));
        TotalArtifacts = artifacts.Count;

        IEnumerable<RuntimeArtifactListItem> query = artifacts;
        if (TryNormalizeDisplayType(Type, out var displayType))
        {
            query = query.Where(x => string.Equals(x.ArtifactType, displayType, StringComparison.OrdinalIgnoreCase));
            Type = displayType;
        }
        else
        {
            Type = null;
        }

        if (!string.IsNullOrWhiteSpace(Query))
        {
            var term = Query.Trim();
            query = query.Where(x =>
                Contains(x.Topic, term)
                || Contains(x.Name, term)
                || Contains(x.VersionNumber, term)
                || Contains(x.ContentHash, term));
            Query = term;
        }

        FilteredArtifacts = query.ToList();
    }

    private static IReadOnlyList<RuntimeArtifactListItem> ToLogicalArtifacts(IReadOnlyList<RuntimeContractArtifact> artifacts)
    {
        var commandIdentities = artifacts
            .Where(x => x.ArtifactType == ContractArtifactType.Command)
            .Select(ArtifactIdentity)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var items = artifacts
            .Where(x => x.ArtifactType is ContractArtifactType.Event or ContractArtifactType.Command)
            .Select(CreateListItem)
            .ToList();

        var legacyCommandItems = artifacts
            .Where(x => x.ArtifactType is ContractArtifactType.CommandRequest or ContractArtifactType.CommandReply)
            .GroupBy(ArtifactIdentity, StringComparer.OrdinalIgnoreCase)
            .Where(group => !commandIdentities.Contains(group.Key))
            .Select(group =>
            {
                var primary = group
                    .OrderBy(x => x.ArtifactType == ContractArtifactType.CommandRequest ? 0 : 1)
                    .ThenByDescending(x => x.DeployedAtUtc)
                    .First();

                return CreateListItem(primary, "Command");
            });

        items.AddRange(legacyCommandItems);
        return items
            .OrderByDescending(x => x.DeployedAtUtc)
            .ThenBy(x => x.Name)
            .ToList();
    }

    private static RuntimeArtifactListItem CreateListItem(RuntimeContractArtifact artifact)
        => CreateListItem(artifact, artifact.ArtifactType == ContractArtifactType.Event ? "Event" : "Command");

    private static RuntimeArtifactListItem CreateListItem(RuntimeContractArtifact artifact, string displayType)
        => new(
            artifact.Id,
            displayType,
            displayType == "Command" ? NormalizeCommandName(artifact.Name) : artifact.Name,
            artifact.Topic,
            artifact.VersionNumber,
            artifact.Description ?? string.Empty,
            artifact.ContentHash,
            artifact.SourceArtifactId.ToString("N"),
            artifact.DeployedAtUtc);

    private static string NormalizeCommandName(string name)
    {
        const StringComparison comparison = StringComparison.OrdinalIgnoreCase;
        if (name.EndsWith(" Request", comparison))
        {
            return name[..^" Request".Length];
        }

        if (name.EndsWith(" Reply", comparison))
        {
            return name[..^" Reply".Length];
        }

        return name;
    }

    private static string ArtifactIdentity(RuntimeContractArtifact artifact)
        => $"{artifact.DefinitionId:N}:{artifact.VersionId:N}:{artifact.Topic}:{artifact.VersionNumber}";

    private static bool TryNormalizeDisplayType(string? value, out string displayType)
    {
        if (string.Equals(value, "Event", StringComparison.OrdinalIgnoreCase))
        {
            displayType = "Event";
            return true;
        }

        if (string.Equals(value, "Command", StringComparison.OrdinalIgnoreCase))
        {
            displayType = "Command";
            return true;
        }

        displayType = string.Empty;
        return false;
    }

    private static bool Contains(string value, string term)
        => value.Contains(term, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Represents one logical runtime artifact row shown by the Runtime catalog UI.
/// </summary>
public sealed record RuntimeArtifactListItem(
    Guid Id,
    string ArtifactType,
    string Name,
    string Topic,
    string VersionNumber,
    string Description,
    string ContentHash,
    string SourceArtifactId,
    DateTime DeployedAtUtc);
