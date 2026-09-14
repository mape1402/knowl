using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.ControlPlane.Application;

/// <summary>
/// Registers interaction services for KnOwl async contract workflows.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds application interaction services that orchestrate storage operations for async contracts.
    /// </summary>
    public static IServiceCollection AddKnOwlControlPlaneApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventInteractionService, EventInteractionService>();
        services.AddScoped<ICommandInteractionService, CommandInteractionService>();
        services.AddScoped<ISchemaTypeInteractionService, SchemaTypeInteractionService>();
        services.AddScoped<IContractFieldMetadataInteractionService, ContractFieldMetadataInteractionService>();
        services.AddSingleton<IContractVersionTransitionPolicy, ContractVersionTransitionPolicy>();
        services.AddScoped<IContractVersionPromotionService, ContractVersionPromotionService>();
        services.AddScoped<IContractSnapshotValidationService, ContractSnapshotValidationService>();
        return services;
    }
}
