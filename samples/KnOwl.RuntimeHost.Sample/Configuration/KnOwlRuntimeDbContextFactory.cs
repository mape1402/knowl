using KnOwl.Runtime.Storage.EntityFramework.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KnOwl.RuntimeHost.Configuration;

/// <summary>
/// Creates the runtime storage DbContext for EF Core design-time operations.
/// </summary>
public sealed class KnOwlRuntimeDbContextFactory : IDesignTimeDbContextFactory<KnOwlRuntimeDbContext>
{
    /// <inheritdoc />
    public KnOwlRuntimeDbContext CreateDbContext(string[] args)
    {
        const string designTimeConnectionString =
            "Server=(localdb)\\mssqllocaldb;Database=KnOwlRuntimeDesignTime;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<KnOwlRuntimeDbContext>()
            .UseSqlServer(
                designTimeConnectionString,
                sql => sql.MigrationsAssembly(typeof(Program).Assembly.GetName().Name))
            .Options;

        return new KnOwlRuntimeDbContext(options);
    }
}
