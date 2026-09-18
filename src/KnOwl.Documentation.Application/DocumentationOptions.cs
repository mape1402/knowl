namespace KnOwl.Documentation.Application;

/// <summary>
/// Configures documentation import behavior.
/// </summary>
public sealed class DocumentationOptions
{
    public long MaxPackageBytes { get; set; } = 25 * 1024 * 1024;
    public string DefaultEntryPath { get; set; } = "index.md";
}
