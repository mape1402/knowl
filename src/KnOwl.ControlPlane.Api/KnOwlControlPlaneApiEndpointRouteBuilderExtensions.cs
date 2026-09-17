using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Api.Contracts;
using KnOwl.ControlPlane.Api.Mapping;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Application.Distribution.Artifacts;
using KnOwl.ControlPlane.Application.Distribution.Catalog;
using KnOwl.ControlPlane.Application.Distribution.ReleaseBundles;
using KnOwl.ControlPlane.Application.Distribution.Security;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace KnOwl.ControlPlane.Api;

/// <summary>
/// Maps the KnOwl Control Plane REST API surface.
/// </summary>
public static class KnOwlControlPlaneApiEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps Control Plane REST endpoints under <c>/api/v1/control-plane</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapKnOwlControlPlaneApi(this IEndpointRouteBuilder endpoints, string? authorizationPolicy = null)
    {
        var api = endpoints.MapGroup("/api/v1/control-plane")
            .WithTags("KnOwl Control Plane API");
        if (!string.IsNullOrWhiteSpace(authorizationPolicy))
        {
            api.RequireAuthorization(authorizationPolicy);
        }

        MapSchemaTypes(api);
        MapMetadataFields(api);
        MapEvents(api);
        MapCommands(api);
        MapArtifacts(api);
        MapRuntimeEnvironments(api);
        MapRuntimeNodes(api);
        MapReleases(api);

        return endpoints;
    }

    private static void MapSchemaTypes(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/schema-types").WithTags("KnOwl Control Plane Schema Types");

        group.MapGet("/", async ([FromServices] ISchemaTypeInteractionService service, CancellationToken cancellationToken)
            => Results.Ok((await service.GetAll(cancellationToken)).Select(x => x.ToResponse())));

        group.MapGet("/{id:guid}", async (Guid id, [FromServices] ISchemaTypeInteractionService service, CancellationToken cancellationToken)
            => await service.GetById(id, includeVersions: true, cancellationToken) is { } item
                ? Results.Ok(item.ToResponse())
                : Results.NotFound());

        group.MapPost("/", async ([FromBody] CreateSchemaTypeRequest request, [FromServices] ISchemaTypeInteractionService service, CancellationToken cancellationToken) =>
        {
            if (await service.KeyExists(request.Key, cancellationToken: cancellationToken))
            {
                return Conflict($"Schema type key '{request.Key}' already exists.");
            }

            var item = new SchemaTypeDefinition
            {
                Key = request.Key.Trim(),
                Name = request.Name.Trim(),
                Description = request.Description,
                IsActive = request.IsActive,
                Versions =
                {
                    new SchemaTypeVersion
                    {
                        VersionNumber = request.VersionNumber.Trim(),
                        DefinitionJson = request.DefinitionJson,
                        Comment = request.Comment,
                        IsActive = request.IsActive
                    }
                }
            };

            await service.Create(item, cancellationToken);
            return Results.Created($"/api/v1/control-plane/schema-types/{item.Id}", item.ToResponse());
        });

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateSchemaTypeRequest request, [FromServices] ISchemaTypeInteractionService service, CancellationToken cancellationToken) =>
        {
            if (await service.GetById(id, cancellationToken: cancellationToken) is null)
            {
                return Results.NotFound();
            }

            if (await service.KeyExists(request.Key, id, cancellationToken))
            {
                return Conflict($"Schema type key '{request.Key}' already exists.");
            }

            await service.UpdateDefinition(id, request.Key.Trim(), request.Name.Trim(), request.Description, request.IsActive, DateTime.UtcNow, cancellationToken);
            var updated = await service.GetById(id, includeVersions: true, cancellationToken);
            return Results.Ok(updated!.ToResponse());
        });

        group.MapPost("/{id:guid}/versions", async (Guid id, [FromBody] CreateSchemaTypeVersionRequest request, [FromServices] ISchemaTypeInteractionService service, CancellationToken cancellationToken) =>
        {
            if (await service.GetById(id, cancellationToken: cancellationToken) is null)
            {
                return Results.NotFound();
            }

            if (await service.VersionExists(id, request.VersionNumber, cancellationToken))
            {
                return Conflict($"Schema type version '{request.VersionNumber}' already exists.");
            }

            var version = new SchemaTypeVersion
            {
                SchemaTypeDefinitionId = id,
                VersionNumber = request.VersionNumber.Trim(),
                DefinitionJson = request.DefinitionJson,
                Comment = request.Comment,
                IsActive = request.IsActive
            };
            await service.AddVersion(id, version, cancellationToken);
            return Results.Created($"/api/v1/control-plane/schema-types/{id}/versions/{version.Id}", version.ToResponse());
        });

        group.MapPatch("/{id:guid}/versions/{versionId:guid}/active", async (Guid id, Guid versionId, [FromBody] SetVersionActiveRequest request, [FromServices] ISchemaTypeInteractionService service, CancellationToken cancellationToken) =>
        {
            if (await service.GetById(id, cancellationToken: cancellationToken) is null)
            {
                return Results.NotFound();
            }

            await service.SetVersionActive(id, versionId, request.IsActive, DateTime.UtcNow, cancellationToken);
            return Results.NoContent();
        });
    }

    private static void MapMetadataFields(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/metadata-fields").WithTags("KnOwl Control Plane Metadata Fields");

        group.MapGet("/", async ([FromServices] IContractFieldMetadataInteractionService service, CancellationToken cancellationToken)
            => Results.Ok((await service.GetAll(cancellationToken)).Select(x => x.ToResponse())));

        group.MapGet("/{id:guid}", async (Guid id, [FromServices] IContractFieldMetadataInteractionService service, CancellationToken cancellationToken)
            => await service.GetById(id, includeVersions: true, cancellationToken) is { } item
                ? Results.Ok(item.ToResponse())
                : Results.NotFound());

        group.MapPost("/", async ([FromBody] CreateMetadataFieldRequest request, [FromServices] IContractFieldMetadataInteractionService service, CancellationToken cancellationToken) =>
        {
            if (await service.KeyExists(request.Key, cancellationToken: cancellationToken))
            {
                return Conflict($"Metadata field key '{request.Key}' already exists.");
            }

            var item = new ContractFieldMetadataDefinition
            {
                Key = request.Key.Trim(),
                Name = request.Name.Trim(),
                Description = request.Description,
                IsActive = request.IsActive,
                Versions =
                {
                    new ContractFieldMetadataVersion
                    {
                        VersionNumber = request.VersionNumber.Trim(),
                        DefinitionJson = request.DefinitionJson,
                        Comment = request.Comment,
                        IsActive = request.IsActive
                    }
                }
            };

            await service.Create(item, cancellationToken);
            return Results.Created($"/api/v1/control-plane/metadata-fields/{item.Id}", item.ToResponse());
        });

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateMetadataFieldRequest request, [FromServices] IContractFieldMetadataInteractionService service, CancellationToken cancellationToken) =>
        {
            if (await service.GetById(id, cancellationToken: cancellationToken) is null)
            {
                return Results.NotFound();
            }

            if (await service.KeyExists(request.Key, id, cancellationToken))
            {
                return Conflict($"Metadata field key '{request.Key}' already exists.");
            }

            await service.UpdateDefinition(id, request.Key.Trim(), request.Name.Trim(), request.Description, request.IsActive, DateTime.UtcNow, cancellationToken);
            var updated = await service.GetById(id, includeVersions: true, cancellationToken);
            return Results.Ok(updated!.ToResponse());
        });

        group.MapPut("/{id:guid}/versions", async (Guid id, [FromBody] UpsertMetadataFieldVersionRequest request, [FromServices] IContractFieldMetadataInteractionService service, CancellationToken cancellationToken) =>
        {
            if (await service.GetById(id, cancellationToken: cancellationToken) is null)
            {
                return Results.NotFound();
            }

            var version = new ContractFieldMetadataVersion
            {
                ContractFieldMetadataDefinitionId = id,
                VersionNumber = request.VersionNumber.Trim(),
                DefinitionJson = request.DefinitionJson,
                Comment = request.Comment,
                IsActive = request.IsActive
            };
            await service.UpsertVersion(id, version, cancellationToken);
            return Results.Ok(version.ToResponse());
        });

        group.MapPatch("/{id:guid}/versions/{versionId:guid}/active", async (Guid id, Guid versionId, [FromBody] SetVersionActiveRequest request, [FromServices] IContractFieldMetadataInteractionService service, CancellationToken cancellationToken) =>
        {
            if (await service.GetById(id, cancellationToken: cancellationToken) is null)
            {
                return Results.NotFound();
            }

            await service.SetVersionActive(id, versionId, request.IsActive, DateTime.UtcNow, cancellationToken);
            return Results.NoContent();
        });
    }

    private static void MapEvents(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/events").WithTags("KnOwl Control Plane Events");

        group.MapGet("/", async ([FromServices] IEventInteractionService service, CancellationToken cancellationToken)
            => Results.Ok((await service.GetAll(cancellationToken)).Select(x => x.ToResponse())));

        group.MapGet("/{id:guid}", async (Guid id, [FromServices] IEventInteractionService service, CancellationToken cancellationToken)
            => await service.GetById(id, includeVersions: true, cancellationToken) is { } item
                ? Results.Ok(item.ToResponse())
                : Results.NotFound());

        group.MapPost("/", async ([FromBody] CreateEventRequest request, [FromServices] IEventInteractionService service, CancellationToken cancellationToken) =>
        {
            var item = new EventDefinition
            {
                Name = request.Name.Trim(),
                Topic = request.Topic.Trim(),
                Description = request.Description,
                Versions =
                {
                    new EventVersion
                    {
                        VersionNumber = request.VersionNumber.Trim(),
                        PayloadSchemaJson = request.PayloadSchemaJson,
                        Comment = request.Comment
                    }
                }
            };

            await service.Create(item, cancellationToken);
            return Results.Created($"/api/v1/control-plane/events/{item.Id}", item.ToResponse());
        });

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateEventRequest request, [FromServices] IEventInteractionService service, CancellationToken cancellationToken) =>
        {
            if (await service.GetById(id, cancellationToken: cancellationToken) is null)
            {
                return Results.NotFound();
            }

            await service.UpdateDefinition(id, request.Name.Trim(), request.Topic.Trim(), request.Description, DateTime.UtcNow, cancellationToken);
            var updated = await service.GetById(id, includeVersions: true, cancellationToken);
            return Results.Ok(updated!.ToResponse());
        });

        group.MapPost("/{id:guid}/versions", async (Guid id, [FromBody] CreateEventVersionRequest request, [FromServices] IEventInteractionService service, CancellationToken cancellationToken) =>
        {
            if (await service.GetById(id, cancellationToken: cancellationToken) is null)
            {
                return Results.NotFound();
            }

            if (await service.VersionExists(id, request.VersionNumber, cancellationToken))
            {
                return Conflict($"Event version '{request.VersionNumber}' already exists.");
            }

            var version = new EventVersion
            {
                EventDefinitionId = id,
                VersionNumber = request.VersionNumber.Trim(),
                PayloadSchemaJson = request.PayloadSchemaJson,
                Comment = request.Comment
            };
            await service.AddVersion(id, version, cancellationToken);
            return Results.Created($"/api/v1/control-plane/events/{id}/versions/{version.Id}", version.ToResponse());
        });

        group.MapPost("/{id:guid}/versions/{versionId:guid}/transition", async (Guid id, Guid versionId, [FromBody] TransitionVersionRequest request, [FromServices] IEventInteractionService events, [FromServices] IContractVersionPromotionService promotion, CancellationToken cancellationToken) =>
        {
            if (await events.GetById(id, cancellationToken: cancellationToken) is null)
            {
                return Results.NotFound();
            }

            await promotion.TransitionEventVersion(versionId, request.TargetStatus, cancellationToken);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] IEventInteractionService service, CancellationToken cancellationToken) =>
        {
            if (await service.GetById(id, cancellationToken: cancellationToken) is null)
            {
                return Results.NotFound();
            }

            await service.Delete(id, cancellationToken);
            return Results.NoContent();
        });
    }

    private static void MapCommands(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/commands").WithTags("KnOwl Control Plane Commands");

        group.MapGet("/", async ([FromServices] ICommandInteractionService service, CancellationToken cancellationToken)
            => Results.Ok((await service.GetAll(cancellationToken)).Select(x => x.ToResponse())));

        group.MapGet("/{id:guid}", async (Guid id, [FromServices] ICommandInteractionService service, CancellationToken cancellationToken)
            => await service.GetById(id, includeVersions: true, cancellationToken) is { } item
                ? Results.Ok(item.ToResponse())
                : Results.NotFound());

        group.MapPost("/", async ([FromBody] CreateCommandRequest request, [FromServices] ICommandInteractionService service, CancellationToken cancellationToken) =>
        {
            var item = new CommandDefinition
            {
                Name = request.Name.Trim(),
                Topic = request.Topic.Trim(),
                Description = request.Description,
                Versions =
                {
                    new CommandVersion
                    {
                        VersionNumber = request.VersionNumber.Trim(),
                        PayloadSchemaJson = request.RequestDefinitionJson,
                        ReplyPayloadSchemaJson = NormalizeOptionalJson(request.ReplyDefinitionJson),
                        Comment = request.Comment
                    }
                }
            };

            await service.Create(item, cancellationToken);
            return Results.Created($"/api/v1/control-plane/commands/{item.Id}", item.ToResponse());
        });

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateCommandRequest request, [FromServices] ICommandInteractionService service, CancellationToken cancellationToken) =>
        {
            if (await service.GetById(id, cancellationToken: cancellationToken) is null)
            {
                return Results.NotFound();
            }

            await service.UpdateDefinition(id, request.Name.Trim(), request.Topic.Trim(), request.Description, DateTime.UtcNow, cancellationToken);
            var updated = await service.GetById(id, includeVersions: true, cancellationToken);
            return Results.Ok(updated!.ToResponse());
        });

        group.MapPost("/{id:guid}/versions", async (Guid id, [FromBody] CreateCommandVersionRequest request, [FromServices] ICommandInteractionService service, CancellationToken cancellationToken) =>
        {
            if (await service.GetById(id, cancellationToken: cancellationToken) is null)
            {
                return Results.NotFound();
            }

            if (await service.VersionExists(id, request.VersionNumber, cancellationToken))
            {
                return Conflict($"Command version '{request.VersionNumber}' already exists.");
            }

            var version = new CommandVersion
            {
                CommandDefinitionId = id,
                VersionNumber = request.VersionNumber.Trim(),
                PayloadSchemaJson = request.RequestDefinitionJson,
                ReplyPayloadSchemaJson = NormalizeOptionalJson(request.ReplyDefinitionJson),
                Comment = request.Comment
            };
            await service.AddVersion(id, version, cancellationToken);
            return Results.Created($"/api/v1/control-plane/commands/{id}/versions/{version.Id}", version.ToResponse());
        });

        group.MapPost("/{id:guid}/versions/{versionId:guid}/transition", async (Guid id, Guid versionId, [FromBody] TransitionVersionRequest request, [FromServices] ICommandInteractionService commands, [FromServices] IContractVersionPromotionService promotion, CancellationToken cancellationToken) =>
        {
            if (await commands.GetById(id, cancellationToken: cancellationToken) is null)
            {
                return Results.NotFound();
            }

            await promotion.TransitionCommandVersion(versionId, request.TargetStatus, cancellationToken);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] ICommandInteractionService service, CancellationToken cancellationToken) =>
        {
            if (await service.GetById(id, cancellationToken: cancellationToken) is null)
            {
                return Results.NotFound();
            }

            await service.Delete(id, cancellationToken);
            return Results.NoContent();
        });
    }

    private static void MapArtifacts(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/artifacts").WithTags("KnOwl Control Plane Artifacts");

        group.MapGet("/", async ([FromServices] IContractArtifactRepository repository, CancellationToken cancellationToken)
            => Results.Ok((await repository.GetAll(cancellationToken)).Select(x => x.ToResponse())));

        group.MapGet("/deployed", async ([FromServices] IControlPlaneContractCatalogService catalog, CancellationToken cancellationToken)
            => Results.Ok((await catalog.GetAll(cancellationToken)).Select(x => x.ToResponse())));

        group.MapPost("/events/{versionId:guid}/build", async (Guid versionId, [FromServices] IContractArtifactBuilder builder, CancellationToken cancellationToken) =>
        {
            var artifact = await builder.BuildEventArtifact(versionId, cancellationToken);
            return Results.Ok(new BuildArtifactResponse([artifact.ToResponse()]));
        });

        group.MapPost("/commands/{versionId:guid}/build", async (Guid versionId, [FromServices] IContractArtifactBuilder builder, CancellationToken cancellationToken) =>
        {
            var artifacts = await builder.BuildCommandArtifacts(versionId, cancellationToken);
            return Results.Ok(new BuildArtifactResponse(artifacts.Select(x => x.ToResponse()).ToArray()));
        });
    }

    private static void MapRuntimeEnvironments(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/runtime-environments").WithTags("KnOwl Control Plane Runtime Environments");

        group.MapGet("/", async ([FromServices] IRuntimeEnvironmentRepository repository, CancellationToken cancellationToken)
            => Results.Ok((await repository.GetAll(cancellationToken)).Select(x => x.ToResponse())));

        group.MapGet("/{id:guid}", async (Guid id, [FromServices] IRuntimeEnvironmentRepository repository, CancellationToken cancellationToken)
            => await repository.GetById(id, cancellationToken) is { } item
                ? Results.Ok(item.ToResponse())
                : Results.NotFound());

        group.MapPost("/", async ([FromBody] CreateRuntimeEnvironmentRequest request, [FromServices] IRuntimeEnvironmentRepository repository, CancellationToken cancellationToken) =>
        {
            var item = new RuntimeEnvironment
            {
                Name = request.Name.Trim(),
                Code = request.Code.Trim(),
                Description = request.Description,
                IsEnabled = request.IsEnabled
            };
            await repository.Create(item, cancellationToken);
            return Results.Created($"/api/v1/control-plane/runtime-environments/{item.Id}", item.ToResponse());
        });

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateRuntimeEnvironmentRequest request, [FromServices] IRuntimeEnvironmentRepository repository, CancellationToken cancellationToken) =>
        {
            var item = await repository.GetById(id, cancellationToken);
            if (item is null)
            {
                return Results.NotFound();
            }

            item.Name = request.Name.Trim();
            item.Code = request.Code.Trim();
            item.Description = request.Description;
            item.IsEnabled = request.IsEnabled;
            item.UpdatedAtUtc = DateTime.UtcNow;
            await repository.Update(item, cancellationToken);
            return Results.Ok(item.ToResponse());
        });
    }

    private static void MapRuntimeNodes(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/runtime-nodes").WithTags("KnOwl Control Plane Runtime Nodes");

        group.MapGet("/", async ([FromServices] IRuntimeNodeRepository repository, CancellationToken cancellationToken)
            => Results.Ok((await repository.GetAll(cancellationToken)).Select(x => x.ToResponse())));

        group.MapGet("/{id:guid}", async (Guid id, [FromServices] IRuntimeNodeRepository repository, CancellationToken cancellationToken)
            => await repository.GetById(id, cancellationToken) is { } item
                ? Results.Ok(item.ToResponse())
                : Results.NotFound());

        group.MapPost("/", async ([FromBody] CreateRuntimeNodeRequest request, [FromServices] IRuntimeNodeRepository repository, CancellationToken cancellationToken) =>
        {
            if (await repository.GetByCode(request.Code.Trim(), cancellationToken) is not null)
            {
                return Conflict($"Runtime node code '{request.Code}' already exists.");
            }

            var item = new RuntimeNode
            {
                Name = request.Name.Trim(),
                Code = request.Code.Trim(),
                EnvironmentId = request.EnvironmentId,
                EnvironmentName = request.EnvironmentName.Trim(),
                DistributionMode = request.DistributionMode,
                EndpointBaseUri = request.EndpointBaseUri.Trim().TrimEnd('/'),
                EndpointApiPath = request.EndpointApiPath.Trim('/'),
                IsEnabled = request.IsEnabled,
                Description = request.Description,
                Status = RuntimeNodeStatus.Active
            };
            await repository.Create(item, cancellationToken);
            return Results.Created($"/api/v1/control-plane/runtime-nodes/{item.Id}", item.ToResponse());
        });

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateRuntimeNodeRequest request, [FromServices] IRuntimeNodeRepository repository, CancellationToken cancellationToken) =>
        {
            var item = await repository.GetById(id, cancellationToken);
            if (item is null)
            {
                return Results.NotFound();
            }

            var duplicate = await repository.GetByCode(request.Code.Trim(), cancellationToken);
            if (duplicate is not null && duplicate.Id != id)
            {
                return Conflict($"Runtime node code '{request.Code}' already exists.");
            }

            item.Name = request.Name.Trim();
            item.Code = request.Code.Trim();
            item.EnvironmentId = request.EnvironmentId;
            item.EnvironmentName = request.EnvironmentName.Trim();
            item.DistributionMode = request.DistributionMode;
            item.EndpointBaseUri = request.EndpointBaseUri.Trim().TrimEnd('/');
            item.EndpointApiPath = request.EndpointApiPath.Trim('/');
            item.IsEnabled = request.IsEnabled;
            item.Description = request.Description;
            item.LastUpdatedAtUtc = DateTime.UtcNow;
            await repository.Update(item, cancellationToken);
            return Results.Ok(item.ToResponse());
        });

        group.MapPost("/{id:guid}/credentials/generate", async (Guid id, string issuerBaseUrl, [FromServices] IRuntimeNodeConnectionInteractionService service, [FromServices] IRuntimeNodeRepository repository, CancellationToken cancellationToken) =>
        {
            if (await repository.GetById(id, cancellationToken) is null)
            {
                return Results.NotFound();
            }

            var package = await service.GenerateCredentialPackage(id, issuerBaseUrl, cancellationToken);
            return Results.Ok(new CredentialPackageResponse(package.Json));
        });

        group.MapPost("/{id:guid}/credentials/import", async (Guid id, [FromBody] ImportRuntimeNodeCredentialPackageRequest request, [FromServices] IRuntimeNodeConnectionInteractionService service, [FromServices] IRuntimeNodeRepository repository, CancellationToken cancellationToken) =>
        {
            if (await repository.GetById(id, cancellationToken) is null)
            {
                return Results.NotFound();
            }

            await service.ImportCredentialPackage(new ImportRuntimeNodeCredentialPackageInput
            {
                RuntimeNodeId = id,
                Package = request.CredentialPackageJson
            }, cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/connect/validate", async (Guid id, [FromServices] IRuntimeNodeConnectionInteractionService service, [FromServices] IRuntimeNodeRepository repository, CancellationToken cancellationToken) =>
        {
            if (await repository.GetById(id, cancellationToken) is null)
            {
                return Results.NotFound();
            }

            var result = await service.ValidateConnection(id, cancellationToken);
            return Results.Ok(new ConnectionValidationResponse(result.Succeeded, result.Message));
        });
    }

    private static void MapReleases(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/releases").WithTags("KnOwl Control Plane Releases");

        group.MapGet("/", async ([FromServices] IContractReleaseRepository repository, CancellationToken cancellationToken)
            => Results.Ok((await repository.GetAll(cancellationToken)).Select(x => x.ToResponse())));

        group.MapGet("/{id:guid}", async (Guid id, [FromServices] IContractReleaseRepository repository, CancellationToken cancellationToken)
            => await repository.GetById(id, includeItems: true, includeTargets: true, cancellationToken) is { } item
                ? Results.Ok(item.ToResponse())
                : Results.NotFound());

        group.MapPost("/", async ([FromBody] CreateReleaseRequest request, [FromServices] IContractReleaseInteractionService service, CancellationToken cancellationToken) =>
        {
            var release = await service.Create(request.Name.Trim(), request.Description, request.ArtifactIds, cancellationToken);
            return Results.Created($"/api/v1/control-plane/releases/{release.Id}", release.ToResponse());
        });

        group.MapPost("/{id:guid}/plan", async (Guid id, [FromBody] PlanReleaseRequest request, [FromServices] IContractReleaseInteractionService service, [FromServices] IContractReleaseRepository repository, CancellationToken cancellationToken) =>
        {
            if (await repository.GetById(id, cancellationToken: cancellationToken) is null)
            {
                return Results.NotFound();
            }

            var release = await service.Plan(id, request.RuntimeNodeIds, request.RolloutGroup, cancellationToken);
            return Results.Ok(release.ToResponse());
        });

        group.MapPost("/{id:guid}/execute", async (Guid id, [FromBody] ExecuteReleaseRequest request, [FromServices] IContractReleaseExecutionService execution, [FromServices] IContractReleaseRepository repository, CancellationToken cancellationToken) =>
        {
            if (await repository.GetById(id, cancellationToken: cancellationToken) is null)
            {
                return Results.NotFound();
            }

            var result = await execution.Execute(id, request.InitiatedBy, cancellationToken);
            var release = await repository.GetById(result.ReleaseId, includeItems: true, includeTargets: true, cancellationToken);
            return Results.Ok(new ContractReleaseExecutionResponse(release!.ToResponse(), result.Results.Select(x => x.ToResponse()).ToArray()));
        });

        group.MapPost("/execute", async ([FromBody] CreateAndExecuteReleaseRequest request, [FromServices] IContractReleaseExecutionService execution, [FromServices] IContractReleaseRepository repository, CancellationToken cancellationToken) =>
        {
            var result = await execution.CreateAndExecute(
                request.Name.Trim(),
                request.Description,
                request.ArtifactIds,
                request.RuntimeNodeIds,
                request.RolloutGroup,
                request.InitiatedBy,
                cancellationToken);
            var release = await repository.GetById(result.ReleaseId, includeItems: true, includeTargets: true, cancellationToken);
            return Results.Ok(new ContractReleaseExecutionResponse(release!.ToResponse(), result.Results.Select(x => x.ToResponse()).ToArray()));
        });
    }

    private static IResult Conflict(string detail)
        => Results.Problem(detail: detail, statusCode: StatusCodes.Status409Conflict);

    private static string? NormalizeOptionalJson(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}

