namespace KnOwl.ControlPlane.WebUI.Navigation;

public class MenuService : IMenuService
{
    public IList<MenuItem> Items { get; } = new List<MenuItem>();

    public MenuService()
    {
        Items.Add(new MenuItem { Text = "Home", Url = "/", Icon = "house-fill" });

        var apis = new MenuItem { Text = "APIs", Icon = "journal-code" };
        apis.Add("Overview", "/apis", "list-ul")
            .Add("REST Catalog", "/apis/rest", "braces")
            .Add("gRPC Services", "/apis/grpc", "hdd-network");
        Items.Add(apis);

        var contracts = new MenuItem { Text = "Async Contracts", Icon = "arrow-left-right" };
        contracts.Add("Custom Fields", "/contracts/metadatafields", "ui-checks")
                 .Add("Data Types", "/contracts/types", "tag")
                 .Add("Events", "/contracts/events", "broadcast")
                 .Add("Commands", "/contracts/commands", "terminal")
                 .Add("Artifacts", "/contracts/contractartifacts", "box")
                 .Add("Environments", "/contracts/runtimeenvironments", "diagram-3")
                 .Add("Runtime Nodes", "/contracts/runtimenodes", "hdd-network")
                 .Add("Releases", "/contracts/contractreleases", "rocket");
        Items.Add(contracts);

        var nuget = new MenuItem { Text = "NuGet Feeds", Icon = "box-seam" };
        nuget.Add("Packages", "/nuget/packages", "archive")
             .Add("Feed Config", "/nuget/config", "gear");
        Items.Add(nuget);

        var guides = new MenuItem { Text = "Guides", Icon = "book" };
        guides.Add("Architecture", "/guides/architecture", "diagram-3")
              .Add("Standards", "/guides/standards", "check2-square")
              .Add("Runbooks", "/guides/runbooks", "play-circle");
        Items.Add(guides);

        Items.Add(new MenuItem { Text = "Privacy", Url = "/Privacy", Icon = "shield-check" });
    }
}

