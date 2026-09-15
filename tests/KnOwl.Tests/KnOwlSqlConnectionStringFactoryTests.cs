using KnOwl.ControlPlane.Bootstrap.Configuration;
using KnOwl.Runtime.Bootstrap.Configuration;
using Microsoft.Extensions.Configuration;

namespace KnOwl.Tests;

public sealed class KnOwlSqlConnectionStringFactoryTests
{
    [Fact]
    public void ControlPlaneCreateReturnsConfiguredConnectionString()
    {
        const string connectionString = "Server=localhost;Database=KnOwl;User Id=sa;Password=password;TrustServerCertificate=True";
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnOwlDb"] = connectionString
        });

        var result = KnOwlSqlConnectionStringFactory.Create(configuration);

        Assert.Equal(connectionString, result);
    }

    [Fact]
    public void RuntimeCreateReturnsRuntimeConnectionString()
    {
        const string connectionString = "Server=localhost;Database=KnOwlRuntime;User Id=sa;Password=password;TrustServerCertificate=True";
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnOwlRuntimeDb"] = connectionString
        });

        var result = KnOwlRuntimeSqlConnectionStringFactory.Create(configuration);

        Assert.Equal(connectionString, result);
    }

    [Fact]
    public void RuntimeCreateFallsBackToControlPlaneConnectionString()
    {
        const string connectionString = "Server=localhost;Database=KnOwlShared;User Id=sa;Password=password;TrustServerCertificate=True";
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnOwlDb"] = connectionString
        });

        var result = KnOwlRuntimeSqlConnectionStringFactory.Create(configuration);

        Assert.Equal(connectionString, result);
    }

    [Fact]
    public void ControlPlaneCreateThrowsWhenConnectionStringIsMissing()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnOwlDb"] = "#{CONNECTIONSTRINGS_KNOWLDB}#"
        });

        var exception = Assert.Throws<InvalidOperationException>(() => KnOwlSqlConnectionStringFactory.Create(configuration));

        Assert.Contains("ConnectionStrings:KnOwlDb", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeCreateThrowsWhenConnectionStringIsMissing()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnOwlRuntimeDb"] = "#{CONNECTIONSTRINGS_KNOWLRUNTIMEDB}#",
            ["ConnectionStrings:KnOwlDb"] = "#{CONNECTIONSTRINGS_KNOWLDB}#"
        });

        var exception = Assert.Throws<InvalidOperationException>(() => KnOwlRuntimeSqlConnectionStringFactory.Create(configuration));

        Assert.Contains("ConnectionStrings:KnOwlRuntimeDb", exception.Message, StringComparison.Ordinal);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
