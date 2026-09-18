using KnOwl.ControlPlane.Bootstrap;
using KnOwl.ControlPlaneHost.Sample.Documentation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var migrationsAssembly = typeof(Program).Assembly.GetName().Name!;
var connectionString = builder.Configuration.GetConnectionString("KnOwlDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:KnOwlDb is required.");
}

builder.Services.AddKnOwlControlPlane(builder.Configuration, options =>
{
    options.MigrationsAssembly = migrationsAssembly;
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
    options.ConfigureStorage = db => db.UseSqlServer(
        connectionString,
        sql => sql.MigrationsAssembly(migrationsAssembly));
});

var app = builder.Build();

await SampleDocumentationSeeder.Initialize(app);

app.MapKnOwlControlPlane();

app.Run();

/// <summary>
/// Control Plane host program marker used by integration tests.
/// </summary>
public partial class Program;
