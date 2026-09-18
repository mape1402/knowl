using System.Net;
using System.Net.Http.Json;
using System.Text;
using KnOwl.Documentation;
using KnOwl.Documentation.Api;
using KnOwl.Documentation.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Tests;

public sealed class DocumentationApiEndpointTests
{
    [Fact]
    public async Task BrowseEndpointReturnsRenderedDocumentationHtml()
    {
        var service = new DocumentationService();
        await using var app = await CreateApp(service);
        using var http = CreateClient(app);

        using var response = await http.GetAsync("/docs/orchestrator/guides/setup/1.0.0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("<h1>Setup</h1>", html);
    }

    [Fact]
    public async Task UploadEndpointImportsDocumentationVersion()
    {
        var service = new DocumentationService();
        await using var app = await CreateApp(service);
        using var http = CreateClient(app);
        using MultipartFormDataContent form = new();
        form.Add(new StringContent("2.0.0"), "versionNumber");
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("# Updated")), "file", "index.md");

        using var response = await http.PostAsync($"/api/v1/documentation/pages/{service.Page.Id}/versions", form);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("2.0.0", service.Imported.Single().VersionNumber);
    }

    [Fact]
    public async Task SourceDownloadReturnsMarkdown()
    {
        var service = new DocumentationService();
        await using var app = await CreateApp(service);
        using var http = CreateClient(app);

        using var response = await http.GetAsync("/docs/orchestrator/guides/setup/1.0.0/source.md");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("# Setup", await response.Content.ReadAsStringAsync());
    }

    private static async Task<WebApplication> CreateApp(IDocumentationInteractionService documentation)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(documentation);

        var app = builder.Build();
        app.MapKnOwlDocumentationApi();
        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()
            ?? throw new InvalidOperationException("The test server did not expose an address.");
        return new HttpClient { BaseAddress = new Uri(addresses.Addresses.Single()) };
    }

    private sealed class DocumentationService : IDocumentationInteractionService
    {
        public DocumentationPage Page { get; } = new() { Id = Guid.NewGuid(), Key = "setup", Title = "Setup" };
        public List<DocumentationPageVersion> Imported { get; } = [];
        private readonly DocumentationPageVersion version;
        private readonly DocumentationAsset source;

        public DocumentationService()
        {
            version = new DocumentationPageVersion
            {
                Id = Guid.NewGuid(),
                Page = Page,
                PageId = Page.Id,
                VersionNumber = "1.0.0",
                EntryPath = "index.md",
                Status = DocPageVersionStatus.Published
            };
            source = new DocumentationAsset
            {
                Id = Guid.NewGuid(),
                PageVersion = version,
                PageVersionId = version.Id,
                Kind = DocAssetKind.SourceMarkdown,
                LogicalPath = "index.md",
                FileName = "index.md",
                ContentType = "text/markdown",
                StorageKey = "source",
                IsDownloadable = true
            };
            version.Assets.Add(source);
            Page.Versions.Add(version);
        }

        public Task<IReadOnlyList<DocumentationSpace>> GetSpaces(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentationSpace>>([]);

        public Task<DocumentationSpace?> GetSpace(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentationSpace?>(null);

        public Task<DocumentationSpace> UpsertSpace(string key, string name, string? description, bool isActive, CancellationToken cancellationToken = default)
            => Task.FromResult(new DocumentationSpace { Key = key, Name = name, Description = description, IsActive = isActive });

        public Task<IReadOnlyList<DocumentationTopic>> GetTopics(string spaceKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentationTopic>>([]);

        public Task<DocumentationTopic?> GetTopic(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentationTopic?>(null);

        public Task<DocumentationTopic> UpsertTopic(string spaceKey, string key, string name, string? description, bool isActive, CancellationToken cancellationToken = default)
            => Task.FromResult(new DocumentationTopic { Key = key, Name = name, Description = description, IsActive = isActive });

        public Task<IReadOnlyList<DocumentationPage>> GetPages(string spaceKey, string topicKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentationPage>>([Page]);

        public Task<DocumentationPage?> GetPage(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentationPage?>(id == Page.Id ? Page : null);

        public Task<DocumentationPage> UpsertPage(string spaceKey, string topicKey, string key, string title, string? description, bool isActive, CancellationToken cancellationToken = default)
            => Task.FromResult(Page);

        public async Task<DocumentationPageVersion> ImportVersion(DocumentationVersionInput input, CancellationToken cancellationToken = default)
        {
            using StreamReader reader = new(input.Content, Encoding.UTF8);
            var markdown = await reader.ReadToEndAsync(cancellationToken);
            var imported = new DocumentationPageVersion
            {
                Id = Guid.NewGuid(),
                PageId = input.PageId,
                Page = Page,
                VersionNumber = input.VersionNumber,
                EntryPath = input.EntryPath ?? input.FileName,
                ContentHash = markdown
            };
            Imported.Add(imported);
            return imported;
        }

        public Task PublishVersion(Guid versionId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ArchiveVersion(Guid versionId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<RenderedDocumentation?> Render(string spaceKey, string topicKey, string pageKey, string? versionNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<RenderedDocumentation?>(new RenderedDocumentation(version, source, "# Setup", "<h1>Setup</h1>"));

        public Task<DocumentationAsset?> GetAsset(Guid assetId, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentationAsset?>(assetId == source.Id ? source : null);

        public Task<Stream> OpenAsset(DocumentationAsset asset, CancellationToken cancellationToken = default)
            => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("# Setup")));

        public Task<(string FileName, Stream Content)> BuildSourcePackage(Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult<(string, Stream)>(("setup.zip", new MemoryStream([1, 2, 3])));

        public Task<(string FileName, Stream Content)> BuildPdf(Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult<(string, Stream)>(("setup.pdf", new MemoryStream([1, 2, 3])));
    }
}
