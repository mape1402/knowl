using System.Net;
using KnOwl.ControlPlane.Bootstrap;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using KnOwl.Documentation.Storage.EntityFramework.Data;
using KnOwl.Runtime.Bootstrap;
using KnOwl.Runtime.Storage.EntityFramework.Data;
using KnOwl.Security;
using KnOwl.Security.Subjects;
using KnOwl.Security.Storage.EntityFramework.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace KnOwl.Tests;

public sealed class BootstrapCompositionCoverageTests
{
    [Fact]
    public async Task ControlPlaneBootstrapRegistersMapsAndServesCoreEndpoints()
    {
        await using var fixture = await StartControlPlane();

        await AssertStatus(fixture.Client.GetAsync("/health"), HttpStatusCode.OK);
        await AssertStatus(fixture.Client.GetAsync("/healthz"), HttpStatusCode.OK);
        await AssertStatus(fixture.Client.GetAsync("/api/v1/control-plane/schema-types"), HttpStatusCode.OK);

        var options = fixture.App.Services.GetRequiredService<KnOwlControlPlaneBootstrapOptions>();
        var security = fixture.App.Services.GetRequiredService<IOptions<KnOwlSecurityOptions>>().Value;
        var theme = fixture.App.Services.GetRequiredService<IOptions<KnOwl.ControlPlane.WebUI.KnOwlControlPlaneThemeOptions>>().Value;
        Assert.Equal("Coverage Control Plane", options.Theme.Title);
        Assert.Equal("/designer", options.ButterMorphPath);
        Assert.False(security.RequireKnownSubject);
        Assert.False(security.AllowBootstrapAdminSync);
        Assert.Equal("coverage-control", security.Subject.Provider);
        Assert.Equal(["control-subject"], security.Subject.SubjectIdClaimTypes);
        Assert.Equal(["control-name"], security.Subject.DisplayNameClaimTypes);
        Assert.Equal(["control-email"], security.Subject.EmailClaimTypes);
        Assert.Equal(["control-group"], security.Subject.GroupClaimTypes);
        Assert.Contains(security.BootstrapAdmins, admin => admin.Provider == "coverage-control" && admin.SubjectId == "root");
        Assert.Equal("#2563eb", theme.Light.PrimaryColor);
        Assert.Equal("#60a5fa", theme.Dark.PrimaryColor);
    }

    [Fact]
    public async Task RuntimeBootstrapRegistersMapsAndServesCoreEndpoints()
    {
        await using var fixture = await StartRuntime();

        await AssertStatus(fixture.Client.GetAsync("/health"), HttpStatusCode.OK);
        await AssertStatus(fixture.Client.GetAsync("/runtime/status"), HttpStatusCode.OK);
        await AssertStatus(fixture.Client.GetAsync("/api/v1/runtime/status"), HttpStatusCode.OK);

        var options = fixture.App.Services.GetRequiredService<KnOwlRuntimeBootstrapOptions>();
        var security = fixture.App.Services.GetRequiredService<IOptions<KnOwlSecurityOptions>>().Value;
        var theme = fixture.App.Services.GetRequiredService<IOptions<KnOwl.Runtime.WebUI.KnOwlRuntimeThemeOptions>>().Value;
        Assert.Equal("Coverage Runtime", options.Theme.Title);
        Assert.Equal("Runtime host", options.Theme.Subtitle);
        Assert.False(security.RequireKnownSubject);
        Assert.False(security.AllowBootstrapAdminSync);
        Assert.Equal("coverage-runtime", security.Subject.Provider);
        Assert.Equal(["runtime-subject"], security.Subject.SubjectIdClaimTypes);
        Assert.Equal(["runtime-name"], security.Subject.DisplayNameClaimTypes);
        Assert.Equal(["runtime-email"], security.Subject.EmailClaimTypes);
        Assert.Equal(["runtime-group"], security.Subject.GroupClaimTypes);
        Assert.Contains(security.BootstrapAdmins, admin => admin.Provider == "coverage-runtime" && admin.SubjectId == "root");
        Assert.Equal("Coverage Runtime", theme.Title);
        Assert.Equal("Runtime host", theme.Subtitle);
        Assert.Equal("#2563eb", theme.Light.PrimaryColor);
        Assert.Equal("#60a5fa", theme.Dark.PrimaryColor);
    }

