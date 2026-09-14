using ButterMorph.DependencyInjection;
using ButterMorph.Design;
using ButterMorph.Json.Schema;
using ButterMorph.SchemaDesign;
using ButterMorph.Web.Razor;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.ControlPlane.WebUI.ButterMorph;

/// <summary>
/// Registers ButterMorph designer services and KnOwl host adapters.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ButterMorph designer dependencies used by KnOwl schema design pages.
    /// </summary>
    public static IServiceCollection AddKnOwlButterMorphDesigner(this IServiceCollection services)
    {
        services.AddButterMorph();
        services.AddButterMorphJsonSchema();
        services.AddButterMorphSchemaDesign();
        services.AddButterMorphDesign();
        services.AddButterMorphRazorDesigner();
        services.AddSingleton<KnOwlButterMorphDraftStore>();
        services.AddScoped<IButterMorphSchemaTypeDesignerHost, KnOwlSchemaTypeDesignerHost>();
        services.AddScoped<IButterMorphFieldMetadataDesignerHost, KnOwlFieldMetadataDesignerHost>();
        services.AddScoped<IButterMorphPayloadSchemaDesignerHost, KnOwlPayloadSchemaDesignerHost>();
        return services;
    }
}
