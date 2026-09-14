using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KnOwl.ControlPlaneHost.Sample.Configuration;

/// <summary>
/// Creates the control plane storage DbContext for EF Core design-time operations.
/// </summary>
public sealed class KnOwlDbContextFactory : IDesignTimeDbContextFactory<KnOwlDbContext>
{
    /// <inheritdoc />
    public KnOwlDbContext CreateDbContext(string[] args)
    {
        const string designTimeConnectionString =
            "Server=(localdb)\\mssqllocaldb;Database=KnOwlControlPlaneDesignTime;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<KnOwlDbContext>()
            .UseSqlServer(
                designTimeConnectionString,
                sql => sql.MigrationsAssembly(typeof(Program).Assembly.GetName().Name))
            .Options;

        return new KnOwlDbContext(options);
    }
}