    [Fact]
    public void BootstrapStorageConfigurationGuardThrowsWhenAProviderIsMissing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var controlPlaneServices = new ServiceCollection();
        controlPlaneServices.AddKnOwlControlPlane(configuration);
        using var controlPlaneProvider = controlPlaneServices.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => controlPlaneProvider.GetRequiredService<KnOwlDbContext>());
        Assert.Throws<InvalidOperationException>(() => controlPlaneProvider.GetRequiredService<KnOwlDocumentationDbContext>());
        Assert.Throws<InvalidOperationException>(() => controlPlaneProvider.GetRequiredService<KnOwlSecurityDbContext>());

        var runtimeServices = new ServiceCollection();
        runtimeServices.AddKnOwlRuntime(configuration);
        using var runtimeProvider = runtimeServices.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => runtimeProvider.GetRequiredService<KnOwlRuntimeDbContext>());
        Assert.Throws<InvalidOperationException>(() => runtimeProvider.GetRequiredService<KnOwlSecurityDbContext>());
    }

    [Fact]
    public async Task BootstrapMapsProductionPipelines()
    {
        await using var controlPlane = await StartControlPlane(Environments.Production);
        await using var runtime = await StartRuntime(Environments.Production);

        await AssertStatus(controlPlane.Client.GetAsync("/health/ready"), HttpStatusCode.OK);
        await AssertStatus(runtime.Client.GetAsync("/health/ready"), HttpStatusCode.OK);
    }

    private static async Task<AppFixture> StartControlPlane(string? environmentName = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName ?? Environments.Development
        });
        builder.WebHost.UseTestServer();

        var configuration = new ConfigurationBuilder().Build();
        builder.Services.AddKnOwlControlPlane(configuration, options =>
        {
            options.ButterMorphPath = "/designer";
            options.Theme.Title = "Coverage Control Plane";
            options.Theme.Light.PrimaryColor = "#2563eb";
            options.Theme.Dark.PrimaryColor = "#60a5fa";
            options.Security.RequireKnownSubject = false;
            options.Security.AllowBootstrapAdminSync = false;
            options.Security.Subject.Provider = "coverage-control";
            Replace(options.Security.Subject.SubjectIdClaimTypes, ["control-subject"]);
            Replace(options.Security.Subject.DisplayNameClaimTypes, ["control-name"]);
            Replace(options.Security.Subject.EmailClaimTypes, ["control-email"]);
            Replace(options.Security.Subject.GroupClaimTypes, ["control-group"]);
            options.Security.BootstrapAdmins.Add(new KnOwlBootstrapSubject { Provider = "coverage-control", SubjectId = "root" });
            options.ConfigureStorage = db => db.UseInMemoryDatabase($"control-plane-{Guid.NewGuid():N}");
        });

        var app = builder.Build();
        EnsureStaticAssetsManifest();
        app.MapKnOwlControlPlane();
        await app.StartAsync();
        return new AppFixture(app, app.GetTestClient());
    }

    private static void EnsureStaticAssetsManifest()
    {
        var manifestPath = Path.Combine(AppContext.BaseDirectory, "testhost.staticwebassets.endpoints.json");
        if (!File.Exists(manifestPath))
        {
            File.WriteAllText(manifestPath, """{"Version":1,"ManifestType":"Build","Endpoints":[]}""");
        }
    }

    private static async Task<AppFixture> StartRuntime(string? environmentName = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName ?? Environments.Development
        });
        builder.WebHost.UseTestServer();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Runtime:ArtifactPull:Enabled"] = "false"
            })
            .Build();
        builder.Services.AddKnOwlRuntime(configuration, options =>
        {
            options.Theme.Title = "Coverage Runtime";
            options.Theme.Subtitle = "Runtime host";
            options.Theme.Light.PrimaryColor = "#2563eb";
            options.Theme.Dark.PrimaryColor = "#60a5fa";
            options.Security.RequireKnownSubject = false;
            options.Security.AllowBootstrapAdminSync = false;
            options.Security.Subject.Provider = "coverage-runtime";
            Replace(options.Security.Subject.SubjectIdClaimTypes, ["runtime-subject"]);
            Replace(options.Security.Subject.DisplayNameClaimTypes, ["runtime-name"]);
            Replace(options.Security.Subject.EmailClaimTypes, ["runtime-email"]);
            Replace(options.Security.Subject.GroupClaimTypes, ["runtime-group"]);
            options.Security.BootstrapAdmins.Add(new KnOwlBootstrapSubject { Provider = "coverage-runtime", SubjectId = "root" });
            options.ConfigureStorage = db => db.UseInMemoryDatabase($"runtime-{Guid.NewGuid():N}");
        });

        var app = builder.Build();
        app.MapKnOwlRuntime();
        await app.StartAsync();
        return new AppFixture(app, app.GetTestClient());
    }

    private static void Replace(List<string> target, IEnumerable<string> values)
    {
        target.Clear();
        target.AddRange(values);
    }

    private static async Task AssertStatus(Task<HttpResponseMessage> task, HttpStatusCode expected)
    {
        using var response = await task;
        Assert.Equal(expected, response.StatusCode);
    }

    private sealed class AppFixture(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public WebApplication App { get; } = app;
        public HttpClient Client { get; } = client;

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.DisposeAsync();
        }
    }
}
