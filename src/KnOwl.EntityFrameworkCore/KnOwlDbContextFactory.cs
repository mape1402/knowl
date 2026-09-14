using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.EntityFrameworkCore;

internal sealed class KnOwlDbContextFactory<TDbContext> : IKnOwlDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DbContextOptions<TDbContext> _options;

    public KnOwlDbContextFactory(
        IServiceProvider serviceProvider,
        DbContextOptions<TDbContext> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public TDbContext CreateDbContext()
        => ActivatorUtilities.CreateInstance<TDbContext>(_serviceProvider, _options);
}
