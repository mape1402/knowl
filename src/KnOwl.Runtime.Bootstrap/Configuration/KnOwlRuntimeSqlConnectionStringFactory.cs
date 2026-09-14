using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace KnOwl.Runtime.Bootstrap.Configuration;

/// <summary>
/// Builds the KnOwl Runtime SQL Server connection string from application configuration.
/// </summary>
public static class KnOwlRuntimeSqlConnectionStringFactory
{
    /// <summary>
    /// Creates the SQL Server connection string used by Entity Framework.
    /// </summary>
    public static string Create(IConfiguration configuration)
    {
        var configuredConnectionString = NormalizeConfigurationValue(configuration.GetConnectionString("KnOwlRuntimeDb"))
            ?? NormalizeConfigurationValue(configuration.GetConnectionString("KnOwlDb"));
        var useManagedIdentity = configuration.GetValue<bool>("Database:UseManagedIdentity");

        if (!useManagedIdentity)
        {
            if (string.IsNullOrWhiteSpace(configuredConnectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:KnOwlRuntimeDb or ConnectionStrings:KnOwlDb is required when Database:UseManagedIdentity is false.");
            }

            return configuredConnectionString;
        }

        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:KnOwlRuntimeDb or ConnectionStrings:KnOwlDb is required when Database:UseManagedIdentity is true.");
        }

        var builder = new SqlConnectionStringBuilder(configuredConnectionString);
        builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryManagedIdentity;
        builder.IntegratedSecurity = false;
        builder.Password = string.Empty;

        var managedIdentityClientId = NormalizeConfigurationValue(configuration["Database:ManagedIdentityClientId"]);
        if (!string.IsNullOrWhiteSpace(managedIdentityClientId))
        {
            builder.UserID = managedIdentityClientId.Trim();
        }
        else
        {
            builder.Remove("User ID");
            builder.Remove("UID");
        }

        return builder.ConnectionString;
    }

    private static string? NormalizeConfigurationValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.StartsWith("#{", StringComparison.Ordinal) && trimmed.EndsWith("}#", StringComparison.Ordinal)
            ? null
            : trimmed;
    }
}
