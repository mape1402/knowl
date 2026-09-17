using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.Security;

namespace KnOwl.Runtime.Api.Contracts;

public sealed record RuntimeStatusResponse(string Service, string Status);

public sealed record RuntimeArtifactResponse(
    Guid Id,
    Guid SourceArtifactId,
    Guid SourceReleaseId,
    ContractArtifactType ArtifactType,
    Guid DefinitionId,
    Guid VersionId,
    string Name,
    string Topic,
    string VersionNumber,
    string? Description,
    string PayloadSchemaJson,
    string ContentHash,
    DateTime DeployedAtUtc);

public sealed record RuntimeCommandArtifactsResponse(
    string CommandKey,
    string Version,
    RuntimeArtifactResponse RequestArtifact,
    RuntimeArtifactResponse? ReplyArtifact);

public sealed record RuntimeDesignNodeResponse(
    Guid Id,
    string Key,
    string Name,
    DistributionMode DistributionMode,
    string EndpointBaseUri,
    string RemoteRuntimeNodeId,
    bool IsEnabled,
    string? Description,
    string Status,
    ConnectionCredentialStatus InboundCredentialStatus,
    ConnectionCredentialStatus OutboundCredentialStatus);

public sealed record UpsertRuntimeDesignNodeRequest(
    Guid? Id,
    string Key,
    string Name,
    DistributionMode DistributionMode,
    string EndpointBaseUri,
    string RemoteRuntimeNodeId,
    bool IsEnabled);

public sealed record RuntimeCredentialPackageResponse(string Json);

public sealed record ImportRuntimeDesignNodeCredentialPackageRequest(
    Guid DesignNodeId,
    string CredentialPackageJson);

public sealed record RuntimeConnectionValidationResponse(bool Succeeded, string Message);

public sealed record ApplyArtifactResponse(
    Guid ReleaseTargetId,
    bool Succeeded,
    string Status,
    string Message);
