using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Application.Distribution.Catalog;
using KnOwl.ControlPlane.Distribution.Core;
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

        group.MapGet("/artifacts/search", async (
            string? type,
            string? q,
            int? limit,
            IControlPlaneContractCatalogService catalog,
            CancellationToken cancellationToken) =>
        {
            if (!TryParseSearchArtifactType(type, out var artifactType))
            {
                return Results.BadRequest(new { message = $"Artifact type '{type}' is not supported. Use event, command, request, or reply." });
            }

            try
            {
                var results = await catalog.Search(new DeployedContractSearchQuery
                {
                    ArtifactType = artifactType,
                    Text = q,
                    Limit = limit
                }, cancellationToken);
                return Results.Ok(results);
            }
            catch (NotSupportedException exception)
            {
                return Results.Problem(
                    detail: exception.Message,
                    statusCode: StatusCodes.Status501NotImplemented);
            }
        });

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

        group.MapGet("/events/{eventKey}/latest", async (
            string eventKey,
            IControlPlaneContractCatalogService catalog,
            CancellationToken cancellationToken) =>
        {
            var artifact = await catalog.GetLatest(ContractArtifactType.Event, eventKey, cancellationToken);
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

        group.MapGet("/commands/{commandKey}/latest", async (
            string commandKey,
            IControlPlaneContractCatalogService catalog,
            CancellationToken cancellationToken) =>
        {
            var artifacts = await catalog.GetLatestCommand(commandKey, cancellationToken);
            return artifacts is null ? Results.NotFound() : Results.Ok(artifacts);
        });

        return endpoints;
    }

    private static bool TryParseSearchArtifactType(string? artifactType, out ContractArtifactType? parsedType)
    {
        parsedType = artifactType?.Trim().ToLowerInvariant() switch
        {
            null or "" => null,
            "event" => ContractArtifactType.Event,
            "command" => ContractArtifactType.Command,
            "request" or "commandrequest" => ContractArtifactType.CommandRequest,
            "reply" or "commandreply" => ContractArtifactType.CommandReply,
            _ => null
        };

        return string.IsNullOrWhiteSpace(artifactType) || parsedType.HasValue;
    }
}
