using KnOwl.Documentation;

namespace KnOwl.Documentation.Application;

public interface IDocumentationInteractionService
{
    Task<IReadOnlyList<DocumentationSpace>> GetSpaces(CancellationToken cancellationToken = default);
    Task<DocumentationSpace?> GetSpace(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentationSpace> UpsertSpace(string key, string name, string? description, bool isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentationTopic>> GetTopics(string spaceKey, CancellationToken cancellationToken = default);
    Task<DocumentationTopic?> GetTopic(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentationTopic> UpsertTopic(string spaceKey, string key, string name, string? description, bool isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentationPage>> GetPages(string spaceKey, string topicKey, CancellationToken cancellationToken = default);
    Task<DocumentationPage?> GetPage(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default);
    Task<DocumentationPage> UpsertPage(string spaceKey, string topicKey, string key, string title, string? description, bool isActive, CancellationToken cancellationToken = default);
    Task<DocumentationPageVersion> ImportVersion(DocumentationVersionInput input, CancellationToken cancellationToken = default);
    Task PublishVersion(Guid versionId, CancellationToken cancellationToken = default);
    Task ArchiveVersion(Guid versionId, CancellationToken cancellationToken = default);
    Task<RenderedDocumentation?> Render(string spaceKey, string topicKey, string pageKey, string? versionNumber, CancellationToken cancellationToken = default);
    Task<DocumentationAsset?> GetAsset(Guid assetId, CancellationToken cancellationToken = default);
    Task<Stream> OpenAsset(DocumentationAsset asset, CancellationToken cancellationToken = default);
    Task<(string FileName, Stream Content)> BuildSourcePackage(Guid versionId, CancellationToken cancellationToken = default);
    Task<(string FileName, Stream Content)> BuildPdf(Guid versionId, CancellationToken cancellationToken = default);
}
