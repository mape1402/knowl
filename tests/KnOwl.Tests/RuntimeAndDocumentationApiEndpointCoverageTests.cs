using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.Documentation;
using KnOwl.Documentation.Api;
using KnOwl.Documentation.Application;
using KnOwl.Runtime.Api;
using KnOwl.Runtime.Api.Contracts;
using KnOwl.Runtime.Application.ArtifactDelivery;
using KnOwl.Runtime.Application.Catalog;
using KnOwl.Runtime.Application.Security;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Distribution;
using KnOwl.Runtime.Storage;
using KnOwl.Security.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Tests;

public sealed class RuntimeAndDocumentationApiEndpointCoverageTests
{
    [Fact]
    public async Task RuntimeApiRoutesExecuteArtifactAndControlPlaneFlows()
    {
        await using var fixture = await RuntimeApiFixture.Start();
        var client = fixture.Client;
        var state = fixture.State;

        await AssertStatus(client.GetAsync("/api/v1/runtime/status"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/api/v1/runtime/artifacts/"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/api/v1/runtime/artifacts/events/customer.created/versions/1.0.0"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/api/v1/runtime/artifacts/events/missing/versions/1.0.0"), HttpStatusCode.NotFound);
        await AssertStatus(client.GetAsync("/api/v1/runtime/artifacts/commands/customer.create/versions/1.0.0"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/api/v1/runtime/artifacts/commands/missing/versions/1.0.0"), HttpStatusCode.NotFound);

        await AssertStatus(client.GetAsync("/api/v1/runtime/control-planes/"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync($"/api/v1/runtime/control-planes/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/runtime/control-planes/", new UpsertRuntimeDesignNodeRequest(null, " cp ", " Control Plane ", DistributionMode.Pull, "https://control.example.test/", " remote-1 ", true)), HttpStatusCode.Created);
        var node = state.Nodes.Single();
        await AssertStatus(client.GetAsync($"/api/v1/runtime/control-planes/{node.Id}"), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/runtime/control-planes/", new UpsertRuntimeDesignNodeRequest(node.Id, "cp", "Control Plane Updated", DistributionMode.Hybrid, "https://control2.example.test/", "remote-2", true)), HttpStatusCode.OK);
        await AssertStatus(client.PostAsync($"/api/v1/runtime/control-planes/{node.Id}/credentials/generate?issuerBaseUrl=https%3A%2F%2Fruntime.example.test", null), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/runtime/control-planes/{node.Id}/credentials/import", new ImportRuntimeDesignNodeCredentialPackageRequest(node.Id, "{}")), HttpStatusCode.NoContent);
        await AssertStatus(client.PostAsync($"/api/v1/runtime/control-planes/{node.Id}/connect/validate", null), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/api/v1/runtime/control-planes/cp/artifacts/pending"), HttpStatusCode.OK);
        await AssertStatus(client.PostAsync($"/api/v1/runtime/control-planes/cp/artifacts/{Guid.NewGuid()}/apply", null), HttpStatusCode.OK);
    }

    [Fact]
    public async Task DocumentationApiRoutesExecuteCrudBrowseAndDownloadFlows()
    {
        await using var fixture = await DocumentationApiFixture.Start();
        var client = fixture.Client;
        var state = fixture.State;

        await AssertStatus(client.GetAsync("/api/v1/documentation/spaces"), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/documentation/spaces", new UpsertDocumentationSpaceRequest("docs", "Docs", "Documentation")), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/api/v1/documentation/spaces/docs/topics"), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/documentation/spaces/docs/topics", new UpsertDocumentationTopicRequest("guides", "Guides", "Guides")), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/api/v1/documentation/spaces/docs/topics/guides/pages"), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/documentation/spaces/docs/topics/guides/pages", new UpsertDocumentationPageRequest("intro", "Intro", "Start")), HttpStatusCode.OK);
        var page = state.Pages.Single();

        using var emptyForm = new MultipartFormDataContent();
        emptyForm.Add(new StringContent("1.0.0"), "versionNumber");
        await AssertStatus(client.PostAsync($"/api/v1/documentation/pages/{page.Id}/versions", emptyForm), HttpStatusCode.BadRequest);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("1.0.0"), "versionNumber");
        form.Add(new StringContent("index.md"), "entryPath");
        var file = new ByteArrayContent("# Title\n\nBody"u8.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        form.Add(file, "file", "index.md");
        await AssertStatus(client.PostAsync($"/api/v1/documentation/pages/{page.Id}/versions", form), HttpStatusCode.Created);

        await AssertStatus(client.GetAsync("/docs/docs/guides/intro"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/docs/docs/guides/intro/1.0.0"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/docs/docs/guides/missing/1.0.0"), HttpStatusCode.NotFound);
        await AssertStatus(client.GetAsync("/docs/docs/guides/intro/1.0.0/source.md"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/docs/docs/guides/intro/1.0.0/package.zip"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync($"/docs/assets/{state.Assets.Single().Id}"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync($"/docs/assets/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
    }

    private static async Task AssertStatus(Task<HttpResponseMessage> task, HttpStatusCode expected)
    {
        using var response = await task;
        Assert.Equal(expected, response.StatusCode);
    }

    private sealed class RuntimeApiFixture : IAsyncDisposable
    {
        private readonly WebApplication app;

        private RuntimeApiFixture(WebApplication app, HttpClient client, RuntimeState state)
        {
            this.app = app;
            Client = client;
            State = state;
        }

        public HttpClient Client { get; }
        public RuntimeState State { get; }

        public static async Task<RuntimeApiFixture> Start()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            var state = new RuntimeState();
            builder.Services.AddSingleton(state);
            builder.Services.AddSingleton<IRuntimeContractCatalogService>(state);
            builder.Services.AddSingleton<IRuntimeDesignNodeRepository>(state);
            builder.Services.AddSingleton<IRuntimeDesignNodeConnectionService>(state);
            builder.Services.AddSingleton<IControlPlaneArtifactPullService>(state);
            var app = builder.Build();
            app.MapKnOwlRuntimeApi(new KnOwlApiAuthorizationOptions());
            await app.StartAsync();
            return new RuntimeApiFixture(app, app.GetTestClient(), state);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }

    private sealed class RuntimeState :
        IRuntimeContractCatalogService,
        IRuntimeDesignNodeRepository,
        IRuntimeDesignNodeConnectionService,
        IControlPlaneArtifactPullService
    {
        private readonly RuntimeContractArtifact eventArtifact = CreateArtifact(ContractArtifactType.Event, "customer.created");
        private readonly RuntimeContractArtifact commandArtifact = CreateArtifact(ContractArtifactType.Command, "customer.create");

        public List<RuntimeDesignNode> Nodes { get; } = [];

        Task<IReadOnlyList<RuntimeContractArtifact>> IRuntimeContractCatalogService.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RuntimeContractArtifact>>([eventArtifact, commandArtifact]);
        Task<RuntimeContractArtifact?> IRuntimeContractCatalogService.GetExact(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken) => Task.FromResult<RuntimeContractArtifact?>(artifactType == eventArtifact.ArtifactType && topic == eventArtifact.Topic ? eventArtifact : null);
        Task<RuntimeContractArtifact?> IRuntimeContractCatalogService.GetLatest(ContractArtifactType artifactType, string topic, CancellationToken cancellationToken) => Task.FromResult<RuntimeContractArtifact?>(artifactType == eventArtifact.ArtifactType && topic == eventArtifact.Topic ? eventArtifact : null);
        Task<RuntimeContractArtifact?> IRuntimeContractCatalogService.GetEvent(string eventKey, string versionNumber, CancellationToken cancellationToken) => Task.FromResult<RuntimeContractArtifact?>(eventKey == eventArtifact.Topic ? eventArtifact : null);
        Task<CommandContractArtifacts<RuntimeContractArtifact>?> IRuntimeContractCatalogService.GetCommand(string commandKey, string versionNumber, CancellationToken cancellationToken) => Task.FromResult<CommandContractArtifacts<RuntimeContractArtifact>?>(commandKey == commandArtifact.Topic ? new(commandKey, versionNumber, commandArtifact, null) : null);

        Task<IReadOnlyList<RuntimeDesignNode>> IRuntimeDesignNodeRepository.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RuntimeDesignNode>>(Nodes);
        Task<RuntimeDesignNode?> IRuntimeDesignNodeRepository.GetById(Guid id, CancellationToken cancellationToken) => Task.FromResult(Nodes.FirstOrDefault(x => x.Id == id));
        Task<RuntimeDesignNode?> IRuntimeDesignNodeRepository.GetByKey(string key, CancellationToken cancellationToken) => Task.FromResult(Nodes.FirstOrDefault(x => x.Key == key));
        Task<RuntimeDesignNode?> IRuntimeDesignNodeRepository.GetByInboundClientId(string clientId, CancellationToken cancellationToken) => Task.FromResult(Nodes.FirstOrDefault(x => x.InboundClientId == clientId));
        Task IRuntimeDesignNodeRepository.Upsert(RuntimeDesignNode designNode, CancellationToken cancellationToken) { if (!Nodes.Contains(designNode)) Nodes.Add(designNode); return Task.CompletedTask; }

        Task<RuntimeDesignNode> IRuntimeDesignNodeConnectionService.UpsertDesignNode(Guid? id, string key, string name, DistributionMode distributionMode, string endpointBaseUri, string remoteRuntimeNodeId, bool isEnabled, CancellationToken cancellationToken)
        {
            var node = id.HasValue ? Nodes.FirstOrDefault(x => x.Id == id.Value) : null;
            node ??= new RuntimeDesignNode { Id = id ?? Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow };
            if (!Nodes.Contains(node)) Nodes.Add(node);
            node.Key = key;
            node.Name = name;
            node.DistributionMode = distributionMode;
            node.EndpointBaseUri = endpointBaseUri;
            node.RemoteRuntimeNodeId = remoteRuntimeNodeId;
            node.IsEnabled = isEnabled;
            node.Status = isEnabled ? RuntimeDesignNodeStatus.Enabled : RuntimeDesignNodeStatus.Pending;
            return Task.FromResult(node);
        }

        Task<RuntimeDesignNodeCredentialPackageModel> IRuntimeDesignNodeConnectionService.GenerateCredentialPackage(Guid designNodeId, string issuerBaseUrl, CancellationToken cancellationToken) => Task.FromResult(new RuntimeDesignNodeCredentialPackageModel { Json = $"{{\"issuer\":\"{issuerBaseUrl}\"}}", Base64 = "e30=" });
        Task IRuntimeDesignNodeConnectionService.ImportCredentialPackage(ImportRuntimeDesignNodeCredentialPackageInput input, CancellationToken cancellationToken) => Task.CompletedTask;
        Task<RuntimeDesignNodeConnectionValidationModel> IRuntimeDesignNodeConnectionService.ValidateConnection(Guid designNodeId, CancellationToken cancellationToken) => Task.FromResult(new RuntimeDesignNodeConnectionValidationModel { Succeeded = true, Message = "OK" });
        Task<IReadOnlyCollection<ControlPlaneDistributionSource>> IControlPlaneArtifactPullService.GetSources(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<ControlPlaneDistributionSource>>([new() { Key = "cp", Name = "Control Plane" }]);
        Task<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>> IControlPlaneArtifactPullService.GetPending(string sourceKey, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>>([CreatePackage()]);
        Task<RuntimeArtifactDeploymentResult> IControlPlaneArtifactPullService.Apply(string sourceKey, Guid releaseTargetId, CancellationToken cancellationToken) => Task.FromResult(new RuntimeArtifactDeploymentResult { Accepted = true, Status = "Applied", Message = "OK", RuntimeArtifactId = "runtime-artifact" });
        Task<RuntimeArtifactDeploymentResult> IControlPlaneArtifactPullService.ApplyPackage(string sourceKey, RuntimeArtifactDeliveryPackage package, CancellationToken cancellationToken) => Task.FromResult(new RuntimeArtifactDeploymentResult { Accepted = true, Status = "Applied", Message = "OK" });

        private static RuntimeContractArtifact CreateArtifact(ContractArtifactType type, string topic)
            => new()
            {
                Id = Guid.NewGuid(),
                SourceArtifactId = Guid.NewGuid(),
                SourceReleaseId = Guid.NewGuid(),
                ArtifactType = type,
                DefinitionId = Guid.NewGuid(),
                VersionId = Guid.NewGuid(),
                Name = topic,
                Topic = topic,
                VersionNumber = "1.0.0",
                PayloadSchemaJson = "{}",
                ContentHash = Guid.NewGuid().ToString("N")
            };

        private static RuntimeArtifactDeliveryPackage CreatePackage()
            => new()
            {
                ReleaseTargetId = Guid.NewGuid(),
                ReleaseId = Guid.NewGuid(),
                RuntimeNodeId = Guid.NewGuid(),
                ArtifactId = Guid.NewGuid(),
                ArtifactType = ContractArtifactType.Event,
                DefinitionId = Guid.NewGuid(),
                VersionId = Guid.NewGuid(),
                Topic = "customer.created",
                VersionNumber = "1.0.0",
                Name = "Customer Created",
                PayloadSchemaJson = "{}",
                ContentHash = "hash"
            };
    }

    private sealed class DocumentationApiFixture : IAsyncDisposable
    {
        private readonly WebApplication app;

        private DocumentationApiFixture(WebApplication app, HttpClient client, DocumentationState state)
        {
            this.app = app;
            Client = client;
            State = state;
        }

        public HttpClient Client { get; }
        public DocumentationState State { get; }

        public static async Task<DocumentationApiFixture> Start()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            var state = new DocumentationState();
            builder.Services.AddSingleton<IDocumentationInteractionService>(state);
            var app = builder.Build();
            app.MapKnOwlDocumentationApi(new KnOwlApiAuthorizationOptions());
            await app.StartAsync();
            return new DocumentationApiFixture(app, app.GetTestClient(), state);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }

    private sealed class DocumentationState : IDocumentationInteractionService
    {
        public List<DocumentationSpace> Spaces { get; } = [];
        public List<DocumentationTopic> Topics { get; } = [];
        public List<DocumentationPage> Pages { get; } = [];
        public List<DocumentationPageVersion> Versions { get; } = [];
        public List<DocumentationAsset> Assets { get; } = [];

        public Task<IReadOnlyList<DocumentationSpace>> GetSpaces(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DocumentationSpace>>(Spaces);
        public Task<DocumentationSpace?> GetSpace(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Spaces.FirstOrDefault(x => x.Id == id));
        public Task<DocumentationSpace> UpsertSpace(string key, string name, string? description, bool isActive, CancellationToken cancellationToken = default)
        {
            var space = Spaces.FirstOrDefault(x => x.Key == key) ?? new DocumentationSpace { Key = key };
            if (!Spaces.Contains(space)) Spaces.Add(space);
            space.Name = name;
            space.Description = description;
            space.IsActive = isActive;
            return Task.FromResult(space);
        }

        public Task<IReadOnlyList<DocumentationTopic>> GetTopics(string spaceKey, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DocumentationTopic>>(Topics.Where(x => Spaces.Any(space => space.Id == x.SpaceId && space.Key == spaceKey)).ToList());
        public Task<DocumentationTopic?> GetTopic(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Topics.FirstOrDefault(x => x.Id == id));
        public Task<DocumentationTopic> UpsertTopic(string spaceKey, string key, string name, string? description, bool isActive, CancellationToken cancellationToken = default)
        {
            var space = Spaces.First(x => x.Key == spaceKey);
            var topic = Topics.FirstOrDefault(x => x.SpaceId == space.Id && x.Key == key) ?? new DocumentationTopic { SpaceId = space.Id, Key = key };
            if (!Topics.Contains(topic)) Topics.Add(topic);
            topic.Name = name;
            topic.Description = description;
            topic.IsActive = isActive;
            return Task.FromResult(topic);
        }

        public Task<IReadOnlyList<DocumentationPage>> GetPages(string spaceKey, string topicKey, CancellationToken cancellationToken = default)
        {
            var topic = Topics.First(x => x.Key == topicKey && Spaces.Any(space => space.Id == x.SpaceId && space.Key == spaceKey));
            return Task.FromResult<IReadOnlyList<DocumentationPage>>(Pages.Where(x => x.TopicId == topic.Id).ToList());
        }

        public Task<DocumentationPage?> GetPage(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default) => Task.FromResult(Pages.FirstOrDefault(x => x.Id == id));
        public Task<DocumentationPage> UpsertPage(string spaceKey, string topicKey, string key, string title, string? description, bool isActive, CancellationToken cancellationToken = default)
        {
            var topic = Topics.First(x => x.Key == topicKey && Spaces.Any(space => space.Id == x.SpaceId && space.Key == spaceKey));
            var page = Pages.FirstOrDefault(x => x.TopicId == topic.Id && x.Key == key) ?? new DocumentationPage { TopicId = topic.Id, Key = key };
            if (!Pages.Contains(page)) Pages.Add(page);
            page.Title = title;
            page.Description = description;
            page.IsActive = isActive;
            return Task.FromResult(page);
        }

        public async Task<DocumentationPageVersion> ImportVersion(DocumentationVersionInput input, CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(input.Content, leaveOpen: true);
            _ = await reader.ReadToEndAsync(cancellationToken);
            var version = new DocumentationPageVersion { PageId = input.PageId, VersionNumber = input.VersionNumber, EntryPath = input.EntryPath ?? "index.md", Status = DocPageVersionStatus.Published, PublishedAtUtc = DateTime.UtcNow };
            var asset = new DocumentationAsset { PageVersionId = version.Id, LogicalPath = version.EntryPath, FileName = input.FileName, ContentType = input.ContentType, ContentHash = "hash", StorageKey = "memory" };
            version.Assets.Add(asset);
            Versions.Add(version);
            Assets.Add(asset);
            return version;
        }

        public Task PublishVersion(Guid versionId, CancellationToken cancellationToken = default) { Versions.First(x => x.Id == versionId).Status = DocPageVersionStatus.Published; return Task.CompletedTask; }
        public Task ArchiveVersion(Guid versionId, CancellationToken cancellationToken = default) { Versions.First(x => x.Id == versionId).Status = DocPageVersionStatus.Archived; return Task.CompletedTask; }
        public Task<RenderedDocumentation?> Render(string spaceKey, string topicKey, string pageKey, string? versionNumber, CancellationToken cancellationToken = default)
        {
            var page = Pages.FirstOrDefault(x => x.Key == pageKey);
            if (page is null) return Task.FromResult<RenderedDocumentation?>(null);
            var version = Versions.LastOrDefault(x => x.PageId == page.Id) ?? new DocumentationPageVersion { PageId = page.Id, VersionNumber = "1.0.0", EntryPath = "index.md" };
            var asset = Assets.FirstOrDefault(x => x.PageVersionId == version.Id) ?? new DocumentationAsset { PageVersionId = version.Id, LogicalPath = "index.md", FileName = "index.md", ContentType = "text/markdown", ContentHash = "hash", StorageKey = "memory" };
            var rendered = new RenderedDocumentation(
                version,
                asset,
                "# Title",
                "<h1>Title</h1><p>Body</p>",
                [new("title", "Title", 1), new("section", "Section", 2)]);
            return Task.FromResult<RenderedDocumentation?>(rendered);
        }

        public Task<DocumentationAsset?> GetAsset(Guid assetId, CancellationToken cancellationToken = default) => Task.FromResult(Assets.FirstOrDefault(x => x.Id == assetId));
        public Task<Stream> OpenAsset(DocumentationAsset asset, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream("asset"u8.ToArray()));
        public Task<(string FileName, Stream Content)> BuildSourcePackage(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult<(string, Stream)>(("docs.zip", new MemoryStream("zip"u8.ToArray())));
        public Task<(string FileName, Stream Content)> BuildPdf(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult<(string, Stream)>(("docs.pdf", new MemoryStream("pdf"u8.ToArray())));
    }
}
