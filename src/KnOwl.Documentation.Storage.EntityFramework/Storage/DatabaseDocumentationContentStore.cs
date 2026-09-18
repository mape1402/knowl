using KnOwl.Documentation.Storage;
using KnOwl.Documentation.Storage.EntityFramework.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.Documentation.Storage.EntityFramework.Storage;

/// <summary>
/// Stores documentation content in the documentation database.
/// </summary>
public sealed class DatabaseDocumentationContentStore(KnOwlDocumentationDbContext db) : IDocumentationContentStore
{
    public async Task Save(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        using MemoryStream buffer = new();
        await content.CopyToAsync(buffer, cancellationToken);
        var existing = await db.ContentBlobs.SingleOrDefaultAsync(x => x.StorageKey == storageKey, cancellationToken);
        if (existing is null)
        {
            db.ContentBlobs.Add(new DocumentationContentBlob
            {
                StorageKey = storageKey,
                ContentType = contentType,
                Content = buffer.ToArray()
            });
        }
        else
        {
            existing.ContentType = contentType;
            existing.Content = buffer.ToArray();
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Stream> Open(string storageKey, CancellationToken cancellationToken = default)
    {
        var blob = await db.ContentBlobs.AsNoTracking().SingleOrDefaultAsync(x => x.StorageKey == storageKey, cancellationToken)
            ?? throw new FileNotFoundException($"Documentation content '{storageKey}' was not found.", storageKey);
        return new MemoryStream(blob.Content, writable: false);
    }

    public Task<bool> Exists(string storageKey, CancellationToken cancellationToken = default)
        => db.ContentBlobs.AnyAsync(x => x.StorageKey == storageKey, cancellationToken);

    public async Task Delete(string storageKey, CancellationToken cancellationToken = default)
    {
        var blob = await db.ContentBlobs.SingleOrDefaultAsync(x => x.StorageKey == storageKey, cancellationToken);
        if (blob is not null)
        {
            db.ContentBlobs.Remove(blob);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
