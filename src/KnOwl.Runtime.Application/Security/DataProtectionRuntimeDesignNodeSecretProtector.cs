using Microsoft.AspNetCore.DataProtection;

namespace KnOwl.Runtime.Application.Security;

/// <summary>
/// Data Protection implementation for Runtime outbound Control Plane secrets.
/// </summary>
public sealed class DataProtectionRuntimeDesignNodeSecretProtector : IRuntimeDesignNodeSecretProtector
{
    private readonly IDataProtector protector;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataProtectionRuntimeDesignNodeSecretProtector"/> class.
    /// </summary>
    public DataProtectionRuntimeDesignNodeSecretProtector(IDataProtectionProvider provider)
    {
        protector = provider.CreateProtector("KnOwl.Runtime.DesignNodes.Secrets.v1");
    }

    /// <inheritdoc />
    public string Protect(string secret) => protector.Protect(secret ?? string.Empty);

    /// <inheritdoc />
    public string Unprotect(string protectedSecret) => protector.Unprotect(protectedSecret ?? string.Empty);
}
