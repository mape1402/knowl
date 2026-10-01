namespace KnOwl.Tests;

public sealed class ControlPlaneDistributionWebUiMarkupTests
{
    [Fact]
    public void DistributionPagesUseCardBoards()
    {
        var pagesPath = GetControlPlanePagesPath();

        var environments = File.ReadAllText(Path.Combine(pagesPath, "Contracts", "RuntimeEnvironments", "Index.cshtml"));
        var runtimeNodes = File.ReadAllText(Path.Combine(pagesPath, "Contracts", "RuntimeNodes", "Index.cshtml"));
        var artifacts = File.ReadAllText(Path.Combine(pagesPath, "Contracts", "ContractArtifacts", "Index.cshtml"));
        var releases = File.ReadAllText(Path.Combine(pagesPath, "Contracts", "ContractReleases", "Index.cshtml"));

        Assert.Contains("od-grid", environments);
        Assert.Contains("od-item-card", environments);
        Assert.Contains("od-grid od-runtime-node-grid", runtimeNodes);
        Assert.Contains("od-runtime-node-card", runtimeNodes);
        Assert.Contains("od-grid od-artifact-group-grid", artifacts);
        Assert.Contains("od-grid od-artifact-grid", artifacts);
        Assert.Contains("release-board", releases);
        Assert.Contains("release-card", releases);
    }

    [Fact]
    public void DistributionCardCssKeepsContentInsideCards()
    {
        var css = File.ReadAllText(GetControlPlaneCssPath());

        Assert.Contains(".od-item-card > *", css);
        Assert.Contains(".od-grid.od-artifact-group-grid:not(.od-versions-grid):not(.od-runtime-node-grid) > .od-item-card", css);
        Assert.Contains(".od-card-footer > span", css);
        Assert.Contains(".od-runtime-node-card .od-runtime-card-summary > div", css);
        Assert.Contains(".release-board", css);
        Assert.Contains("grid-template-columns: repeat(auto-fill, minmax(min(100%, 300px), 1fr));", css);
        Assert.Contains(".release-card > *", css);
        Assert.Contains(".release-selected-item > div", css);
        Assert.Contains("text-overflow: ellipsis;", css);
    }

    private static string GetControlPlanePagesPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "KnOwl.ControlPlane.WebUI",
                "Pages");

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Control Plane Razor pages.");
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
}
