using KnOwl.Contracts.Artifacts;
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

        group.MapGet("/{artifactType}/{topic}/versions/{versionNumber}", async (
            string artifactType,
            string topic,
            string versionNumber,
            IControlPlaneContractCatalogService catalog,
            CancellationToken cancellationToken) =>
        {
            if (!TryParseArtifactType(artifactType, out var parsedType))
            {
                return Results.BadRequest(new { message = $"Artifact type '{artifactType}' is not supported." });
            }

            var artifact = await catalog.GetExact(parsedType, topic, versionNumber, cancellationToken);
            return artifact is null ? Results.NotFound() : Results.Ok(artifact);
        });

        group.MapGet("/{artifactType}/{topic}/latest", async (
            string artifactType,
            string topic,
            IControlPlaneContractCatalogService catalog,
            CancellationToken cancellationToken) =>
        {
            if (!TryParseArtifactType(artifactType, out var parsedType))
            {
                return Results.BadRequest(new { message = $"Artifact type '{artifactType}' is not supported." });
            }

            var artifact = await catalog.GetLatest(parsedType, topic, cancellationToken);
            return artifact is null ? Results.NotFound() : Results.Ok(artifact);
        });

        return endpoints;
    }

    private static bool TryParseArtifactType(string artifactType, out ContractArtifactType parsedType)
        => Enum.TryParse(artifactType, ignoreCase: true, out parsedType);
}
