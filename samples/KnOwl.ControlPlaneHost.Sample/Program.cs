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
    options.Theme.IconImageUrl = "/_content/KnOwl.ControlPlane.WebUI/img/knowl-icon.png";
    options.Theme.PrimaryColor = "#2563eb";
    options.Theme.PrimaryHoverColor = "#1d4ed8";
    options.Theme.SidebarBackgroundColor = "#0f2f5f";
    options.Theme.SidebarBrandBackgroundColor = "#0b2347";
    options.Theme.SidebarTextColor = "#ffffff";
    options.Theme.SidebarMutedTextColor = "#bfdbfe";
    options.Theme.ContentBackgroundColor = "#f5f9ff";
    options.Theme.SurfaceColor = "#ffffff";
    options.Theme.TextColor = "#102033";
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
