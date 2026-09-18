using KnOwl.Documentation;

namespace KnOwl.Documentation.Application;

public sealed record RenderedDocumentation(
    DocumentationPageVersion Version,
    DocumentationAsset SourceMarkdown,
    string Markdown,
    string Html);
