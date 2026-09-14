using Microsoft.EntityFrameworkCore.Infrastructure;
using KnOwl.EntityFrameworkCore.Internal;

namespace Microsoft.EntityFrameworkCore;

internal static class KnOwlDbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseKnOwlModel(this DbContextOptionsBuilder optionsBuilder)
    {
        var extension = optionsBuilder.Options.FindExtension<KnOwlDbContextOptionsExtension>()
            ?? new KnOwlDbContextOptionsExtension();

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        return optionsBuilder;
    }
}
