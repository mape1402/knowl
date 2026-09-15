using KnOwl.Contracts.Artifacts;
using System.Text.Json;
using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.Security;
using KnOwl.Runtime.Application.ArtifactDelivery;
using KnOwl.Runtime.Application.Catalog;
using KnOwl.Runtime.Application.Deployments;
using KnOwl.Runtime.Application.Security;
using KnOwl.Runtime.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KnOwl.Runtime.Bootstrap.Runtime;

/// <summary>
/// Maps KnOwl Runtime REST endpoints for distribution and metadata artifact lookup.
/// </summary>
public static class RuntimeEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Adds Runtime distribution, credential and contract catalog endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapKnOwlRuntimeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/runtime/distribution/connect/token", async (
            ConnectionTokenRequest request,
            IRuntimeConnectionTokenIssuer tokenIssuer,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var token = await tokenIssuer.Issue(request, cancellationToken);
                return Results.Ok(token);
            }
            catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
            {
                return Results.BadRequest(new { error = "invalid_client", error_description = ex.Message });
            }
        });

        endpoints.MapGet("/runtime/distribution/connect/validate", async (
            HttpContext httpContext,
            IRuntimeConnectionTokenValidator tokenValidator,
            CancellationToken cancellationToken) =>
        {
            var validation = await tokenValidator.Validate(
                ReadBearerToken(httpContext.Request),
                [ArtifactDeliveryScope.ConnectionValidate],
                cancellationToken);

            return validation.Succeeded
                ? Results.Ok(new { succeeded = true, sourceKey = validation.Principal?.NodeKey ?? string.Empty })
                : Results.Unauthorized();
        });

        endpoints.MapPost("/runtime/artifacts/deploy", async (
            HttpContext httpContext,
            IRuntimeArtifactDeliveryEndpointAuthenticator authenticator,
            IRuntimeContractDeploymentService deploymentService,
            CancellationToken cancellationToken) =>
        {
            var body = await ReadBody(httpContext.Request);
            var authentication = await authenticator.Authenticate(httpContext.Request, body, cancellationToken);
            if (!authentication.Succeeded)
            {
                return Results.Unauthorized();
            }

            var package = JsonSerializer.Deserialize<RuntimeArtifactDeliveryPackage>(body, JsonOptions());
            if (package is null)
            {
                return Results.BadRequest(new RuntimeArtifactDeploymentResult
                {
                    Accepted = false,
                    Status = "Rejected",
                    Message = "Artifact package body is empty."
                });
            }

            var result = await deploymentService.DeployArtifact(package, authentication.SourceKey, cancellationToken);
            return result.Accepted ? Results.Ok(result) : Results.BadRequest(result);
        });

        endpoints.MapGet("/runtime/distribution/control-planes", async (
            IControlPlaneArtifactPullService service,
            CancellationToken cancellationToken) =>
        {
            var sources = await service.GetSources(cancellationToken);
            return Results.Ok(sources);
        });

        endpoints.MapGet("/runtime/distribution/control-planes/{sourceKey}/artifacts/pending", async (
            string sourceKey,
            IControlPlaneArtifactPullService service,
            CancellationToken cancellationToken) =>
        {
            var packages = await service.GetPending(sourceKey, cancellationToken);
            return Results.Ok(packages);
        });

        endpoints.MapPost("/runtime/distribution/control-planes/{sourceKey}/artifacts/{releaseTargetId:guid}/apply", async (
            string sourceKey,
            Guid releaseTargetId,
            IControlPlaneArtifactPullService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.Apply(sourceKey, releaseTargetId, cancellationToken);
            return result.Accepted ? Results.Ok(result) : Results.BadRequest(result);
        });

        endpoints.MapGet("/runtime/distribution/design-nodes", async (
            IRuntimeDesignNodeRepository repository,
            CancellationToken cancellationToken) =>
        {
            var nodes = await repository.GetAll(cancellationToken);
            return Results.Ok(nodes);
        });

        endpoints.MapPost("/runtime/distribution/design-nodes", async (
            UpsertDesignNodeRequest request,
            IRuntimeDesignNodeConnectionService service,
            CancellationToken cancellationToken) =>
        {
            var node = await service.UpsertDesignNode(
                request.Id,
                request.Key,
                request.Name,
                request.DistributionMode,
                request.EndpointBaseUri,
                request.RemoteRuntimeNodeId,
                request.IsEnabled,
                cancellationToken);
            return Results.Ok(node);
        });

        endpoints.MapPost("/runtime/distribution/design-nodes/{designNodeId:guid}/credentials/generate", async (
            Guid designNodeId,
            CredentialPackageRequest request,
            IRuntimeDesignNodeConnectionService service,
            CancellationToken cancellationToken) =>
        {
            var package = await service.GenerateCredentialPackage(designNodeId, request.IssuerBaseUrl, cancellationToken);
            return Results.Ok(package);
        });

        endpoints.MapPost("/runtime/distribution/design-nodes/{designNodeId:guid}/credentials/import", async (
            Guid designNodeId,
            ImportCredentialPackageRequest request,
            IRuntimeDesignNodeConnectionService service,
            CancellationToken cancellationToken) =>
        {
            await service.ImportCredentialPackage(new ImportRuntimeDesignNodeCredentialPackageInput
            {
                DesignNodeId = designNodeId,
                Package = request.Package
            }, cancellationToken);
            return Results.Ok(new { imported = true });
        });

        endpoints.MapGet("/runtime/contracts/artifacts", async (
            IRuntimeContractCatalogService catalog,
            CancellationToken cancellationToken) =>
        {
            var artifacts = await catalog.GetAll(cancellationToken);
            return Results.Ok(artifacts);
        });

        endpoints.MapGet("/runtime/contracts/{artifactType}/{topic}/versions/{versionNumber}", async (
            string artifactType,
            string topic,
            string versionNumber,
            IRuntimeContractCatalogService catalog,
            CancellationToken cancellationToken) =>
        {
            if (!TryParseArtifactType(artifactType, out var parsedType))
            {
                return Results.BadRequest(new { message = $"Artifact type '{artifactType}' is not supported." });
            }

            var artifact = await catalog.GetExact(parsedType, topic, versionNumber, cancellationToken);
            return artifact is null ? Results.NotFound() : Results.Ok(artifact);
        });

        endpoints.MapGet("/runtime/contracts/{artifactType}/{topic}/latest", async (
            string artifactType,
            string topic,
            IRuntimeContractCatalogService catalog,
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

    private static async Task<string> ReadBody(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body);
        return await reader.ReadToEndAsync();
    }

    private static string ReadBearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : string.Empty;
    }

    private static bool TryParseArtifactType(string artifactType, out ContractArtifactType parsedType)
        => Enum.TryParse(artifactType, ignoreCase: true, out parsedType);

    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);
}

/// <summary>
/// Request used to create or update a Runtime-side Control Plane node.
/// </summary>
public sealed class UpsertDesignNodeRequest
{
    /// <summary>
    /// Gets or sets the optional node id when updating an existing node.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the runtime-local source key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how artifacts move between Control Plane and Runtime.
    /// </summary>
    public DistributionMode DistributionMode { get; set; } = DistributionMode.Hybrid;

    /// <summary>
    /// Gets or sets the Control Plane endpoint base URI.
    /// </summary>
    public string EndpointBaseUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the runtime node id assigned by the Control Plane.
    /// </summary>
    public string RemoteRuntimeNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this source is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Request used to generate a credential package.
/// </summary>
public sealed class CredentialPackageRequest
{
    /// <summary>
    /// Gets or sets the issuer base URL included in the generated credential package.
    /// </summary>
    public string IssuerBaseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Request used to import a credential package.
/// </summary>
public sealed class ImportCredentialPackageRequest
{
    /// <summary>
    /// Gets or sets the JSON or Base64 credential package.
    /// </summary>
    public string Package { get; set; } = string.Empty;
}
