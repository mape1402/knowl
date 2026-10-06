using System.IO.Compression;
using System.Reflection;
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
        Assert.Contains("<h1 id=\"runbook\">Runbook</h1>", rendered.Html);
        Assert.Contains("https://example.test", rendered.Html);
        Assert.Contains(rendered.TableOfContents, x => x.Id == "runbook" && x.Title == "Runbook");
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
    public async Task DocumentationAccessorsArchiveAndPathValidationCoverBranches()
    {
        var service = CreateService(out _);
        var space = await service.UpsertSpace("Docs Space", "Docs Space", "Space description", true);
        var topic = await service.UpsertTopic(space.Key, "Guides", "Guides", "Topic description", true);
        var page = await service.UpsertPage(space.Key, topic.Key, "Intro", "Intro", "Page description", true);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("# Intro"));
        var version = await service.ImportVersion(new DocumentationVersionInput(page.Id, "1.0.0", "index.md", "text/markdown", content));

        var spaces = await service.GetSpaces();
        var sameSpace = await service.GetSpace(space.Id);
        var topics = await service.GetTopics(space.Key);
        var sameTopic = await service.GetTopic(topic.Id);
        var pages = await service.GetPages(space.Key, topic.Key);
        var samePage = await service.GetPage(page.Id, includeVersions: true);
        var source = version.Assets.Single(x => x.Kind == DocAssetKind.SourceMarkdown);
        var sameAsset = await service.GetAsset(source.Id);
        await using var sourceStream = await service.OpenAsset(source);
        using var reader = new StreamReader(sourceStream, Encoding.UTF8);

        await service.ArchiveVersion(version.Id);
        var latest = await service.Render(space.Key, topic.Key, page.Key, "latest");

        Assert.Single(spaces);
        Assert.Equal(space.Id, sameSpace?.Id);
        Assert.Single(topics);
        Assert.Equal(topic.Id, sameTopic?.Id);
        Assert.Single(pages);
        Assert.Equal(page.Id, samePage?.Id);
        Assert.Equal(source.Id, sameAsset?.Id);
        Assert.Equal("# Intro", await reader.ReadToEndAsync());
        Assert.Equal(DocPageVersionStatus.Archived, version.Status);
        Assert.NotNull(version.ArchivedAtUtc);
        Assert.Null(latest);
        Assert.Equal("docs/folder/image.png", DocumentationPath.CombineRelative("docs/index.md", "./folder/image.png"));
        Assert.Equal("images/logo.png", DocumentationPath.CombineRelative("docs/index.md", "../images/logo.png"));
        Assert.Throws<InvalidOperationException>(() => DocumentationPath.Normalize("../escape.md"));
        Assert.Throws<InvalidOperationException>(() => DocumentationPath.Normalize("docs/../escape.md"));
        Assert.Throws<InvalidOperationException>(() => DocumentationPath.Normalize(".."));
        Assert.Throws<InvalidOperationException>(() => DocumentationPath.CombineRelative("index.md", "/rooted.png"));
        Assert.Throws<InvalidOperationException>(() => DocumentationPath.CombineRelative("index.md", "../escape.png"));
    }

    [Fact]
    public async Task RenderSupportsRichMarkdownCodeBlocksAndTableOfContents()
    {
        var service = CreateService(out _);
        var space = await service.UpsertSpace("platform", "Platform", null, true);
        var topic = await service.UpsertTopic(space.Key, "guides", "Guides", null, true);
        var page = await service.UpsertPage(space.Key, topic.Key, "setup", "Setup", null, true);
        await using var package = CreateRichMarkdownPackage();

        var version = await service.ImportVersion(new DocumentationVersionInput(page.Id, "2.0.0", "rich-docs.zip", "application/zip", package, "docs/index.md"));
        await service.PublishVersion(version.Id);
        var rendered = await service.Render(space.Key, topic.Key, page.Key, "2.0.0");

        Assert.NotNull(rendered);
        Assert.Contains("<pre><code class=\"language-csharp\">", rendered.Html);
        Assert.Contains("public sealed class Demo", rendered.Html);
        Assert.Contains("<pre><code class=\"language-mermaid\">", rendered.Html);
        Assert.Contains("Draft[Draft]", rendered.Html);
        Assert.Contains("<code>inline</code>", rendered.Html);
        Assert.Contains("<table>", rendered.Html);
        Assert.Contains("/docs/assets/", rendered.Html);
        Assert.DoesNotContain("assets/diagram.png", rendered.Html);
        Assert.DoesNotContain("<script", rendered.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(rendered.TableOfContents, x => x.Title == "Overview" && x.Id == "overview");
        Assert.Equal(2, rendered.TableOfContents.Count(x => x.Title == "Configuration"));
        Assert.Equal(2, rendered.TableOfContents.Select(x => x.Id).Where(x => x.StartsWith("configuration", StringComparison.Ordinal)).Distinct().Count());
        Assert.Contains(rendered.TableOfContents, x => x.Title == "!!!" && x.Id == "section");
        Assert.Contains(rendered.TableOfContents, x => x.Title == "???" && x.Id == "section-1");
        Assert.DoesNotContain(rendered.TableOfContents, x => string.IsNullOrWhiteSpace(x.Title));
    }

    [Fact]
    public void MarkdownHelpersCoverEmptyInlineAndGeneratedDuplicateSlugs()
    {
        var serviceType = typeof(DocumentationInteractionService);
        var extractText = serviceType.GetMethod("ExtractText", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("ExtractText was not found.");
        var uniqueSlug = serviceType.GetMethod("UniqueSlug", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("UniqueSlug was not found.");
        Dictionary<string, int> usedIds = new(StringComparer.OrdinalIgnoreCase);

        var empty = Assert.IsType<string>(extractText.Invoke(null, [null]));
        var first = Assert.IsType<string>(uniqueSlug.Invoke(null, ["!!!", usedIds]));
        var second = Assert.IsType<string>(uniqueSlug.Invoke(null, ["???", usedIds]));

        Assert.Equal(string.Empty, empty);
        Assert.Equal("section", first);
        Assert.Equal("section-2", second);
    }

    [Fact]
    public async Task ImportZipCoversEntryFallbackContentTypesAndAssetSuffixRewrites()
    {
        var service = CreateService(out _);
        var space = await service.UpsertSpace("assets", "Assets", null, true);
        var topic = await service.UpsertTopic(space.Key, "reference", "Reference", null, true);
        var page = await service.UpsertPage(space.Key, topic.Key, "files", "Files", null, true);
        await using var package = CreateMixedAssetPackage();

        var version = await service.ImportVersion(new DocumentationVersionInput(page.Id, "3.0.0", "assets.zip", "application/zip", package, "missing.md"));
        var rendered = await service.Render(space.Key, topic.Key, page.Key, "3.0.0");

        Assert.NotNull(rendered);
        Assert.Equal("docs/guide.md", version.EntryPath);
        Assert.Contains("/docs/assets/", rendered.Html);
        Assert.Contains("?size=large#logo", rendered.Html);
        Assert.Contains(rendered.TableOfContents, x => x.Title == "Code Heading" && x.Id == "code-heading");
        Assert.Contains(rendered.TableOfContents, x => x.Title == "Nested Bold Link" && x.Id == "nested-bold-link");
        Assert.Contains(version.Assets, x => x.ContentType == "image/jpeg" && x.Kind == DocAssetKind.InlineResource);
        Assert.Contains(version.Assets, x => x.ContentType == "image/gif" && x.Kind == DocAssetKind.InlineResource);
        Assert.Contains(version.Assets, x => x.ContentType == "image/svg+xml" && x.Kind == DocAssetKind.InlineResource);
        Assert.Contains(version.Assets, x => x.ContentType == "application/pdf" && x.Kind == DocAssetKind.Attachment);
        Assert.Contains(version.Assets, x => x.ContentType == "application/json" && x.Kind == DocAssetKind.Attachment);
        Assert.Contains(version.Assets, x => x.ContentType == "text/plain" && x.Kind == DocAssetKind.Attachment);
        Assert.Contains(version.Assets, x => x.ContentType == "application/octet-stream" && x.Kind == DocAssetKind.Attachment);
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

    [Fact]
    public async Task ImportMarkdownNormalizesEntryPathAndBuildsGeneratedSourcePackage()
    {
        var service = CreateService(out _);
        var space = await service.UpsertSpace("Docs Space", "Docs Space", "Description", true);
        var topic = await service.UpsertTopic(space.Key, "How To", "How To", "Topic", true);
        var page = await service.UpsertPage(space.Key, topic.Key, "Intro Page", "Intro Page", "Page", true);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("# Intro\n\nSee ![missing](missing.png)."));

        var version = await service.ImportVersion(new DocumentationVersionInput(page.Id, "1.0.1", "notes.txt", "text/plain", content, "folder/readme.txt"));
        var package = await service.BuildSourcePackage(version.Id);
        await using var packageStream = package.Content;
        using var zip = new ZipArchive(packageStream, ZipArchiveMode.Read);

        Assert.Equal("documentation-1.0.1.zip", package.FileName);
        Assert.Equal("index.md", version.EntryPath);
        Assert.Contains(zip.Entries, x => x.FullName == "index.md");
    }

    [Fact]
    public async Task BuildPdfRendersLatestHtmlThroughConfiguredRenderer()
    {
        var renderer = new CapturingPdfRenderer();
        var service = CreateService(out _, renderer);
        var space = await service.UpsertSpace("knowl", "KnOwl", null, true);
        var topic = await service.UpsertTopic(space.Key, "docs", "Docs", null, true);
        var page = await service.UpsertPage(space.Key, topic.Key, "intro", "Intro", null, true);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("# Intro\n\nBody"));
        var version = await service.ImportVersion(new DocumentationVersionInput(page.Id, "1.0.2", "index.md", "text/markdown", content));

        var pdf = await service.BuildPdf(version.Id);
        await using var stream = pdf.Content;
        using var reader = new StreamReader(stream, Encoding.UTF8);

        Assert.Equal("intro-1.0.2.pdf", pdf.FileName);
        Assert.Equal("Intro", renderer.Title);
        Assert.Contains("<h1 id=\"intro\">Intro</h1>", renderer.Html);
        Assert.Equal("pdf", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task DocumentationOperationsReportMissingDependencies()
    {
        var service = CreateService(out _);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("# Missing"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetTopics("missing"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetPages("missing", "topic"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ImportVersion(new DocumentationVersionInput(Guid.NewGuid(), "1.0.0", "index.md", "text/markdown", content)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PublishVersion(Guid.NewGuid()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ArchiveVersion(Guid.NewGuid()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BuildSourcePackage(Guid.NewGuid()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BuildPdf(Guid.NewGuid()));
    }

    [Fact]
    public async Task ImportRejectsOversizedAndEmptyZipPackages()
    {
        var service = CreateService(out _, options: new DocumentationOptions { MaxPackageBytes = 4 });
        var space = await service.UpsertSpace("knowl", "KnOwl", null, true);
        var topic = await service.UpsertTopic(space.Key, "docs", "Docs", null, true);
        var page = await service.UpsertPage(space.Key, topic.Key, "intro", "Intro", null, true);
        await using var tooLarge = new MemoryStream(Encoding.UTF8.GetBytes("# Intro"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ImportVersion(new DocumentationVersionInput(page.Id, "1.0.0", "index.md", "text/markdown", tooLarge)));

        var serviceWithDefaultLimit = CreateService(out _);
        var space2 = await serviceWithDefaultLimit.UpsertSpace("knowl", "KnOwl", null, true);
        var topic2 = await serviceWithDefaultLimit.UpsertTopic(space2.Key, "docs", "Docs", null, true);
        var page2 = await serviceWithDefaultLimit.UpsertPage(space2.Key, topic2.Key, "intro", "Intro", null, true);
        await using var emptyZip = CreateZipWithoutMarkdown();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            serviceWithDefaultLimit.ImportVersion(new DocumentationVersionInput(page2.Id, "1.0.0", "empty.zip", "application/zip", emptyZip)));
    }

    [Fact]
    public async Task SimplePdfRendererProducesEscapedPdfContent()
    {
        var renderer = new SimpleDocumentationPdfRenderer();
        await using var stream = await renderer.RenderPdf(
            "Intro (Guide)",
            "<h1>Intro</h1><p>Long line with (parentheses), backslash \\ and enough repeated words to force wrapping at least once in the generated PDF output.</p>");
        using var reader = new StreamReader(stream, Encoding.ASCII);
        var pdf = await reader.ReadToEndAsync();

        Assert.StartsWith("%PDF-1.4", pdf, StringComparison.Ordinal);
        Assert.Contains("Intro \\(Guide\\)", pdf);
        Assert.Contains("\\\\", pdf);
        Assert.Contains("xref", pdf);
    }

    private static DocumentationInteractionService CreateService(
        out InMemoryDocumentationRepository repository,
        IDocumentationPdfRenderer? renderer = null,
        DocumentationOptions? options = null)
    {
        repository = new InMemoryDocumentationRepository();
        return new DocumentationInteractionService(
            repository,
            new InMemoryDocumentationContentStore(),
            renderer ?? new SimpleDocumentationPdfRenderer(),
            Options.Create(options ?? new DocumentationOptions()));
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

    private static MemoryStream CreateRichMarkdownPackage()
    {
        MemoryStream output = new();
        using (ZipArchive zip = new(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var markdown = zip.CreateEntry("docs/index.md");
            using (var writer = new StreamWriter(markdown.Open(), Encoding.UTF8))
            {
                writer.Write(
                    """
                    # Overview

                    This has `inline` code and a relative image.

                    ![Diagram](assets/diagram.png)

                    ## Configuration

                    ```csharp
                    public sealed class Demo
                    {
                        public string Name { get; init; } = "KnOwl";
                    }
                    ```

                    ```mermaid
                    flowchart LR
                        Draft[Draft] --> Review[Review]
                        Review --> Published[Published]
                        Review --> Unsafe["<script>alert(1)</script>"]
                    ```

                    | Name | Value |
                    | --- | --- |
                    | Mode | Dark |

                    ## Configuration

                    Repeated headings must have stable unique anchors.

                    ## !!!

                    Punctuation-only headings still get usable anchors.

                    ## ???

                    Duplicate punctuation-only headings get unique anchors.

                    ##
                    """);
            }

            var image = zip.CreateEntry("docs/assets/diagram.png");
            using var stream = image.Open();
            stream.Write([0x89, 0x50, 0x4E, 0x47]);
        }

        output.Position = 0;
        return output;
    }

    private static MemoryStream CreateMixedAssetPackage()
    {
        MemoryStream output = new();
        using (ZipArchive zip = new(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteTextEntry(
                zip,
                "docs/guide.md",
                """
                # `Code` Heading

                ## [Nested **Bold** Link](#local)

                ![Logo](images/logo.jpg?size=large#logo)
                ![Animation](images/animation.gif)
                ![Vector](images/vector.svg)
                [Manual](files/manual.pdf)
                [Json](files/config.json)
                [Notes](files/readme.txt)
                [Binary](files/blob.bin)
                """);
            WriteBytesEntry(zip, "docs/images/logo.jpg", [0xFF, 0xD8, 0xFF]);
            WriteBytesEntry(zip, "docs/images/animation.gif", [0x47, 0x49, 0x46]);
            WriteTextEntry(zip, "docs/images/vector.svg", "<svg></svg>");
            WriteBytesEntry(zip, "docs/files/manual.pdf", [0x25, 0x50, 0x44, 0x46]);
            WriteTextEntry(zip, "docs/files/config.json", "{}");
            WriteTextEntry(zip, "docs/files/readme.txt", "hello");
            WriteBytesEntry(zip, "docs/files/blob.bin", [1, 2, 3]);
        }

        output.Position = 0;
        return output;
    }

    private static MemoryStream CreateZipWithoutMarkdown()
    {
        MemoryStream output = new();
        using (ZipArchive zip = new(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var asset = zip.CreateEntry("assets/readme.txt");
            using var writer = new StreamWriter(asset.Open(), Encoding.UTF8);
            writer.Write("No markdown here.");
        }

        output.Position = 0;
        return output;
    }

    private static void WriteTextEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private static void WriteBytesEntry(ZipArchive zip, string path, byte[] content)
    {
        var entry = zip.CreateEntry(path);
        using var stream = entry.Open();
        stream.Write(content);
    }

    private sealed class CapturingPdfRenderer : IDocumentationPdfRenderer
    {
        public string Title { get; private set; } = string.Empty;
        public string Html { get; private set; } = string.Empty;

        public Task<Stream> RenderPdf(string title, string html, CancellationToken cancellationToken = default)
        {
            Title = title;
            Html = html;
            return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("pdf")));
        }
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
