namespace KnOwl.ButterMorph.SampleTypes;

using global::ButterMorph.Contracts;

/// <summary>
/// Exposes the KnOwl sample contract types as a ButterMorph language type bundle.
/// </summary>
public sealed class KnOwlSampleLanguageTypeBundle : IButterMorphLanguageTypeBundle
{
    /// <inheritdoc />
    public string Key => "knowl-sample-contract-types";

    /// <inheritdoc />
    public string Language => "csharp";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public string Description => "Sample C# value objects and enums for validating KnOwl type bundles and mappings.";
}
