namespace KnOwl.Security.Authorization;

/// <summary>
/// Defines KnOwl authorization scope types.
/// </summary>
public static class KnOwlAuthorizationScopeTypes
{
    public const string Global = "Global";
    public const string ControlPlane = "ControlPlane";
    public const string Runtime = "Runtime";
    public const string RuntimeEnvironment = "RuntimeEnvironment";
    public const string RuntimeNode = "RuntimeNode";
    public const string ContractDefinition = "ContractDefinition";
    public const string ContractVersion = "ContractVersion";
    public const string Release = "Release";
}
