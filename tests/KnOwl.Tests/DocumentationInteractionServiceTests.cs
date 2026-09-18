using System.IO.Compression;
using System.Text;
using KnOwl.Documentation;
using KnOwl.Documentation.Application;
using KnOwl.Documentation.Storage;
using Microsoft.Extensions.Options;

namespace KnOwl.Tests;

public sealed class DocumentationInteractionServiceTests
{
    [Fact]
    public async Task ImportMarkdownPublishesAndRendersNavigableHtml()
    {
        var service = CreateService(out _);
        var space = await service.UpsertSpace("Orchestrator", "Orchestrator", null, true);
        var topic = await service.UpsertTopic(space.Key, "Operations", "Operations", null, true);
        var page = await service.UpsertPage(space.Key, topic.Key, "Runbook", "Runbook", null, true);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("# Runbook\n\nSee [External](https://example.test)."));

        var version = await service.ImportVersion(new DocumentationVersionInput(page.Id, "1.0.0", "index.md", "text/markdown", content));
        await service.PublishVersion(version.Id);
        var rendered = await service.Render(space.Key, topic.Key, page.Key, "latest");

        Assert.NotNull(rendered);
        Assert.Equal("1.0.0", rendered.Version.VersionNumber);
        Assert.Contains("<h1>Runbook</h1>", rendered.Html);
        Assert.Contains("https://example.test", rendered.Html);
    }

    [Fact]
    public async Task ImportZipPreservesOriginalPackageAndResolvesInlineAssets()
    {
        var service = CreateService(out _);
        var space = await service.UpsertSpace("orchestrator", "Orchestrator", null, true);
        var topic = await service.UpsertTopic(space.Key, "guides", "Guides", null, true);
        var page = await service.UpsertPage(space.Key, topic.Key, "setup", "Setup", null, true);
        await using var package = CreatePackage();

        var version = await service.ImportVersion(new DocumentationVersionInput(page.Id, "1.2.3", "docs.zip", "application/zip", package, "docs/index.md"));
        await service.PublishVersion(version.Id);
        var rendered = await service.Render(space.Key, topic.Key, page.Key, "1.2.3");
        var sourcePackage = await service.BuildSourcePackage(version.Id);

        Assert.NotNull(rendered);
        Assert.Contains("/docs/assets/", rendered.Html);
        Assert.Equal("docs.zip", sourcePackage.FileName);
    }

    [Fact]
    public async Task ImportRejectsNonSemanticVersion()
    {
        var service = CreateService(out _);
        var space = await service.UpsertSpace("knowl", "KnOwl", null, true);
        var topic = await service.UpsertTopic(space.Key, "docs", "Docs", null, true);
        var page = await service.UpsertPage(space.Key, topic.Key, "intro", "Intro", null, true);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("# Intro"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ImportVersion(new DocumentationVersionInput(page.Id, "v1", "index.md", "text/markdown", content)));
    }

    private static DocumentationInteractionService CreateService(out InMemoryDocumentationRepository repository)
    {
        repository = new InMemoryDocumentationRepository();
        return new DocumentationInteractionService(
            repository,
            new InMemoryDocumentationContentStore(),
            new SimpleDocumentationPdfRenderer(),
            Options.Create(new DocumentationOptions()));
    }

    private static MemoryStream CreatePackage()
    {
        MemoryStream output = new();
        using (ZipArchive zip = new(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var markdown = zip.CreateEntry("docs/index.md");
            using (var writer = new StreamWriter(markdown.Open(), Encoding.UTF8))
            {
                writer.Write("# Setup\n\n![Logo](images/logo.png)");
            }

            var image = zip.CreateEntry("docs/images/logo.png");
            using var stream = image.Open();
            stream.Write([0x89, 0x50, 0x4E, 0x47]);
        }

        output.Position = 0;
        return output;
    }

    private sealed class InMemoryDocumentationContentStore : IDocumentationContentStore
    {
        private readonly Dictionary<string, (string ContentType, byte[] Content)> items = [];

        public async Task Save(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            using MemoryStream ms = new();
            await content.CopyToAsync(ms, cancellationToken);
            items[storageKey] = (contentType, ms.ToArray());
        }

        public Task<Stream> Open(string storageKey, CancellationToken cancellationToken = default)
            => Task.FromResult<Stream>(new MemoryStream(items[storageKey].Content));

        public Task<bool> Exists(string storageKey, CancellationToken cancellationToken = default)
            => Task.FromResult(items.ContainsKey(storageKey));

        public Task Delete(string storageKey, CancellationToken cancellationToken = default)
        {
            items.Remove(storageKey);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryDocumentationRepository : IDocumentationRepository
    {
        private readonly List<DocumentationSpace> spaces = [];
        private readonly List<DocumentationTopic> topics = [];
        private readonly List<DocumentationPage> pages = [];
        private readonly List<DocumentationPageVersion> versions = [];
        private readonly List<DocumentationAsset> assets = [];

        public Task<IReadOnlyList<DocumentationSpace>> GetSpaces(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentationSpace>>(spaces.OrderBy(x => x.Name).ToArray());

        public Task<DocumentationSpace?> GetSpace(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(spaces.SingleOrDefault(x => x.Id == id));

        public Task<DocumentationSpace?> GetSpaceByKey(string key, CancellationToken cancellationToken = default)
            => Task.FromResult(spaces.SingleOrDefault(x => x.Key == key));

        public Task<DocumentationSpace> UpsertSpace(DocumentationSpace space, CancellationToken cancellationToken = default)
        {
            var existing = spaces.SingleOrDefault(x => x.Key == space.Key);
            if (existing is null)
            {
                spaces.Add(space);
                return Task.FromResult(space);
            }

            existing.Name = space.Name;
            existing.Description = space.Description;
            existing.IsActive = space.IsActive;
            return Task.FromResult(existing);
        }

        public Task<IReadOnlyList<DocumentationTopic>> GetTopics(Guid spaceId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentationTopic>>(topics.Where(x => x.SpaceId == spaceId).OrderBy(x => x.Name).ToArray());

        public Task<DocumentationTopic?> GetTopic(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(topics.SingleOrDefault(x => x.Id == id));

        public Task<DocumentationTopic?> GetTopicByKey(Guid spaceId, string key, CancellationToken cancellationToken = default)
            => Task.FromResult(topics.SingleOrDefault(x => x.SpaceId == spaceId && x.Key == key));

        public Task<DocumentationTopic> UpsertTopic(DocumentationTopic topic, CancellationToken cancellationToken = default)
        {
            var existing = topics.SingleOrDefault(x => x.SpaceId == topic.SpaceId && x.Key == topic.Key);
            if (existing is null)
            {
                topic.Space = spaces.Single(x => x.Id == topic.SpaceId);
                topics.Add(topic);
                topic.Space.Topics.Add(topic);
                return Task.FromResult(topic);
            }

            existing.Name = topic.Name;
            existing.Description = topic.Description;
            existing.IsActive = topic.IsActive;
            return Task.FromResult(existing);
        }

        public Task<IReadOnlyList<DocumentationPage>> GetPages(Guid topicId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentationPage>>(pages.Where(x => x.TopicId == topicId).OrderBy(x => x.Title).ToArray());

        public Task<DocumentationPage?> GetPage(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
        {
            var page = pages.SingleOrDefault(x => x.Id == id);
            return Task.FromResult(page);
        }

        public Task<DocumentationPage?> GetPageByPath(string spaceKey, string topicKey, string pageKey, bool includeVersions = false, CancellationToken cancellationToken = default)
            => Task.FromResult(pages.SingleOrDefault(x => x.Key == pageKey && x.Topic?.Key == topicKey && x.Topic.Space?.Key == spaceKey));

        public Task<DocumentationPage> UpsertPage(DocumentationPage page, CancellationToken cancellationToken = default)
        {
            var existing = pages.SingleOrDefault(x => x.TopicId == page.TopicId && x.Key == page.Key);
            if (existing is null)
            {
                page.Topic = topics.Single(x => x.Id == page.TopicId);
                pages.Add(page);
                page.Topic.Pages.Add(page);
                return Task.FromResult(page);
            }

            existing.Title = page.Title;
            existing.Description = page.Description;
            existing.IsActive = page.IsActive;
            return Task.FromResult(existing);
        }

        public Task<DocumentationPageVersion?> GetVersion(Guid id, bool includeAssets = false, CancellationToken cancellationToken = default)
            => Task.FromResult(versions.SingleOrDefault(x => x.Id == id));

        public Task<DocumentationPageVersion?> GetVersionByPath(string spaceKey, string topicKey, string pageKey, string versionNumber, bool includeAssets = false, CancellationToken cancellationToken = default)
            => Task.FromResult(versions.SingleOrDefault(x => x.VersionNumber == versionNumber && x.Page?.Key == pageKey && x.Page.Topic?.Key == topicKey && x.Page.Topic.Space?.Key == spaceKey));

        public Task<DocumentationPageVersion?> GetLatestPublishedVersion(string spaceKey, string topicKey, string pageKey, bool includeAssets = false, CancellationToken cancellationToken = default)
            => Task.FromResult(versions
                .Where(x => x.Status == DocPageVersionStatus.Published && x.Page?.Key == pageKey && x.Page.Topic?.Key == topicKey && x.Page.Topic.Space?.Key == spaceKey)
                .OrderByDescending(x => x.PublishedAtUtc ?? x.CreatedAtUtc)
                .FirstOrDefault());

        public Task<DocumentationPageVersion> AddVersion(DocumentationPageVersion version, CancellationToken cancellationToken = default)
        {
            version.Page = pages.Single(x => x.Id == version.PageId);
            versions.Add(version);
            version.Page.Versions.Add(version);
            return Task.FromResult(version);
        }

        public Task UpdateVersion(DocumentationPageVersion version, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<DocumentationAsset?> GetAsset(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(assets.SingleOrDefault(x => x.Id == id));

        public Task<DocumentationAsset?> GetAssetByLogicalPath(Guid versionId, string logicalPath, CancellationToken cancellationToken = default)
            => Task.FromResult(assets.SingleOrDefault(x => x.PageVersionId == versionId && x.LogicalPath == logicalPath));

        public Task<DocumentationAsset> AddAsset(DocumentationAsset asset, CancellationToken cancellationToken = default)
        {
            asset.PageVersion = versions.Single(x => x.Id == asset.PageVersionId);
            assets.Add(asset);
            asset.PageVersion.Assets.Add(asset);
            return Task.FromResult(asset);
        }
    }
}
