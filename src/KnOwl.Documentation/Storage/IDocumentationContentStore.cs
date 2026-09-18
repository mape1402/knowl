namespace KnOwl.Documentation.Storage;

/// <summary>
/// Stores documentation content independently from documentation metadata.
/// </summary>
public interface IDocumentationContentStore
{
    Task Save(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> Open(string storageKey, CancellationToken cancellationToken = default);
    Task<bool> Exists(string storageKey, CancellationToken cancellationToken = default);
    Task Delete(string storageKey, CancellationToken cancellationToken = default);
}
