using KnOwl.Contracts.Artifacts;
using KnOwl.Runtime.Api.Contracts;
using KnOwl.Runtime.Api.Mapping;
using KnOwl.Runtime.Application.ArtifactDelivery;
using KnOwl.Runtime.Application.Catalog;
using KnOwl.Runtime.Application.Security;
using KnOwl.Runtime.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace KnOwl.Runtime.Api;

/// <summary>
/// Maps the KnOwl Runtime REST API surface.
/// </summary>
public static class KnOwlRuntimeApiEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps Runtime REST endpoints under <c>/api/v1/runtime</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapKnOwlRuntimeApi(this IEndpointRouteBuilder endpoints, string? authorizationPolicy = null)
    {
        var api = endpoints.MapGroup("/api/v1/runtime")
            .WithTags("KnOwl Runtime API");
        if (!string.IsNullOrWhiteSpace(authorizationPolicy))
        {
            api.RequireAuthorization(authorizationPolicy);
        }

        api.MapGet("/status", () => Results.Ok(new RuntimeStatusResponse("KnOwl.Runtime", "ok")))
            .WithName("GetKnOwlRuntimeApiStatus");

        MapArtifacts(api);
        MapControlPlanes(api);

        return endpoints;
    }

    private static void MapArtifacts(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/artifacts").WithTags("KnOwl Runtime Artifacts");

        group.MapGet("/", async ([FromServices] IRuntimeContractCatalogService catalog, CancellationToken cancellationToken)
            => Results.Ok((await catalog.GetAll(cancellationToken)).Select(x => x.ToResponse())));

        group.MapGet("/events/{eventKey}/versions/{versionNumber}", async (string eventKey, string versionNumber, [FromServices] IRuntimeContractCatalogService catalog, CancellationToken cancellationToken)
            => await catalog.GetEvent(eventKey, versionNumber, cancellationToken) is { } artifact
                ? Results.Ok(artifact.ToResponse())
                : Results.NotFound());

        group.MapGet("/commands/{commandKey}/versions/{versionNumber}", async (string commandKey, string versionNumber, [FromServices] IRuntimeContractCatalogService catalog, CancellationToken cancellationToken) =>
        {
            var artifacts = await catalog.GetCommand(commandKey, versionNumber, cancellationToken);
            return artifacts is null
                ? Results.NotFound()
                : Results.Ok(new RuntimeCommandArtifactsResponse(
                    commandKey,
                    versionNumber,
                    artifacts.RequestArtifact.ToResponse(),
                    artifacts.ReplyArtifact?.ToResponse()));
        });
    }

    private static void MapControlPlanes(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/control-planes").WithTags("KnOwl Runtime Control Planes");

        group.MapGet("/", async ([FromServices] IRuntimeDesignNodeRepository repository, CancellationToken cancellationToken)
            => Results.Ok((await repository.GetAll(cancellationToken)).Select(x => x.ToResponse())));

        group.MapGet("/{id:guid}", async (Guid id, [FromServices] IRuntimeDesignNodeRepository repository, CancellationToken cancellationToken)
            => await repository.GetById(id, cancellationToken) is { } node
                ? Results.Ok(node.ToResponse())
                : Results.NotFound());

        group.MapPost("/", async ([FromBody] UpsertRuntimeDesignNodeRequest request, [FromServices] IRuntimeDesignNodeConnectionService service, CancellationToken cancellationToken) =>
        {
            var node = await service.UpsertDesignNode(
                request.Id,
                request.Key.Trim(),
                request.Name.Trim(),
                request.DistributionMode,
                request.EndpointBaseUri.Trim().TrimEnd('/'),
                request.RemoteRuntimeNodeId.Trim(),
                request.IsEnabled,
                cancellationToken);

            return request.Id is null
                ? Results.Created($"/api/v1/runtime/control-planes/{node.Id}", node.ToResponse())
                : Results.Ok(node.ToResponse());
        });

        group.MapPost("/{id:guid}/credentials/generate", async (Guid id, string issuerBaseUrl, [FromServices] IRuntimeDesignNodeRepository repository, [FromServices] IRuntimeDesignNodeConnectionService service, CancellationToken cancellationToken) =>
        {
            if (await repository.GetById(id, cancellationToken) is null)
            {
                return Results.NotFound();
            }

            var package = await service.GenerateCredentialPackage(id, issuerBaseUrl, cancellationToken);
            return Results.Ok(new RuntimeCredentialPackageResponse(package.Json));
        });

        group.MapPost("/{id:guid}/credentials/import", async (Guid id, [FromBody] ImportRuntimeDesignNodeCredentialPackageRequest request, [FromServices] IRuntimeDesignNodeRepository repository, [FromServices] IRuntimeDesignNodeConnectionService service, CancellationToken cancellationToken) =>
        {
            if (await repository.GetById(id, cancellationToken) is null)
            {
                return Results.NotFound();
            }

            await service.ImportCredentialPackage(new ImportRuntimeDesignNodeCredentialPackageInput
            {
                DesignNodeId = id,
                Package = request.CredentialPackageJson
            }, cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/connect/validate", async (Guid id, [FromServices] IRuntimeDesignNodeRepository repository, [FromServices] IRuntimeDesignNodeConnectionService service, CancellationToken cancellationToken) =>
        {
            if (await repository.GetById(id, cancellationToken) is null)
            {
                return Results.NotFound();
            }

            var result = await service.ValidateConnection(id, cancellationToken);
            return Results.Ok(new RuntimeConnectionValidationResponse(result.Succeeded, result.Message));
        });

        group.MapGet("/{sourceKey}/artifacts/pending", async (string sourceKey, [FromServices] IControlPlaneArtifactPullService pull, CancellationToken cancellationToken)
            => Results.Ok(await pull.GetPending(sourceKey, cancellationToken)));

        group.MapPost("/{sourceKey}/artifacts/{releaseTargetId:guid}/apply", async (string sourceKey, Guid releaseTargetId, [FromServices] IControlPlaneArtifactPullService pull, CancellationToken cancellationToken) =>
        {
            var result = await pull.Apply(sourceKey, releaseTargetId, cancellationToken);
            return Results.Ok(new ApplyArtifactResponse(
                releaseTargetId,
                result.Accepted,
                result.Status,
                result.Message));
        });
    }
}

