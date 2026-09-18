using KnOwl.Documentation.Storage;
using KnOwl.Documentation.Storage.EntityFramework.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.Documentation.Storage.EntityFramework.Storage;

/// <summary>
/// EF Core implementation of documentation metadata storage.
/// </summary>
public sealed class DocumentationRepository(KnOwlDocumentationDbContext db) : IDocumentationRepository
{
    public async Task<IReadOnlyList<DocumentationSpace>> GetSpaces(CancellationToken cancellationToken = default)
        => await db.Spaces.AsNoTracking().OrderBy(x => x.Name).ToArrayAsync(cancellationToken);

    public Task<DocumentationSpace?> GetSpace(Guid id, CancellationToken cancellationToken = default)
        => db.Spaces.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<DocumentationSpace?> GetSpaceByKey(string key, CancellationToken cancellationToken = default)
        => db.Spaces.SingleOrDefaultAsync(x => x.Key == key, cancellationToken);

    public async Task<DocumentationSpace> UpsertSpace(DocumentationSpace space, CancellationToken cancellationToken = default)
    {
        var existing = await GetSpaceByKey(space.Key, cancellationToken);
        if (existing is null)
        {
            db.Spaces.Add(space);
            await db.SaveChangesAsync(cancellationToken);
            return space;
        }

        existing.Name = space.Name;
        existing.Description = space.Description;
        existing.IsActive = space.IsActive;
        existing.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<IReadOnlyList<DocumentationTopic>> GetTopics(Guid spaceId, CancellationToken cancellationToken = default)
        => await db.Topics.AsNoTracking().Where(x => x.SpaceId == spaceId).OrderBy(x => x.Name).ToArrayAsync(cancellationToken);

    public Task<DocumentationTopic?> GetTopic(Guid id, CancellationToken cancellationToken = default)
        => db.Topics.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<DocumentationTopic?> GetTopicByKey(Guid spaceId, string key, CancellationToken cancellationToken = default)
        => db.Topics.SingleOrDefaultAsync(x => x.SpaceId == spaceId && x.Key == key, cancellationToken);

    public async Task<DocumentationTopic> UpsertTopic(DocumentationTopic topic, CancellationToken cancellationToken = default)
    {
        var existing = await GetTopicByKey(topic.SpaceId, topic.Key, cancellationToken);
        if (existing is null)
        {
            db.Topics.Add(topic);
            await db.SaveChangesAsync(cancellationToken);
            return topic;
        }

        existing.Name = topic.Name;
        existing.Description = topic.Description;
        existing.IsActive = topic.IsActive;
        existing.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<IReadOnlyList<DocumentationPage>> GetPages(Guid topicId, CancellationToken cancellationToken = default)
        => await db.Pages.AsNoTracking().Where(x => x.TopicId == topicId).OrderBy(x => x.Title).ToArrayAsync(cancellationToken);

    public async Task<DocumentationPage?> GetPage(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
    {
        var query = db.Pages.AsQueryable();
        if (includeVersions)
        {
            query = query.Include(x => x.Versions).ThenInclude(x => x.Assets);
        }

        return await query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<DocumentationPage?> GetPageByPath(string spaceKey, string topicKey, string pageKey, bool includeVersions = false, CancellationToken cancellationToken = default)
    {
        var query = db.Pages
            .Include(x => x.Topic)!.ThenInclude(x => x!.Space)
            .AsQueryable();
        if (includeVersions)
        {
            query = query.Include(x => x.Versions).ThenInclude(x => x.Assets);
        }

        return await query.SingleOrDefaultAsync(
            x => x.Key == pageKey && x.Topic!.Key == topicKey && x.Topic.Space!.Key == spaceKey,
            cancellationToken);
    }

    public async Task<DocumentationPage> UpsertPage(DocumentationPage page, CancellationToken cancellationToken = default)
    {
        var existing = await db.Pages.SingleOrDefaultAsync(x => x.TopicId == page.TopicId && x.Key == page.Key, cancellationToken);
        if (existing is null)
        {
            db.Pages.Add(page);
            await db.SaveChangesAsync(cancellationToken);
            return page;
        }

        existing.Title = page.Title;
        existing.Description = page.Description;
        existing.IsActive = page.IsActive;
        existing.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<DocumentationPageVersion?> GetVersion(Guid id, bool includeAssets = false, CancellationToken cancellationToken = default)
    {
        var query = db.PageVersions.AsQueryable();
        if (includeAssets)
        {
            query = query.Include(x => x.Assets).Include(x => x.Page)!.ThenInclude(x => x!.Topic)!.ThenInclude(x => x!.Space);
        }

        return await query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<DocumentationPageVersion?> GetVersionByPath(string spaceKey, string topicKey, string pageKey, string versionNumber, bool includeAssets = false, CancellationToken cancellationToken = default)
    {
        var query = db.PageVersions
            .Include(x => x.Page)!.ThenInclude(x => x!.Topic)!.ThenInclude(x => x!.Space)
            .AsQueryable();
        if (includeAssets)
        {
            query = query.Include(x => x.Assets);
        }

        return await query.SingleOrDefaultAsync(
            x => x.VersionNumber == versionNumber && x.Page!.Key == pageKey && x.Page.Topic!.Key == topicKey && x.Page.Topic.Space!.Key == spaceKey,
            cancellationToken);
    }

    public async Task<DocumentationPageVersion?> GetLatestPublishedVersion(string spaceKey, string topicKey, string pageKey, bool includeAssets = false, CancellationToken cancellationToken = default)
    {
        var query = db.PageVersions
            .Include(x => x.Page)!.ThenInclude(x => x!.Topic)!.ThenInclude(x => x!.Space)
            .Where(x => x.Status == DocPageVersionStatus.Published &&
                        x.Page!.Key == pageKey &&
                        x.Page.Topic!.Key == topicKey &&
                        x.Page.Topic.Space!.Key == spaceKey);
        if (includeAssets)
        {
            query = query.Include(x => x.Assets);
        }

        return await query.OrderByDescending(x => x.PublishedAtUtc ?? x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DocumentationPageVersion> AddVersion(DocumentationPageVersion version, CancellationToken cancellationToken = default)
    {
        db.PageVersions.Add(version);
        await db.SaveChangesAsync(cancellationToken);
        return version;
    }

    public async Task UpdateVersion(DocumentationPageVersion version, CancellationToken cancellationToken = default)
    {
        db.PageVersions.Update(version);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<DocumentationAsset?> GetAsset(Guid id, CancellationToken cancellationToken = default)
        => db.Assets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<DocumentationAsset?> GetAssetByLogicalPath(Guid versionId, string logicalPath, CancellationToken cancellationToken = default)
        => db.Assets.SingleOrDefaultAsync(x => x.PageVersionId == versionId && x.LogicalPath == logicalPath, cancellationToken);

    public async Task<DocumentationAsset> AddAsset(DocumentationAsset asset, CancellationToken cancellationToken = default)
    {
        db.Assets.Add(asset);
        await db.SaveChangesAsync(cancellationToken);
        return asset;
    }
}
