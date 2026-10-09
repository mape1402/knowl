using System.Text;
using KnOwl.Documentation;
using KnOwl.Documentation.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using IndexModel = KnOwl.Documentation.WebUI.Pages.Documentation.IndexModel;
using PageModel = KnOwl.Documentation.WebUI.Pages.Documentation.DocumentationPageModel;
using SpaceModel = KnOwl.Documentation.WebUI.Pages.Documentation.SpaceModel;
using TopicModel = KnOwl.Documentation.WebUI.Pages.Documentation.TopicModel;

namespace KnOwl.Tests;

public sealed class DocumentationPageModelCoverageTests
{
    [Fact]
    public async Task DocumentationIndexSearchCreateAndValidationBranches()
    {
        var docs = new DocumentationServiceFake();
        var model = Attach(new IndexModel(docs));

        await model.OnGetAsync("orchestrator", CancellationToken.None);
        Assert.Single(model.Spaces);

        model.ModelState.AddModelError("NewSpace.Key", "Required");
        var invalid = await model.OnPostCreateSpaceAsync("none", CancellationToken.None);
        Assert.IsType<PageResult>(invalid);
        Assert.True(model.ShowNewSpaceModal);

        var validModel = Attach(new IndexModel(docs));
        validModel.NewSpace = new IndexModel.SpaceInput
        {
            Key = "new-space",
            Name = "New Space",
            Description = "Created"
        };
        var created = Assert.IsType<RedirectToPageResult>(await validModel.OnPostCreateSpaceAsync(null, CancellationToken.None));
        Assert.Equal("/Documentation/Space", created.PageName);
    }

    [Fact]
    public async Task DocumentationSpaceSearchCreateAndNotFoundBranches()
    {
        var docs = new DocumentationServiceFake();
        var missing = Attach(new SpaceModel(docs));
        Assert.IsType<NotFoundResult>(await missing.OnGetAsync(Guid.NewGuid(), null, CancellationToken.None));

        var model = Attach(new SpaceModel(docs));
        Assert.IsType<PageResult>(await model.OnGetAsync(docs.Space.Id, "getting", CancellationToken.None));
        Assert.Single(model.Topics);

        model.ModelState.AddModelError("NewTopic.Key", "Required");
        var invalid = await model.OnPostCreateTopicAsync(docs.Space.Id, null, CancellationToken.None);
        Assert.IsType<PageResult>(invalid);
        Assert.True(model.ShowNewTopicModal);

        var validModel = Attach(new SpaceModel(docs));
        validModel.NewTopic = new SpaceModel.TopicInput
        {
            Key = "advanced",
            Name = "Advanced",
            Description = "Advanced topic"
        };
        var created = Assert.IsType<RedirectToPageResult>(await validModel.OnPostCreateTopicAsync(docs.Space.Id, null, CancellationToken.None));
        Assert.Equal("/Documentation/Topic", created.PageName);
    }

    [Fact]
    public async Task DocumentationTopicSearchCreateAndMissingSpaceBranches()
    {
        var docs = new DocumentationServiceFake();
        var missingTopic = Attach(new TopicModel(docs));
        Assert.IsType<NotFoundResult>(await missingTopic.OnGetAsync(docs.Space.Id, Guid.NewGuid(), null, CancellationToken.None));

        var orphan = new DocumentationTopic { Id = Guid.NewGuid(), SpaceId = Guid.NewGuid(), Key = "orphan", Name = "Orphan" };
        docs.Topics.Add(orphan);
        var missingSpace = Attach(new TopicModel(docs));
        Assert.IsType<NotFoundResult>(await missingSpace.OnGetAsync(docs.Space.Id, orphan.Id, null, CancellationToken.None));

        var model = Attach(new TopicModel(docs));
        Assert.IsType<PageResult>(await model.OnGetAsync(docs.Space.Id, docs.Topic.Id, "active", CancellationToken.None));
        Assert.Single(model.Pages);

        model.ModelState.AddModelError("NewPage.Key", "Required");
        var invalid = await model.OnPostCreatePageAsync(docs.Space.Id, docs.Topic.Id, null, CancellationToken.None);
        Assert.IsType<PageResult>(invalid);
        Assert.True(model.ShowNewPageModal);

        var validModel = Attach(new TopicModel(docs));
        validModel.NewPage = new TopicModel.DocumentationPageInput
        {
            Key = "install",
            Title = "Install",
            Description = "Install guide"
        };
        var created = Assert.IsType<RedirectToPageResult>(await validModel.OnPostCreatePageAsync(docs.Space.Id, docs.Topic.Id, null, CancellationToken.None));
        Assert.Equal("/Documentation/Page", created.PageName);
    }

