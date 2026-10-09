using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using KnOwl.Documentation;
using KnOwl.Documentation.Storage;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.Diagrams;
using Markdig.Helpers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Options;

namespace KnOwl.Documentation.Application;

public sealed class DocumentationInteractionService(
    IDocumentationRepository repository,
    IDocumentationContentStore contentStore,
    IDocumentationPdfRenderer pdfRenderer,
    IOptions<DocumentationOptions> options) : IDocumentationInteractionService
{
    private static readonly Regex SemanticVersion = new(@"^\d+\.\d+\.\d+$", RegexOptions.Compiled);
    private static readonly Regex SlugInvalidCharacters = new(@"[^a-z0-9\-\._ ]+", RegexOptions.Compiled);
    private static readonly MarkdownPipeline MarkdownPipeline = CreateMarkdownPipeline();
    private readonly DocumentationOptions optionsValue = options.Value;

    public Task<IReadOnlyList<DocumentationSpace>> GetSpaces(CancellationToken cancellationToken = default)
        => repository.GetSpaces(cancellationToken);

    public Task<DocumentationSpace?> GetSpace(Guid id, CancellationToken cancellationToken = default)
        => repository.GetSpace(id, cancellationToken);

    public Task<DocumentationSpace> UpsertSpace(string key, string name, string? description, bool isActive, CancellationToken cancellationToken = default)
        => repository.UpsertSpace(new DocumentationSpace { Key = Slug(key), Name = name.Trim(), Description = description, IsActive = isActive }, cancellationToken);

    public async Task<IReadOnlyList<DocumentationTopic>> GetTopics(string spaceKey, CancellationToken cancellationToken = default)
    {
        var space = await RequiredSpace(spaceKey, cancellationToken);
        return await repository.GetTopics(space.Id, cancellationToken);
    }

    public Task<DocumentationTopic?> GetTopic(Guid id, CancellationToken cancellationToken = default)
        => repository.GetTopic(id, cancellationToken);

    public async Task<DocumentationTopic> UpsertTopic(string spaceKey, string key, string name, string? description, bool isActive, CancellationToken cancellationToken = default)
    {
        var space = await RequiredSpace(spaceKey, cancellationToken);
        return await repository.UpsertTopic(new DocumentationTopic { SpaceId = space.Id, Key = Slug(key), Name = name.Trim(), Description = description, IsActive = isActive }, cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentationPage>> GetPages(string spaceKey, string topicKey, CancellationToken cancellationToken = default)
    {
        var topic = await RequiredTopic(spaceKey, topicKey, cancellationToken);
        return await repository.GetPages(topic.Id, cancellationToken);
    }

    public Task<DocumentationPage?> GetPage(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
        => repository.GetPage(id, includeVersions, cancellationToken);

    public async Task<DocumentationPage> UpsertPage(string spaceKey, string topicKey, string key, string title, string? description, bool isActive, CancellationToken cancellationToken = default)
    {
        var topic = await RequiredTopic(spaceKey, topicKey, cancellationToken);
        return await repository.UpsertPage(new DocumentationPage { TopicId = topic.Id, Key = Slug(key), Title = title.Trim(), Description = description, IsActive = isActive }, cancellationToken);
    }

    public async Task<DocumentationPageVersion> ImportVersion(DocumentationVersionInput input, CancellationToken cancellationToken = default)
    {
        if (!SemanticVersion.IsMatch(input.VersionNumber))
        {
            throw new InvalidOperationException("Documentation version must use semantic versioning, for example 1.0.0.");
        }

        var page = await repository.GetPage(input.PageId, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Documentation page was not found.");
        using MemoryStream packageBuffer = new();
        await input.Content.CopyToAsync(packageBuffer, cancellationToken);
        if (packageBuffer.Length > optionsValue.MaxPackageBytes)
        {
            throw new InvalidOperationException("Documentation package is larger than the configured limit.");
        }

        var entries = await ReadEntries(input.FileName, packageBuffer.ToArray(), input.EntryPath, cancellationToken);
        var entryPath = entries.EntryPath;
        var source = entries.Files.Single(x => x.Path == entryPath);
        var versionId = Guid.NewGuid();
        var hash = Hash(entries.Files.SelectMany(x => x.Content).ToArray());
        var version = new DocumentationPageVersion
        {
            Id = versionId,
            PageId = page.Id,
            VersionNumber = input.VersionNumber.Trim(),
            EntryPath = entryPath,
            ContentHash = hash,
            Status = DocPageVersionStatus.Published,
            PublishedAtUtc = DateTime.UtcNow
        };
        await repository.AddVersion(version, cancellationToken);

        if (input.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            await SaveAsset(versionId, input.FileName, input.FileName, "application/zip", packageBuffer.ToArray(), DocAssetKind.OriginalPackage, cancellationToken);
        }

        foreach (var file in entries.Files)
        {
            var kind = file.Path == source.Path
                ? DocAssetKind.SourceMarkdown
                : IsInlineResource(file.ContentType) ? DocAssetKind.InlineResource : DocAssetKind.Attachment;
            await SaveAsset(versionId, file.Path, Path.GetFileName(file.Path), file.ContentType, file.Content, kind, cancellationToken);
        }

        return (await repository.GetVersion(versionId, includeAssets: true, cancellationToken))!;
    }

    public async Task PublishVersion(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await repository.GetVersion(versionId, cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Version was not found.");
        version.Status = DocPageVersionStatus.Published;
        version.PublishedAtUtc = DateTime.UtcNow;
        await repository.UpdateVersion(version, cancellationToken);
    }

    public async Task ArchiveVersion(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await repository.GetVersion(versionId, cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Version was not found.");
        version.Status = DocPageVersionStatus.Archived;
        version.ArchivedAtUtc = DateTime.UtcNow;
        await repository.UpdateVersion(version, cancellationToken);
    }

    public async Task<RenderedDocumentation?> Render(string spaceKey, string topicKey, string pageKey, string? versionNumber, CancellationToken cancellationToken = default)
    {
        var version = string.IsNullOrWhiteSpace(versionNumber) || versionNumber == "latest"
            ? await repository.GetLatestPublishedVersion(spaceKey, topicKey, pageKey, includeAssets: true, cancellationToken)
            : await repository.GetVersionByPath(spaceKey, topicKey, pageKey, versionNumber, includeAssets: true, cancellationToken);
        if (version is null)
        {
            return null;
        }

        var source = version.Assets.SingleOrDefault(x => x.Kind == DocAssetKind.SourceMarkdown)
            ?? throw new InvalidOperationException("Documentation source markdown was not found.");
        using var stream = await contentStore.Open(source.StorageKey, cancellationToken);
        using StreamReader reader = new(stream, Encoding.UTF8);
        var markdown = await reader.ReadToEndAsync(cancellationToken);
        var rendered = RenderMarkdown(markdown, version);
        return new RenderedDocumentation(version, source, markdown, rendered.Html, rendered.TableOfContents);
    }

    public async Task<RenderedDocumentation?> Render(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await repository.GetVersion(versionId, includeAssets: true, cancellationToken);
        return version is null ? null : await RenderByVersion(version, cancellationToken);
    }

    public Task<DocumentationAsset?> GetAsset(Guid assetId, CancellationToken cancellationToken = default)
        => repository.GetAsset(assetId, cancellationToken);

    public Task<Stream> OpenAsset(DocumentationAsset asset, CancellationToken cancellationToken = default)
        => contentStore.Open(asset.StorageKey, cancellationToken);

    public async Task<(string FileName, Stream Content)> BuildSourcePackage(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await repository.GetVersion(versionId, includeAssets: true, cancellationToken) ?? throw new InvalidOperationException("Version was not found.");
        var original = version.Assets.FirstOrDefault(x => x.Kind == DocAssetKind.OriginalPackage);
        if (original is not null)
        {
            return (original.FileName, await contentStore.Open(original.StorageKey, cancellationToken));
        }

        MemoryStream output = new();
        using (ZipArchive zip = new(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var asset in version.Assets.Where(x => x.Kind != DocAssetKind.GeneratedPdf))
            {
                var entry = zip.CreateEntry(asset.LogicalPath);
                await using var entryStream = entry.Open();
                await using var content = await contentStore.Open(asset.StorageKey, cancellationToken);
                await content.CopyToAsync(entryStream, cancellationToken);
            }
        }

        output.Position = 0;
        return ($"documentation-{version.VersionNumber}.zip", output);
    }

    public async Task<(string FileName, Stream Content)> BuildPdf(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await repository.GetVersion(versionId, includeAssets: true, cancellationToken) ?? throw new InvalidOperationException("Version was not found.");
        var page = await repository.GetPage(version.PageId, cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Page was not found.");
        var rendered = await RenderByVersion(version, cancellationToken);
        var pdf = await pdfRenderer.RenderPdf(page.Title, rendered.Html, cancellationToken);
        return ($"{page.Key}-{version.VersionNumber}.pdf", pdf);
    }

    private async Task<DocumentationSpace> RequiredSpace(string key, CancellationToken cancellationToken)
        => await repository.GetSpaceByKey(Slug(key), cancellationToken) ?? throw new InvalidOperationException($"Documentation space '{key}' was not found.");

    private async Task<DocumentationTopic> RequiredTopic(string spaceKey, string topicKey, CancellationToken cancellationToken)
    {
        var space = await RequiredSpace(spaceKey, cancellationToken);
        return await repository.GetTopicByKey(space.Id, Slug(topicKey), cancellationToken) ?? throw new InvalidOperationException($"Documentation topic '{topicKey}' was not found.");
    }

    private async Task<RenderedDocumentation> RenderByVersion(DocumentationPageVersion version, CancellationToken cancellationToken)
    {
        var source = version.Assets.Single(x => x.Kind == DocAssetKind.SourceMarkdown);
        using var stream = await contentStore.Open(source.StorageKey, cancellationToken);
        using StreamReader reader = new(stream, Encoding.UTF8);
        var markdown = await reader.ReadToEndAsync(cancellationToken);
        var rendered = RenderMarkdown(markdown, version);
        return new RenderedDocumentation(version, source, markdown, rendered.Html, rendered.TableOfContents);
    }

    private async Task SaveAsset(Guid versionId, string logicalPath, string fileName, string contentType, byte[] content, DocAssetKind kind, CancellationToken cancellationToken)
    {
        var normalized = DocumentationPath.Normalize(logicalPath);
        var storageKey = $"documentation/{versionId:N}/{Guid.NewGuid():N}";
        await contentStore.Save(storageKey, new MemoryStream(content), contentType, cancellationToken);
        await repository.AddAsset(new DocumentationAsset
        {
            PageVersionId = versionId,
            LogicalPath = normalized,
            FileName = fileName,
            ContentType = contentType,
            Length = content.Length,
            ContentHash = Hash(content),
            StorageKey = storageKey,
            Kind = kind,
            IsDownloadable = kind != DocAssetKind.InlineResource
        }, cancellationToken);
    }

    private static async Task<(string EntryPath, IReadOnlyList<PackageFile> Files)> ReadEntries(string fileName, byte[] content, string? entryPath, CancellationToken cancellationToken)
    {
        if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            var path = DocumentationPath.Normalize(entryPath ?? fileName);
            if (!path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                path = "index.md";
            }

            return (path, [new PackageFile(path, content, "text/markdown")]);
        }

        using MemoryStream ms = new(content);
        using ZipArchive zip = new(ms, ZipArchiveMode.Read);
        List<PackageFile> files = [];
        foreach (var entry in zip.Entries.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
        {
            var path = DocumentationPath.Normalize(entry.FullName);
            using MemoryStream entryBuffer = new();
            await using var entryStream = entry.Open();
            await entryStream.CopyToAsync(entryBuffer, cancellationToken);
            files.Add(new PackageFile(path, entryBuffer.ToArray(), ContentType(path)));
        }

        var selectedEntry = DocumentationPath.Normalize(entryPath ?? "index.md");
        if (!files.Any(x => x.Path == selectedEntry))
        {
            selectedEntry = files.FirstOrDefault(x => x.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))?.Path
                ?? throw new InvalidOperationException("Documentation package must contain at least one markdown file.");
        }

        return (selectedEntry, files);
    }

    private static (string Html, IReadOnlyList<DocumentationTableOfContentsItem> TableOfContents) RenderMarkdown(string markdown, DocumentationPageVersion version)
    {
        var document = Markdown.Parse(markdown, MarkdownPipeline);
        var tableOfContents = BuildTableOfContents(document);
        using StringWriter writer = new();
        HtmlRenderer renderer = new(writer) { LinkRewriter = target => ResolveAssetUrl(version, target) };
        MarkdownPipeline.Setup(renderer);
        renderer.Render(document);
        writer.Flush();
        return (writer.ToString(), tableOfContents);
    }

    private static MarkdownPipeline CreateMarkdownPipeline()
    {
        var builder = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .DisableHtml()
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub);

        for (var index = builder.Extensions.Count - 1; index >= 0; index--)
        {
            if (builder.Extensions[index] is DiagramExtension)
            {
                builder.Extensions.RemoveAt(index);
            }
        }

        return builder.Build();
    }

    private static IReadOnlyList<DocumentationTableOfContentsItem> BuildTableOfContents(MarkdownDocument document)
    {
        List<DocumentationTableOfContentsItem> items = [];
        Dictionary<string, int> usedIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            var title = ExtractText(heading.Inline).Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var attributes = heading.GetAttributes();
            var id = string.IsNullOrWhiteSpace(attributes.Id)
                ? UniqueSlug(title, usedIds)
                : UniqueSlug(attributes.Id, usedIds);
            attributes.Id = id;
            items.Add(new DocumentationTableOfContentsItem(id, title, heading.Level));
        }

        return items;
    }

    private static string ExtractText(ContainerInline? inline)
    {
        if (inline is null)
        {
            return string.Empty;
        }

        StringBuilder text = new();
        foreach (var child in inline)
        {
            AppendInlineText(text, child);
        }

        return text.ToString();
    }

    private static void AppendInlineText(StringBuilder text, Inline inline)
    {
        switch (inline)
        {
            case LiteralInline literal:
                text.Append(literal.Content.ToString());
                break;
            case CodeInline code:
                text.Append(code.Content);
                break;
            case ContainerInline container:
                foreach (var child in container)
                {
                    AppendInlineText(text, child);
                }

                break;
        }
    }

    private static string UniqueSlug(string value, IDictionary<string, int> usedIds)
    {
        var slug = SlugInvalidCharacters.Replace(value.Trim().ToLowerInvariant(), string.Empty);
        slug = Regex.Replace(slug, @"[\s_\.]+", "-", RegexOptions.None, TimeSpan.FromSeconds(1)).Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "section";
        }

        if (!usedIds.TryGetValue(slug, out var count))
        {
            usedIds[slug] = 1;
            return slug;
        }

        usedIds[slug] = count + 1;
        return $"{slug}-{count + 1}";
    }

    private static string ResolveAssetUrl(DocumentationPageVersion version, string target)
    {
        if (string.IsNullOrWhiteSpace(target) ||
            target.StartsWith("#", StringComparison.Ordinal) ||
            Uri.TryCreate(target, UriKind.Absolute, out _))
        {
            return target;
        }

        var targetWithoutFragment = SplitAssetTarget(target, out var suffix);
        var source = version.Assets.FirstOrDefault(x => x.Kind == DocAssetKind.SourceMarkdown)?.LogicalPath ?? version.EntryPath;
        var logical = DocumentationPath.CombineRelative(source, targetWithoutFragment);
        var asset = version.Assets.FirstOrDefault(x => string.Equals(x.LogicalPath, logical, StringComparison.OrdinalIgnoreCase));
        return asset is null ? target : $"/docs/assets/{asset.Id}{suffix}";
    }

    private static string SplitAssetTarget(string target, out string suffix)
    {
        var hashIndex = target.IndexOf('#', StringComparison.Ordinal);
        var queryIndex = target.IndexOf('?', StringComparison.Ordinal);
        var splitIndex = hashIndex >= 0 && queryIndex >= 0
            ? Math.Min(hashIndex, queryIndex)
            : Math.Max(hashIndex, queryIndex);
        if (splitIndex < 0)
        {
            suffix = string.Empty;
            return target;
        }

        suffix = target[splitIndex..];
        return target[..splitIndex];
    }

    private static bool IsInlineResource(string contentType)
        => contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) || contentType is "text/css" or "text/javascript";

    private static string ContentType(string path)
        => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".md" => "text/markdown",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".pdf" => "application/pdf",
            ".json" => "application/json",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

    private static string Hash(byte[] content)
        => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private static string Slug(string value)
        => Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9\-\.]+", "-").Trim('-');

    private sealed record PackageFile(string Path, byte[] Content, string ContentType);
}
