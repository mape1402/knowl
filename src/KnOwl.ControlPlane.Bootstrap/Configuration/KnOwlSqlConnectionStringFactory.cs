using Microsoft.Extensions.Configuration;

namespace KnOwl.ControlPlane.Bootstrap.Configuration;

/// <summary>
/// Reads the KnOwl SQL Server connection string from application configuration.
/// </summary>
public static class KnOwlSqlConnectionStringFactory
{
    /// <summary>
    /// Resolves the SQL Server connection string used by Entity Framework.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The configured SQL Server connection string.</returns>
    public static string Create(IConfiguration configuration)
    {
        var configuredConnectionString = NormalizeConfigurationValue(configuration.GetConnectionString("KnOwlDb"));
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:KnOwlDb is required.");
        }

        return configuredConnectionString;
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
