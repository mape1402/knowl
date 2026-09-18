namespace KnOwl.Documentation;

/// <summary>
/// Classifies content stored with a documentation page version.
/// </summary>
public enum DocAssetKind
{
    SourceMarkdown = 0,
    InlineResource = 1,
    Attachment = 2,
    OriginalPackage = 3,
    GeneratedPdf = 4
}
