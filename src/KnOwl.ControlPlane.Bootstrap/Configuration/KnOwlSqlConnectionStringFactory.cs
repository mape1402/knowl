using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace KnOwl.ControlPlane.Bootstrap.Configuration;

/// <summary>
/// Builds the KnOwl SQL Server connection string from application configuration.
/// </summary>
public static class KnOwlSqlConnectionStringFactory
{
    /// <summary>
    /// Creates the SQL Server connection string used by Entity Framework.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>A SQL Server connection string for either direct credentials or Azure managed identity.</returns>
    public static string Create(IConfiguration configuration)
    {
        var configuredConnectionString = NormalizeConfigurationValue(configuration.GetConnectionString("KnOwlDb"));
        var useManagedIdentity = configuration.GetValue<bool>("Database:UseManagedIdentity");

        if (!useManagedIdentity)
        {
            if (string.IsNullOrWhiteSpace(configuredConnectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:KnOwlDb is required when Database:UseManagedIdentity is false.");
            }

            return configuredConnectionString;
        }

        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:KnOwlDb is required when Database:UseManagedIdentity is true.");
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
