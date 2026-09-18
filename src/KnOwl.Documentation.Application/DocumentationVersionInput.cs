namespace KnOwl.Documentation.Application;

public sealed record DocumentationVersionInput(
    Guid PageId,
    string VersionNumber,
    string FileName,
    string ContentType,
    Stream Content,
    string? EntryPath = null);
