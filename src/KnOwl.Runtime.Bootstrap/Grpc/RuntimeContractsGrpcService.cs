using KnOwl.Contracts.Artifacts;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Application.Catalog;
using Grpc.Core;

namespace KnOwl.Runtime.Bootstrap.Grpc;

/// <summary>
/// gRPC service used by backend services to read KnOwl Runtime contract metadata artifacts.
/// </summary>
public sealed class RuntimeContractsGrpcService(IRuntimeContractCatalogService catalog) : RuntimeContracts.RuntimeContractsBase
{
    /// <inheritdoc />
    public override async Task<RuntimeContractResponse> GetExact(RuntimeContractExactRequest request, ServerCallContext context)
    {
        if (!TryParseArtifactType(request.ArtifactType, out var artifactType))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Artifact type '{request.ArtifactType}' is not supported."));
        }

        var artifact = await catalog.GetExact(artifactType, request.Topic, request.VersionNumber, context.CancellationToken);
        return ToResponse(artifact);
    }

    /// <inheritdoc />
    public override async Task<RuntimeContractResponse> GetLatest(RuntimeContractLatestRequest request, ServerCallContext context)
    {
        if (!TryParseArtifactType(request.ArtifactType, out var artifactType))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Artifact type '{request.ArtifactType}' is not supported."));
        }

        var artifact = await catalog.GetLatest(artifactType, request.Topic, context.CancellationToken);
        return ToResponse(artifact);
    }

    private static RuntimeContractResponse ToResponse(RuntimeContractArtifact? artifact)
    {
        if (artifact is null)
        {
            return new RuntimeContractResponse { Found = false };
        }

        return new RuntimeContractResponse
        {
            Found = true,
            Id = artifact.Id.ToString("N"),
            SourceArtifactId = artifact.SourceArtifactId.ToString("N"),
            SourceReleaseId = artifact.SourceReleaseId.ToString("N"),
            ArtifactType = artifact.ArtifactType.ToString(),
            DefinitionId = artifact.DefinitionId.ToString("N"),
            VersionId = artifact.VersionId.ToString("N"),
            Name = artifact.Name,
            Topic = artifact.Topic,
            VersionNumber = artifact.VersionNumber,
            Description = artifact.Description ?? string.Empty,
            PayloadSchemaJson = artifact.PayloadSchemaJson,
            ContentHash = artifact.ContentHash,
            DeployedAtUtc = artifact.DeployedAtUtc.ToUniversalTime().ToString("O")
        };
    }

    private static bool TryParseArtifactType(string artifactType, out ContractArtifactType parsedType)
        => Enum.TryParse(artifactType, ignoreCase: true, out parsedType);
}
