using Microsoft.Extensions.Options;

namespace KnOwl.ControlPlane.WebUI.Navigation;

public class MenuService : IMenuService
{
    public IList<MenuItem> Items { get; } = new List<MenuItem>();

    public MenuService(IOptions<KnOwlControlPlaneHomeOptions> homeOptions)
    {
        Items.Add(new MenuItem { Text = "Home", Url = NormalizePath(homeOptions.Value.Path), Icon = "house-fill" });

        var apis = new MenuItem { Text = "APIs", Icon = "journal-code" };
        apis.Add("Overview", "/apis", "list-ul")
            .Add("REST Catalog", "/apis/rest", "braces")
            .Add("gRPC Services", "/apis/grpc", "hdd-network");
        Items.Add(apis);

        var contracts = new MenuItem { Text = "Async Contracts", Icon = "arrow-left-right" };
        contracts.Add("Custom Fields", "/contracts/metadatafields", "ui-checks")
                 .Add("Data Types", "/contracts/types", "tag")
                 .Add("Type Bundles", "/contracts/typebundles", "collection")
                 .Add("Type Mappings", "/contracts/typemappings", "braces-asterisk")
                 .Add("Events", "/contracts/events", "broadcast")
                 .Add("Commands", "/contracts/commands", "terminal");
        Items.Add(contracts);

        var distribution = new MenuItem { Text = "Distribution", Icon = "diagram-3" };
        distribution.Add("Environments", "/contracts/runtimeenvironments", "diagram-3")
                    .Add("Runtime Nodes", "/contracts/runtimenodes", "hdd-network")
                    .Add("Artifacts", "/contracts/contractartifacts", "box")
                    .Add("Releases", "/contracts/contractreleases", "rocket");
        Items.Add(distribution);

        var nuget = new MenuItem { Text = "NuGet Feeds", Icon = "box-seam" };
        nuget.Add("Packages", "/nuget/packages", "archive")
             .Add("Feed Config", "/nuget/config", "gear");
        Items.Add(nuget);

        var documentation = new MenuItem { Text = "Documentation", Icon = "book" };
        documentation.Add("Spaces", "/documentation/spaces", "journal-richtext");
        Items.Add(documentation);

        Items.Add(new MenuItem { Text = "Privacy", Url = "/Privacy", Icon = "shield-check" });
    }

    private static string NormalizePath(string? path)
        => string.IsNullOrWhiteSpace(path) || !path.StartsWith("/", StringComparison.Ordinal)
            ? "/"
            : path;
}

