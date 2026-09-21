namespace KnOwl.Tests;

public sealed class DocumentationWebUiMarkupTests
{
    private static readonly string[] CollectionPages =
    [
        "Index.cshtml",
        "Space.cshtml",
        "Topic.cshtml"
    ];

    [Fact]
    public void DocumentationCollectionPagesUseCardGrids()
    {
        var pagesPath = GetDocumentationPagesPath();

        foreach (var page in CollectionPages)
        {
            var markup = File.ReadAllText(Path.Combine(pagesPath, page));

            Assert.Contains("od-grid od-documentation-grid", markup);
            Assert.Contains("od-item-card", markup);
            Assert.Contains("data-open-url", markup);
            Assert.DoesNotContain("event-list-card", markup);
            Assert.DoesNotContain("compact-contract-list", markup);
            Assert.DoesNotContain("platform-card", markup);
        }
    }

    [Fact]
    public void DocumentationVersionPageUsesVersionCards()
    {
        var markup = File.ReadAllText(Path.Combine(GetDocumentationPagesPath(), "Page.cshtml"));

        Assert.Contains("od-grid od-versions-grid", markup);
        Assert.Contains("od-item-card od-version-card", markup);
        Assert.Contains("data-open-url", markup);
        Assert.DoesNotContain("event-list-card", markup);
        Assert.DoesNotContain("compact-contract-list", markup);
        Assert.DoesNotContain("platform-card", markup);
    }

    [Fact]
    public void DocumentationReaderUsesPanelShell()
    {
        var markup = File.ReadAllText(Path.Combine(GetDocumentationPagesPath(), "View.cshtml"));

        Assert.Contains("od-panel", markup);
        Assert.Contains("documentation-reader", markup);
        Assert.Contains("data-history-back", markup);
        Assert.DoesNotContain("asp-page=\"/Documentation/Page\"", markup);
        Assert.DoesNotContain("platform-card", markup);
    }

    private static string GetDocumentationPagesPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "KnOwl.Documentation.WebUI",
                "Pages",
                "Documentation");

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Documentation Razor pages.");
    }
}
