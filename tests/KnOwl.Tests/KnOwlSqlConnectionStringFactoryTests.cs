using KnOwl.ControlPlane.Bootstrap.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace KnOwl.Tests;

public sealed class KnOwlSqlConnectionStringFactoryTests
{
    [Fact]
    public void CreateReturnsConfiguredConnectionStringWhenManagedIdentityIsDisabled()
    {
        const string connectionString = "Server=localhost;Database=KnOwl;User Id=sa;Password=password;TrustServerCertificate=True";
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnOwlDb"] = connectionString,
            ["Database:UseManagedIdentity"] = "false"
        });

        var result = KnOwlSqlConnectionStringFactory.Create(configuration);

        Assert.Equal(connectionString, result);
    }

    [Fact]
    public void CreateBuildsManagedIdentityConnectionStringFromConfiguredConnectionString()
    {
        const string managedIdentityClientId = "11111111-1111-1111-1111-111111111111";
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnOwlDb"] = "Server=knowl-sql.database.windows.net;Database=SchemaRegistry;User Id=legacy;Password=secret;Encrypt=True;TrustServerCertificate=False",
            ["Database:UseManagedIdentity"] = "true",
            ["Database:ManagedIdentityClientId"] = managedIdentityClientId
        });

        var result = KnOwlSqlConnectionStringFactory.Create(configuration);
        var builder = new SqlConnectionStringBuilder(result);

        Assert.Equal("knowl-sql.database.windows.net", builder.DataSource);
        Assert.Equal("SchemaRegistry", builder.InitialCatalog);
        Assert.Equal(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity, builder.Authentication);
        Assert.Equal(managedIdentityClientId, builder.UserID);
        Assert.True(builder.Encrypt);
        Assert.False(builder.TrustServerCertificate);
        Assert.Empty(builder.Password);
    }

    [Fact]
    public void CreateCanUseManagedIdentityWithConnectionStringAsBase()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnOwlDb"] = "Server=knowl-sql.database.windows.net;Database=SchemaRegistry;User Id=legacy;Password=secret;TrustServerCertificate=True",
            ["Database:UseManagedIdentity"] = "true"
        });

        var result = KnOwlSqlConnectionStringFactory.Create(configuration);
        var builder = new SqlConnectionStringBuilder(result);

        Assert.Equal("knowl-sql.database.windows.net", builder.DataSource);
        Assert.Equal("SchemaRegistry", builder.InitialCatalog);
        Assert.Equal(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity, builder.Authentication);
        Assert.Empty(builder.UserID);
        Assert.Empty(builder.Password);
    }

    [Fact]
    public void CreateIgnoresUnresolvedTokensWhenManagedIdentityIsEnabled()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnOwlDb"] = "Server=knowl-sql.database.windows.net;Database=SchemaRegistry;TrustServerCertificate=True",
            ["Database:UseManagedIdentity"] = "true",
            ["Database:ManagedIdentityClientId"] = "#{SA_CLIENT_ID}#"
        });

        var result = KnOwlSqlConnectionStringFactory.Create(configuration);
        var builder = new SqlConnectionStringBuilder(result);

        Assert.Equal("knowl-sql.database.windows.net", builder.DataSource);
        Assert.Equal("SchemaRegistry", builder.InitialCatalog);
        Assert.Equal(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity, builder.Authentication);
        Assert.Empty(builder.UserID);
    }

    [Fact]
    public void CreateThrowsWhenManagedIdentityIsEnabledWithoutConnectionString()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnOwlDb"] = "#{CONNECTIONSTRINGS_ATLASDB}#",
            ["Database:UseManagedIdentity"] = "true"
        });

        var exception = Assert.Throws<InvalidOperationException>(() => KnOwlSqlConnectionStringFactory.Create(configuration));

        Assert.Contains("ConnectionStrings:KnOwlDb", exception.Message, StringComparison.Ordinal);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
