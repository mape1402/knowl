using KnOwl.Documentation.Application;

namespace KnOwl.Documentation.WebUI;

internal static class DocumentationRouteValidator
{
    public static async Task<DocumentationRouteHierarchy?> LoadHierarchy(
        IDocumentationInteractionService documentation,
        Guid spaceId,
        Guid topicId,
        Guid pageId,
        CancellationToken cancellationToken)
    {
        var page = await documentation.GetPage(pageId, cancellationToken: cancellationToken);
        if (page is null || page.TopicId != topicId)
        {
            return null;
        }

        var topic = await documentation.GetTopic(topicId, cancellationToken);
        if (topic is null || topic.SpaceId != spaceId)
        {
            return null;
        }

        var space = await documentation.GetSpace(spaceId, cancellationToken);
        return space is null ? null : new DocumentationRouteHierarchy(space, topic, page);
    }
}

internal sealed record DocumentationRouteHierarchy(
    DocumentationSpace Space,
    DocumentationTopic Topic,
    DocumentationPage Page);
