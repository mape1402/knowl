using Microsoft.EntityFrameworkCore;

namespace KnOwl.EntityFrameworkCore;

internal sealed class KnOwlDbContextFactoryAdapter<TDbContext> : IKnOwlDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    private readonly IDbContextFactory<TDbContext> _dbContextFactory;

    public KnOwlDbContextFactoryAdapter(IDbContextFactory<TDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    public TDbContext CreateDbContext()
        => _dbContextFactory.CreateDbContext();
}
