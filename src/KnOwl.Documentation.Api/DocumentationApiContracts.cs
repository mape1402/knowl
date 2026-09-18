namespace KnOwl.Documentation.Api;

public sealed record DocumentationSpaceResponse(Guid Id, string Key, string Name, string? Description, bool IsActive);
public sealed record UpsertDocumentationSpaceRequest(string Key, string Name, string? Description, bool IsActive = true);
public sealed record DocumentationTopicResponse(Guid Id, Guid SpaceId, string Key, string Name, string? Description, bool IsActive);
public sealed record UpsertDocumentationTopicRequest(string Key, string Name, string? Description, bool IsActive = true);
public sealed record DocumentationPageResponse(Guid Id, Guid TopicId, string Key, string Title, string? Description, bool IsActive);
public sealed record UpsertDocumentationPageRequest(string Key, string Title, string? Description, bool IsActive = true);
public sealed record DocumentationPageVersionResponse(Guid Id, Guid PageId, string VersionNumber, string EntryPath, DocPageVersionStatus Status, DateTime CreatedAtUtc, DateTime? PublishedAtUtc);
