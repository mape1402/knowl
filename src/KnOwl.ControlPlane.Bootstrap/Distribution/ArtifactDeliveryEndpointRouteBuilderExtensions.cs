using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.ControlPlane.Application.Distribution.ArtifactDelivery;
using KnOwl.ControlPlane.Application.Distribution.Security;
using KnOwl.Contracts.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KnOwl.ControlPlane.Bootstrap.Distribution;

/// <summary>
/// Maps HTTP endpoints used by runtime hosts and operators for artifact delivery.
/// </summary>
public static class ArtifactDeliveryEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Adds Distribution endpoints for runtime pull, acknowledgements, connection security and push retries.
    /// </summary>
    public static IEndpointRouteBuilder MapKnOwlArtifactDeliveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/distribution/connect/token", async (
            ConnectionTokenRequest request,
            IControlPlaneConnectionTokenIssuer tokenIssuer,
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

        endpoints.MapGet("/distribution/runtime-nodes/{runtimeNodeId:guid}/connect/validate", async (
            Guid runtimeNodeId,
            HttpContext httpContext,
            IControlPlaneConnectionTokenValidator tokenValidator,
            CancellationToken cancellationToken) =>
        {
            var validation = await tokenValidator.Validate(
                ReadBearerToken(httpContext.Request),
                runtimeNodeId,
                [ArtifactDeliveryScope.ConnectionValidate],
                cancellationToken);

            return validation.Succeeded
                ? Results.Ok(new { succeeded = true, nodeKey = validation.Principal?.NodeKey ?? string.Empty })
                : Results.Unauthorized();
        });

        endpoints.MapPost("/distribution/runtime-nodes/{runtimeNodeId:guid}/credentials/generate", async (
            Guid runtimeNodeId,
            CredentialPackageRequest request,
            IRuntimeNodeConnectionInteractionService service,
            CancellationToken cancellationToken) =>
        {
            var package = await service.GenerateCredentialPackage(runtimeNodeId, request.IssuerBaseUrl, cancellationToken);
            return Results.Ok(package);
        });

        endpoints.MapPost("/distribution/runtime-nodes/{runtimeNodeId:guid}/credentials/import", async (
            Guid runtimeNodeId,
            ImportCredentialPackageRequest request,
            IRuntimeNodeConnectionInteractionService service,
            CancellationToken cancellationToken) =>
        {
            await service.ImportCredentialPackage(new ImportRuntimeNodeCredentialPackageInput
            {
                RuntimeNodeId = runtimeNodeId,
                Package = request.Package
            }, cancellationToken);
            return Results.Ok(new { imported = true });
        });

        var group = endpoints.MapGroup("/distribution/artifacts");

        group.MapGet("/runtime-nodes/{runtimeNodeId:guid}/pending", async (
            Guid runtimeNodeId,
            HttpContext httpContext,
            IArtifactDeliveryEndpointAuthenticator authenticator,
            IArtifactDeliveryInteractionService delivery,
            CancellationToken cancellationToken) =>
        {
            var authentication = await authenticator.AuthenticateRuntimeNode(httpContext.Request, runtimeNodeId, string.Empty, cancellationToken);
            if (!authentication.Succeeded)
            {
                return Results.Unauthorized();
            }

            var packages = await delivery.GetPendingForPull(runtimeNodeId, cancellationToken);
            return Results.Ok(packages);
        });

        group.MapGet("/runtime-nodes/{runtimeNodeId:guid}/targets/{releaseTargetId:guid}", async (
            Guid runtimeNodeId,
            Guid releaseTargetId,
            HttpContext httpContext,
            IArtifactDeliveryEndpointAuthenticator authenticator,
            IArtifactDeliveryInteractionService delivery,
            CancellationToken cancellationToken) =>
        {
            var authentication = await authenticator.AuthenticateRuntimeNode(httpContext.Request, runtimeNodeId, string.Empty, cancellationToken);
            if (!authentication.Succeeded)
            {
                return Results.Unauthorized();
            }

            try
            {
                var package = await delivery.GetForPull(runtimeNodeId, releaseTargetId, cancellationToken);
                return Results.Ok(package);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapPost("/runtime-nodes/{runtimeNodeId:guid}/targets/{releaseTargetId:guid}/ack", async (
            Guid runtimeNodeId,
            Guid releaseTargetId,
            RuntimeArtifactPullAckRequest request,
            HttpContext httpContext,
            IArtifactDeliveryEndpointAuthenticator authenticator,
            IArtifactDeliveryInteractionService delivery,
            CancellationToken cancellationToken) =>
        {
            var authentication = await authenticator.AuthenticateRuntimeNode(httpContext.Request, runtimeNodeId, string.Empty, cancellationToken);
            if (!authentication.Succeeded)
            {
                return Results.Unauthorized();
            }

            var result = await delivery.AcknowledgePull(
                runtimeNodeId,
                releaseTargetId,
                request.RuntimeArtifactId,
                request.RuntimeArtifactStatus,
                cancellationToken);
            return Results.Ok(result);
        });

        group.MapPost("/targets/{releaseTargetId:guid}/push", async (
            Guid releaseTargetId,
            IArtifactDeliveryInteractionService delivery,
            CancellationToken cancellationToken) =>
        {
            var result = await delivery.Push(releaseTargetId, cancellationToken: cancellationToken);
            return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
        });

        return endpoints;
    }

    private static string ReadBearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : string.Empty;
    }
}

/// <summary>
/// Request used to generate a Control Plane credential package.
/// </summary>
public sealed class CredentialPackageRequest
{
    /// <summary>
    /// Gets or sets the issuer base URL included in the generated credential package.
    /// </summary>
    public string IssuerBaseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Request used to import a Runtime-generated credential package.
/// </summary>
public sealed class ImportCredentialPackageRequest
{
    /// <summary>
    /// Gets or sets the JSON or Base64 credential package.
    /// </summary>
    public string Package { get; set; } = string.Empty;
}
