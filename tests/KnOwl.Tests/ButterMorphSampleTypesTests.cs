using ButterMorph.Web.Razor;
using KnOwl.ButterMorph.SampleTypes;

namespace KnOwl.Tests;

public sealed class ButterMorphSampleTypesTests
{
    [Fact]
    public void SampleTypesAssemblyIsDiscoverableAsButterMorphBundle()
    {
        ReflectionButterMorphLanguageTypeBundleDiscovery discovery = new();

        var result = discovery.Discover(typeof(KnOwlSampleLanguageTypeBundle).Assembly);

        Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));
        Assert.Empty(result.Diagnostics);
        Assert.Equal("knowl-sample-contract-types", result.Bundle.AssemblyKey);
        Assert.Equal("csharp", result.Bundle.LanguageKey);
        Assert.Equal("1.0.0", result.Bundle.AssemblyVersion);
        Assert.Contains(result.Bundle.Types, type => type.Key == "CustomerCode" && type.DisplayName == "Customer Code");
        Assert.Contains(result.Bundle.Types, type => type.Key == "FulfillmentPriority" && type.QualifiedName.EndsWith(".FulfillmentPriority", StringComparison.Ordinal));
        Assert.Equal(result.Bundle.Types.Count, result.Bundle.Types.Select(type => type.Key).Distinct(StringComparer.Ordinal).Count());
    }
}
