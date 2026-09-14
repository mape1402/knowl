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
    public IReadOnlyList<string> ArtifactTypes { get; } = Enum.GetNames<ContractArtifactType>();

    /// <summary>
    /// Gets the total artifact count before filters are applied.
    /// </summary>
    public int TotalArtifacts { get; private set; }

    /// <summary>
    /// Gets the artifacts matching the active filters.
    /// </summary>
    public IReadOnlyList<RuntimeContractArtifact> FilteredArtifacts { get; private set; } = [];

    /// <summary>
    /// Loads artifacts and applies user-selected filters.
    /// </summary>
    public async Task OnGet(CancellationToken cancellationToken)
    {
        var artifacts = await catalog.GetAll(cancellationToken);
        TotalArtifacts = artifacts.Count;

        IEnumerable<RuntimeContractArtifact> query = artifacts;
        if (Enum.TryParse<ContractArtifactType>(Type, ignoreCase: true, out var artifactType))
        {
            query = query.Where(x => x.ArtifactType == artifactType);
            Type = artifactType.ToString();
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

    private static bool Contains(string value, string term)
        => value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
