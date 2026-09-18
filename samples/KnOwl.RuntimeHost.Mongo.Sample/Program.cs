using KnOwl.Runtime.Bootstrap;
using KnOwl.Runtime.Storage.EntityFramework.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = FirstConfiguredConnectionString(
        builder.Configuration.GetConnectionString("KnOwlRuntimeDb"),
        builder.Configuration.GetConnectionString("KnOwlDb"))
    ?? throw new InvalidOperationException("ConnectionStrings:KnOwlRuntimeDb or ConnectionStrings:KnOwlDb is required.");
var databaseName = builder.Configuration["Mongo:DatabaseName"];
if (string.IsNullOrWhiteSpace(databaseName))
{
    throw new InvalidOperationException("Mongo:DatabaseName is required.");
}

builder.Services.AddKnOwlRuntime(builder.Configuration, options =>
{
    options.Theme.Title = "Sample KnOwl";
    options.Theme.Mode = KnOwl.Runtime.WebUI.KnOwlThemeMode.Dark;
    options.Theme.Subtitle = "Contract storage";
    options.Theme.IconImageUrl = "/_content/KnOwl.Runtime.WebUI/img/knowl-icon.png";
    options.Theme.Light.PrimaryColor = "#2563eb";
    options.Theme.Light.PrimaryHoverColor = "#1d4ed8";
    options.Theme.Light.SidebarBackgroundColor = "#0f2f5f";
    options.Theme.Light.SidebarTextColor = "#ffffff";
    options.Theme.Light.SidebarMutedTextColor = "#bfdbfe";
    options.Theme.Dark.PrimaryColor = "#60a5fa";
    options.Theme.Dark.PrimaryHoverColor = "#93c5fd";
    options.Theme.Dark.SidebarBackgroundColor = "#0f172a";
    options.Theme.Dark.SidebarTextColor = "#f8fafc";
    options.Theme.Dark.SidebarMutedTextColor = "#bfdbfe";
    options.ConfigureStorage = db => db.UseMongoDB(connectionString, databaseName);
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KnOwlRuntimeDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapKnOwlRuntime();

app.Run();

static string? FirstConfiguredConnectionString(params string?[] values)
    => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

/// <summary>
/// MongoDB Runtime host program marker used by integration tests.
/// </summary>
public partial class Program;