    [Fact]
    public async Task DocumentationPageLoadUploadAndParentValidationBranches()
    {
        var docs = new DocumentationServiceFake();
        var missing = Attach(new PageModel(docs));
        Assert.IsType<NotFoundResult>(await missing.OnGetAsync(docs.Space.Id, docs.Topic.Id, Guid.NewGuid(), null, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await missing.OnGetAsync(Guid.NewGuid(), docs.Topic.Id, docs.Page.Id, null, CancellationToken.None));

        var model = Attach(new PageModel(docs));
        Assert.IsType<PageResult>(await model.OnGetAsync(docs.Space.Id, docs.Topic.Id, docs.Page.Id, "hash", CancellationToken.None));
        Assert.Single(model.Versions);

        model.ModelState.AddModelError("NewVersion.File", "Required");
        var invalid = await model.OnPostUploadVersionAsync(docs.Space.Id, docs.Topic.Id, docs.Page.Id, null, CancellationToken.None);
        Assert.IsType<PageResult>(invalid);
        Assert.True(model.ShowNewVersionModal);

        var validModel = Attach(new PageModel(docs));
        validModel.NewVersion = new PageModel.VersionInput
        {
            VersionNumber = "1.0.1",
            EntryPath = "docs/index.md",
            File = CreateFormFile("docs.zip", "application/zip", "package")
        };
        var uploaded = Assert.IsType<RedirectToPageResult>(await validModel.OnPostUploadVersionAsync(docs.Space.Id, docs.Topic.Id, docs.Page.Id, "1.0", CancellationToken.None));

        Assert.Equal("/Documentation/Page", uploaded.PageName);
        Assert.Equal("1.0.1", docs.ImportedVersion);
        Assert.Equal("docs/index.md", docs.ImportedEntryPath);
    }

    private static T Attach<T>(T model)
        where T : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext(),
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        };
        return model;
    }

    private static IFormFile CreateFormFile(string name, string contentType, string content)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return new FormFile(stream, 0, stream.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class DocumentationServiceFake : IDocumentationInteractionService
    {
        public DocumentationServiceFake()
        {
            Topic.SpaceId = Space.Id;
            Page.TopicId = Topic.Id;
            Version.PageId = Page.Id;
            Space.Topics.Add(Topic);
            Topic.Pages.Add(Page);
            Page.Versions.Add(Version);
            Spaces.Add(Space);
            Topics.Add(Topic);
            Pages.Add(Page);
        }

        public DocumentationSpace Space { get; } = new() { Key = "orchestrator", Name = "Orchestrator", Description = "Sample space" };
        public DocumentationTopic Topic { get; } = new() { Key = "getting-started", Name = "Getting Started", Description = "Topic" };
        public DocumentationPage Page { get; } = new() { Key = "overview", Title = "Overview", Description = "Active page", IsActive = true };
        public DocumentationPageVersion Version { get; } = new() { VersionNumber = "1.0.0", EntryPath = "docs/index.md", ContentHash = "hash" };
        public List<DocumentationSpace> Spaces { get; } = [];
        public List<DocumentationTopic> Topics { get; } = [];
        public List<DocumentationPage> Pages { get; } = [];
        public string? ImportedVersion { get; private set; }
        public string? ImportedEntryPath { get; private set; }

        public Task<IReadOnlyList<DocumentationSpace>> GetSpaces(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentationSpace>>(Spaces);

        public Task<DocumentationSpace?> GetSpace(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Spaces.FirstOrDefault(x => x.Id == id));

        public Task<DocumentationSpace> UpsertSpace(string key, string name, string? description, bool isActive, CancellationToken cancellationToken = default)
        {
            var space = new DocumentationSpace { Key = key, Name = name, Description = description, IsActive = isActive };
            Spaces.Add(space);
            return Task.FromResult(space);
        }

        public Task<IReadOnlyList<DocumentationTopic>> GetTopics(string spaceKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentationTopic>>(Topics.Where(x => Spaces.Any(space => space.Id == x.SpaceId && space.Key == spaceKey)).ToArray());

        public Task<DocumentationTopic?> GetTopic(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Topics.FirstOrDefault(x => x.Id == id));

        public Task<DocumentationTopic> UpsertTopic(string spaceKey, string key, string name, string? description, bool isActive, CancellationToken cancellationToken = default)
        {
            var space = Spaces.Single(x => x.Key == spaceKey);
            var topic = new DocumentationTopic { SpaceId = space.Id, Key = key, Name = name, Description = description, IsActive = isActive };
            Topics.Add(topic);
            return Task.FromResult(topic);
        }

        public Task<IReadOnlyList<DocumentationPage>> GetPages(string spaceKey, string topicKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentationPage>>(Pages.Where(x => Topics.Any(topic => topic.Id == x.TopicId && topic.Key == topicKey)).ToArray());

        public Task<DocumentationPage?> GetPage(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
            => Task.FromResult(Pages.FirstOrDefault(x => x.Id == id));

        public Task<DocumentationPage> UpsertPage(string spaceKey, string topicKey, string key, string title, string? description, bool isActive, CancellationToken cancellationToken = default)
        {
            var topic = Topics.Single(x => x.Key == topicKey);
            var page = new DocumentationPage { TopicId = topic.Id, Key = key, Title = title, Description = description, IsActive = isActive };
            Pages.Add(page);
            return Task.FromResult(page);
        }

        public Task<DocumentationPageVersion> ImportVersion(DocumentationVersionInput input, CancellationToken cancellationToken = default)
        {
            ImportedVersion = input.VersionNumber;
            ImportedEntryPath = input.EntryPath;
            return Task.FromResult(new DocumentationPageVersion { PageId = input.PageId, VersionNumber = input.VersionNumber, EntryPath = input.EntryPath ?? input.FileName });
        }

        public Task PublishVersion(Guid versionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ArchiveVersion(Guid versionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<RenderedDocumentation?> Render(string spaceKey, string topicKey, string pageKey, string? versionNumber, CancellationToken cancellationToken = default) => Task.FromResult<RenderedDocumentation?>(null);
        public Task<RenderedDocumentation?> Render(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult<RenderedDocumentation?>(null);
        public Task<DocumentationAsset?> GetAsset(Guid assetId, CancellationToken cancellationToken = default) => Task.FromResult<DocumentationAsset?>(null);
        public Task<Stream> OpenAsset(DocumentationAsset asset, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream());
        public Task<(string FileName, Stream Content)> BuildSourcePackage(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult<(string, Stream)>(("docs.zip", new MemoryStream()));
        public Task<(string FileName, Stream Content)> BuildPdf(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult<(string, Stream)>(("docs.pdf", new MemoryStream()));
    }
}
