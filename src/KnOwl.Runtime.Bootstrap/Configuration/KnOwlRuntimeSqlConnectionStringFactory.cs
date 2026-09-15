using Microsoft.Extensions.Configuration;

namespace KnOwl.Runtime.Bootstrap.Configuration;

/// <summary>
/// Reads the KnOwl Runtime SQL Server connection string from application configuration.
/// </summary>
public static class KnOwlRuntimeSqlConnectionStringFactory
{
    /// <summary>
    /// Resolves the SQL Server connection string used by Entity Framework.
    /// </summary>
    public static string Create(IConfiguration configuration)
    {
        var configuredConnectionString = NormalizeConfigurationValue(configuration.GetConnectionString("KnOwlRuntimeDb"))
            ?? NormalizeConfigurationValue(configuration.GetConnectionString("KnOwlDb"));
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:KnOwlRuntimeDb or ConnectionStrings:KnOwlDb is required.");
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
