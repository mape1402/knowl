using Microsoft.AspNetCore.DataProtection;

namespace KnOwl.ControlPlane.Application.Distribution.Security;

/// <summary>
/// Data Protection implementation for Control Plane outbound Runtime secrets.
/// </summary>
public sealed class DataProtectionControlPlaneRuntimeNodeSecretProtector : IControlPlaneRuntimeNodeSecretProtector
{
    private readonly IDataProtector protector;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataProtectionControlPlaneRuntimeNodeSecretProtector"/> class.
    /// </summary>
    public DataProtectionControlPlaneRuntimeNodeSecretProtector(IDataProtectionProvider provider)
    {
        protector = provider.CreateProtector("KnOwl.ControlPlane.RuntimeNodes.Secrets.v1");
    }

    /// <inheritdoc />
    public string Protect(string secret) => protector.Protect(secret ?? string.Empty);

    /// <inheritdoc />
    public string Unprotect(string protectedSecret) => protector.Unprotect(protectedSecret ?? string.Empty);
}
