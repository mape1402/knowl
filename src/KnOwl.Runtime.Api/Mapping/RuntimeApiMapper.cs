using KnOwl.Runtime.Api.Contracts;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Distribution;

namespace KnOwl.Runtime.Api.Mapping;

internal static class RuntimeApiMapper
{
    public static RuntimeArtifactResponse ToResponse(this RuntimeContractArtifact value)
        => new(
            value.Id,
            value.SourceArtifactId,
            value.SourceReleaseId,
            value.ArtifactType,
            value.DefinitionId,
            value.VersionId,
            value.Name,
            value.Topic,
            value.VersionNumber,
            value.Description,
            value.PayloadSchemaJson,
            value.ContentHash,
            value.DeployedAtUtc);

    public static RuntimeDesignNodeResponse ToResponse(this RuntimeDesignNode value)
        => new(
            value.Id,
            value.Key,
            value.Name,
            value.DistributionMode,
            value.EndpointBaseUri,
            value.RemoteRuntimeNodeId,
            value.IsEnabled,
            value.Description,
            value.Status.ToString(),
            value.InboundCredentialStatus,
            value.OutboundCredentialStatus);
}
