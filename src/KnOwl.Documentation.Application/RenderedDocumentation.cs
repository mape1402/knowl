using KnOwl.Documentation;

namespace KnOwl.Documentation.Application;

public sealed record RenderedDocumentation(
    DocumentationPageVersion Version,
    DocumentationAsset SourceMarkdown,
    string Markdown,
    string Html,
    IReadOnlyList<DocumentationTableOfContentsItem> TableOfContents);

public sealed record DocumentationTableOfContentsItem(
    string Id,
    string Title,
    int Level);
