namespace KnOwl.Tests;

public sealed class RuntimeAndHomeWebUiMarkupTests
{
    [Fact]
    public void RuntimeLayoutUsesControlPlaneShellPattern()
    {
        var markup = File.ReadAllText(Path.Combine(GetRuntimePagesPath(), "Shared", "_Layout.cshtml"));

        Assert.Contains("app-shell", markup);
        Assert.Contains("sidebar-toggle", markup);
        Assert.Contains("nav-item-link", markup);
        Assert.Contains("content-shell", markup);
        Assert.DoesNotContain("runtime-shell", markup);
        Assert.DoesNotContain("runtime-sidebar", markup);
        Assert.DoesNotContain("runtime-topbar", markup);
    }

    [Fact]
    public void RuntimeArtifactsPageUsesCards()
    {
        var markup = File.ReadAllText(Path.Combine(GetRuntimePagesPath(), "Artifacts", "Index.cshtml"));

        Assert.Contains("od-page-head", markup);
        Assert.Contains("runtime-filter-panel", markup);
        Assert.Contains("od-grid runtime-artifact-grid", markup);
        Assert.Contains("od-item-card", markup);
        Assert.Contains("data-open-url", markup);
        Assert.DoesNotContain("Published catalog", markup);
        Assert.DoesNotContain("<table", markup);
        Assert.DoesNotContain("<th>", markup);
        Assert.DoesNotContain("<td>", markup);
    }

    [Fact]
    public void RuntimeControlPlanesPageUsesControlPlaneCardPattern()
    {
        var markup = File.ReadAllText(Path.Combine(GetRuntimePagesPath(), "ControlPlanes", "Index.cshtml"));

        Assert.Contains("od-page-head", markup);
        Assert.Contains("runtime-filter-panel", markup);
        Assert.Contains("od-grid runtime-connection-grid", markup);
        Assert.Contains("od-item-card runtime-connection-card", markup);
        Assert.DoesNotContain("eyebrow", markup);
        Assert.DoesNotContain("card-heading", markup);
    }

    [Fact]
    public void RuntimeOverviewUsesDashboardCards()
    {
        var markup = File.ReadAllText(Path.Combine(GetRuntimePagesPath(), "Index.cshtml"));

        Assert.Contains("od-page-head", markup);
        Assert.Contains("runtime-metric-grid", markup);
        Assert.Contains("runtime-dashboard-grid", markup);
        Assert.DoesNotContain("runtime-hero", markup);
    }

    [Fact]
    public void ControlPlaneHomeUsesEnglishTextOnly()
    {
        var markup = File.ReadAllText(Path.Combine(GetControlPlanePagesPath(), "Index.cshtml"));
        var spanishFragments = new[]
        {
            "Explora",
            "Resumen",
            "registradas",
            "Contratos async",
            "Primeros pasos",
            "Integra paquetes",
            "Paquetes",
            "configura",
            "agrega",
            "proyecto"
        };

        foreach (var fragment in spanishFragments)
        {
            Assert.DoesNotContain(fragment, markup, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string GetRuntimePagesPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "KnOwl.Runtime.WebUI", "Pages");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Runtime Razor pages.");
    }

    private static string GetControlPlanePagesPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "KnOwl.ControlPlane.WebUI", "Pages");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Control Plane Razor pages.");
    }
}
