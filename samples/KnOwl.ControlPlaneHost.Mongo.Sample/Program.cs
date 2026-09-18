using KnOwl.ControlPlane.Bootstrap;
using KnOwl.ControlPlaneHost.Mongo.Sample.Documentation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("KnOwlDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:KnOwlDb is required.");
}

var databaseName = builder.Configuration["Mongo:DatabaseName"];
if (string.IsNullOrWhiteSpace(databaseName))
{
    throw new InvalidOperationException("Mongo:DatabaseName is required.");
}

builder.Services.AddKnOwlControlPlane(builder.Configuration, options =>
{
    options.Theme.Title = "Sample KnOwl";
    options.Theme.Mode = KnOwl.ControlPlane.WebUI.KnOwlThemeMode.Dark;
    options.Theme.IconImageUrl = "/_content/KnOwl.ControlPlane.WebUI/img/knowl-icon.png";
    options.Theme.Light.PrimaryColor = "#2563eb";
    options.Theme.Light.PrimaryHoverColor = "#1d4ed8";
    options.Theme.Light.SidebarBackgroundColor = "#0f2f5f";
    options.Theme.Light.SidebarBrandBackgroundColor = "#0b2347";
    options.Theme.Light.SidebarTextColor = "#ffffff";
    options.Theme.Light.SidebarMutedTextColor = "#bfdbfe";
    options.Theme.Dark.PrimaryColor = "#60a5fa";
    options.Theme.Dark.PrimaryHoverColor = "#93c5fd";
    options.Theme.Dark.SidebarBackgroundColor = "#0f172a";
    options.Theme.Dark.SidebarBrandBackgroundColor = "#0b1220";
    options.Theme.Dark.SidebarTextColor = "#f8fafc";
    options.Theme.Dark.SidebarMutedTextColor = "#bfdbfe";
    options.ConfigureStorage = db => db.UseMongoDB(connectionString, databaseName);
});

var app = builder.Build();

await SampleDocumentationSeeder.Initialize(app);

app.MapKnOwlControlPlane();

app.Run();

/// <summary>
/// MongoDB Control Plane host program marker used by integration tests.
/// </summary>
public partial class Program;
