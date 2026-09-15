using KnOwl.ControlPlane.Application.Distribution.Catalog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KnOwl.ControlPlane.Bootstrap.Contracts;

/// <summary>
/// Maps KnOwl Control Plane REST endpoints for deployed contract artifact lookup.
/// </summary>
public static class ControlPlaneContractCatalogEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Adds Control Plane contract catalog endpoints for artifacts already marked as deployed.
    /// </summary>
    public static IEndpointRouteBuilder MapKnOwlControlPlaneContractCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/contracts");

        group.MapGet("/artifacts", async (
            IControlPlaneContractCatalogService catalog,
            CancellationToken cancellationToken) =>
        {
            var artifacts = await catalog.GetAll(cancellationToken);
            return Results.Ok(artifacts);
        });

        group.MapGet("/events/{eventKey}/versions/{versionNumber}", async (
            string eventKey,
            string versionNumber,
            IControlPlaneContractCatalogService catalog,
            CancellationToken cancellationToken) =>
        {
            var artifact = await catalog.GetEvent(eventKey, versionNumber, cancellationToken);
            return artifact is null ? Results.NotFound() : Results.Ok(artifact);
        });

        group.MapGet("/commands/{commandKey}/versions/{versionNumber}", async (
            string commandKey,
            string versionNumber,
            IControlPlaneContractCatalogService catalog,
            CancellationToken cancellationToken) =>
        {
            var artifacts = await catalog.GetCommand(commandKey, versionNumber, cancellationToken);
            return artifacts is null ? Results.NotFound() : Results.Ok(artifacts);
        });

        return endpoints;
    }
}
