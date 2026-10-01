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

            Assert.Contains("documentation-card-grid", markup);
            Assert.Contains("documentation-card", markup);
            Assert.Contains("data-open-url", markup);
            Assert.DoesNotContain("od-grid od-documentation-grid", markup);
            Assert.DoesNotContain("od-item-card", markup);
            Assert.DoesNotContain("event-list-card", markup);
            Assert.DoesNotContain("compact-contract-list", markup);
            Assert.DoesNotContain("platform-card", markup);
        }
    }

    [Fact]
    public void DocumentationVersionPageUsesVersionCards()
    {
        var markup = File.ReadAllText(Path.Combine(GetDocumentationPagesPath(), "Page.cshtml"));

        Assert.Contains("documentation-card-grid documentation-version-grid", markup);
        Assert.Contains("documentation-card documentation-version-card", markup);
        Assert.Contains("data-open-url", markup);
        Assert.DoesNotContain("od-grid od-versions-grid", markup);
        Assert.DoesNotContain("od-item-card od-version-card", markup);
        Assert.DoesNotContain("event-list-card", markup);
        Assert.DoesNotContain("compact-contract-list", markup);
        Assert.DoesNotContain("platform-card", markup);
    }

    [Fact]
    public void DocumentationCardsDoNotRenderRedundantOpenButtons()
    {
        var pagesPath = GetDocumentationPagesPath();
        var cardPages = CollectionPages.Append("Page.cshtml");

        foreach (var page in cardPages)
        {
            var markup = File.ReadAllText(Path.Combine(pagesPath, page));

            Assert.DoesNotContain(">Open<", markup);
            Assert.DoesNotContain(">Browse<", markup);
            Assert.DoesNotContain("documentation-card-actions", markup);
        }
    }

    [Fact]
    public void DocumentationPageCardsExposeVersionsInContextMenu()
    {
        var markup = File.ReadAllText(Path.Combine(GetDocumentationPagesPath(), "Topic.cshtml"));

        Assert.Contains("aria-label=\"Page actions\"", markup);
        Assert.Contains("dropdown-menu dropdown-menu-end", markup);
        Assert.Contains("asp-page=\"/Documentation/Page\"", markup);
        Assert.Contains(">Versions<", markup);
    }

    [Fact]
    public void DocumentationReaderUsesPanelShell()
    {
        var markup = File.ReadAllText(Path.Combine(GetDocumentationPagesPath(), "View.cshtml"));

        Assert.Contains("od-panel", markup);
        Assert.Contains("documentation-reader", markup);
        Assert.Contains("documentation-toc", markup);
        Assert.Contains("documentation-toc-link", markup);
        Assert.Contains("data-history-back", markup);
        Assert.Contains("data-documentation-focus-toggle", markup);
        Assert.Contains("documentation-focus-controls", markup);
        Assert.Contains("data-theme-toggle", markup);
        Assert.Contains("data-documentation-focus-exit", markup);
        Assert.Contains("tabindex=\"-1\"", markup);
        Assert.DoesNotContain("asp-page=\"/Documentation/Page\"", markup);
        Assert.DoesNotContain("platform-card", markup);
    }

    [Fact]
    public void DocumentationReaderUsesIndependentScrollPanesOnDesktop()
    {
        var css = File.ReadAllText(GetControlPlaneCssPath());

        Assert.Contains(".documentation-reader-layout.has-toc", css);
        Assert.Contains("grid-template-columns: minmax(0, 1fr) 240px;", css);
        Assert.Contains(".content-body:has(.documentation-render-panel)", css);
        Assert.Contains("overflow: hidden;", css);
        Assert.Contains("height: 100%;", css);
        Assert.Contains("overflow-y: auto;", css);
        Assert.Contains("scrollbar-width: thin;", css);

        var tocBlock = System.Text.RegularExpressions.Regex.Match(css, @"\.documentation-toc\s*\{[^}]+\}").Value;

        Assert.NotEmpty(tocBlock);
        Assert.DoesNotContain("position: fixed;", tocBlock);
    }

    [Fact]
    public void DocumentationReaderSupportsFullPageFocusMode()
    {
        var css = File.ReadAllText(GetControlPlaneCssPath());
        var js = File.ReadAllText(GetControlPlaneJsPath());

        Assert.Contains(".app-shell.documentation-focus-mode .sidebar", css);
        Assert.Contains(".app-shell.documentation-focus-mode .topbar", css);
        Assert.Contains(".app-shell.documentation-focus-mode .content-footer", css);
        Assert.Contains(".documentation-focus-controls", css);
        Assert.Contains(".documentation-focus-exit", css);
        Assert.Contains("grid-template-columns: 260px minmax(0, 1fr);", css);
        Assert.Contains("grid-column: 2;", css);
        Assert.Contains("position: fixed;", css);
        Assert.Contains("width: 100%;", css);
        Assert.Contains("margin: 0;", css);

        Assert.Contains("documentation-focus-mode", js);
        Assert.Contains("data-documentation-focus-toggle", js);
        Assert.Contains("data-documentation-focus-exit", js);
        Assert.Contains("data-theme-toggle", js);
        Assert.Contains("Escape", js);
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

    private static string GetControlPlaneCssPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "KnOwl.ControlPlane.WebUI",
                "wwwroot",
                "css",
                "site.css");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate Control Plane site.css.");
    }

    private static string GetControlPlaneJsPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "KnOwl.ControlPlane.WebUI",
                "wwwroot",
                "js",
                "site.js");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate Control Plane site.js.");
    }
}
