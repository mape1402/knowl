namespace KnOwl.Documentation.Api;

internal static class DocumentationApiMapper
{
    public static DocumentationSpaceResponse ToResponse(this DocumentationSpace value)
        => new(value.Id, value.Key, value.Name, value.Description, value.IsActive);

    public static DocumentationTopicResponse ToResponse(this DocumentationTopic value)
        => new(value.Id, value.SpaceId, value.Key, value.Name, value.Description, value.IsActive);

    public static DocumentationPageResponse ToResponse(this DocumentationPage value)
        => new(value.Id, value.TopicId, value.Key, value.Title, value.Description, value.IsActive);

    public static DocumentationPageVersionResponse ToResponse(this DocumentationPageVersion value)
        => new(value.Id, value.PageId, value.VersionNumber, value.EntryPath, value.Status, value.CreatedAtUtc, value.PublishedAtUtc);
}
