using Microsoft.EntityFrameworkCore;

namespace KnOwl.EntityFrameworkCore;

internal interface IKnOwlDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    TDbContext CreateDbContext();
}
