namespace KnOwl.Documentation.Storage;

/// <summary>
/// Persists documentation metadata.
/// </summary>
public interface IDocumentationRepository
{
    Task<IReadOnlyList<DocumentationSpace>> GetSpaces(CancellationToken cancellationToken = default);
    Task<DocumentationSpace?> GetSpace(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentationSpace?> GetSpaceByKey(string key, CancellationToken cancellationToken = default);
    Task<DocumentationSpace> UpsertSpace(DocumentationSpace space, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentationTopic>> GetTopics(Guid spaceId, CancellationToken cancellationToken = default);
    Task<DocumentationTopic?> GetTopic(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentationTopic?> GetTopicByKey(Guid spaceId, string key, CancellationToken cancellationToken = default);
    Task<DocumentationTopic> UpsertTopic(DocumentationTopic topic, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentationPage>> GetPages(Guid topicId, CancellationToken cancellationToken = default);
    Task<DocumentationPage?> GetPage(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default);
    Task<DocumentationPage?> GetPageByPath(string spaceKey, string topicKey, string pageKey, bool includeVersions = false, CancellationToken cancellationToken = default);
    Task<DocumentationPage> UpsertPage(DocumentationPage page, CancellationToken cancellationToken = default);

    Task<DocumentationPageVersion?> GetVersion(Guid id, bool includeAssets = false, CancellationToken cancellationToken = default);
    Task<DocumentationPageVersion?> GetVersionByPath(string spaceKey, string topicKey, string pageKey, string versionNumber, bool includeAssets = false, CancellationToken cancellationToken = default);
    Task<DocumentationPageVersion?> GetLatestPublishedVersion(string spaceKey, string topicKey, string pageKey, bool includeAssets = false, CancellationToken cancellationToken = default);
    Task<DocumentationPageVersion> AddVersion(DocumentationPageVersion version, CancellationToken cancellationToken = default);
    Task UpdateVersion(DocumentationPageVersion version, CancellationToken cancellationToken = default);

    Task<DocumentationAsset?> GetAsset(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentationAsset?> GetAssetByLogicalPath(Guid versionId, string logicalPath, CancellationToken cancellationToken = default);
    Task<DocumentationAsset> AddAsset(DocumentationAsset asset, CancellationToken cancellationToken = default);
}
