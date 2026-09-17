using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.Security.Storage;

namespace KnOwl.ControlPlane.Api.Contracts;

public sealed record SchemaTypeResponse(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive,
    IReadOnlyList<SchemaTypeVersionResponse> Versions);

public sealed record SchemaTypeVersionResponse(
    Guid Id,
    string VersionNumber,
    string DefinitionJson,
    string? Comment,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateSchemaTypeRequest(
    string Key,
    string Name,
    string? Description,
    string VersionNumber,
    string DefinitionJson,
    string? Comment,
    bool IsActive = true);

public sealed record UpdateSchemaTypeRequest(
    string Key,
    string Name,
    string? Description,
    bool IsActive);

public sealed record CreateSchemaTypeVersionRequest(
    string VersionNumber,
    string DefinitionJson,
    string? Comment,
    bool IsActive = true);

public sealed record SetVersionActiveRequest(bool IsActive);

public sealed record MetadataFieldResponse(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    bool IsActive,
    IReadOnlyList<MetadataFieldVersionResponse> Versions);

public sealed record MetadataFieldVersionResponse(
    Guid Id,
    string VersionNumber,
    string DefinitionJson,
    string? Comment,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateMetadataFieldRequest(
    string Key,
    string Name,
    string? Description,
    string VersionNumber,
    string DefinitionJson,
    string? Comment,
    bool IsActive = true);

public sealed record UpdateMetadataFieldRequest(
    string Key,
    string Name,
    string? Description,
    bool IsActive);

public sealed record UpsertMetadataFieldVersionRequest(
    string VersionNumber,
    string DefinitionJson,
    string? Comment,
    bool IsActive = true);

public sealed record EventDefinitionResponse(
    Guid Id,
    string Name,
    string Topic,
    string? Description,
    bool IsActive,
    IReadOnlyList<EventVersionResponse> Versions);

public sealed record EventVersionResponse(
    Guid Id,
    string VersionNumber,
    string PayloadSchemaJson,
    string? Comment,
    ContractVersionStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateEventRequest(
    string Name,
    string Topic,
    string? Description,
    string VersionNumber,
    string PayloadSchemaJson,
    string? Comment);

public sealed record UpdateEventRequest(
    string Name,
    string Topic,
    string? Description);

public sealed record CreateEventVersionRequest(
    string VersionNumber,
    string PayloadSchemaJson,
    string? Comment);

public sealed record CommandDefinitionResponse(
    Guid Id,
    string Name,
    string Topic,
    string? Description,
    bool IsActive,
    IReadOnlyList<CommandVersionResponse> Versions);

public sealed record CommandVersionResponse(
    Guid Id,
    string VersionNumber,
    string RequestDefinitionJson,
    string? ReplyDefinitionJson,
    string? Comment,
    ContractVersionStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateCommandRequest(
    string Name,
    string Topic,
    string? Description,
    string VersionNumber,
    string RequestDefinitionJson,
    string? ReplyDefinitionJson,
    string? Comment);

public sealed record UpdateCommandRequest(
    string Name,
    string Topic,
    string? Description);

public sealed record CreateCommandVersionRequest(
    string VersionNumber,
    string RequestDefinitionJson,
    string? ReplyDefinitionJson,
    string? Comment);

public sealed record TransitionVersionRequest(ContractVersionStatus TargetStatus);

public sealed record ContractArtifactResponse(
    Guid Id,
    ContractArtifactType ArtifactType,
    Guid DefinitionId,
    Guid VersionId,
    string Name,
    string Topic,
    string VersionNumber,
    string? Description,
    string PayloadSchemaJson,
    string ContentHash,
    string SourceStatus,
    DateTime CreatedAtUtc);

public sealed record BuildArtifactResponse(IReadOnlyList<ContractArtifactResponse> Artifacts);

public sealed record RuntimeEnvironmentResponse(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    bool IsEnabled,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CreateRuntimeEnvironmentRequest(
    string Name,
    string Code,
    string? Description,
    bool IsEnabled = true);

public sealed record UpdateRuntimeEnvironmentRequest(
    string Name,
    string Code,
    string? Description,
    bool IsEnabled);

public sealed record RuntimeNodeResponse(
    Guid Id,
    string Name,
    string Code,
    Guid? EnvironmentId,
    string EnvironmentName,
    DistributionMode DistributionMode,
    string EndpointBaseUri,
    string EndpointApiPath,
    RuntimeNodeStatus Status,
    bool IsEnabled,
    string? Description,
    ConnectionCredentialStatus InboundCredentialStatus,
    ConnectionCredentialStatus OutboundCredentialStatus);

public sealed record CreateRuntimeNodeRequest(
    string Name,
    string Code,
    Guid? EnvironmentId,
    string EnvironmentName,
    DistributionMode DistributionMode,
    string EndpointBaseUri,
    string EndpointApiPath,
    bool IsEnabled = true,
    string? Description = null);

public sealed record UpdateRuntimeNodeRequest(
    string Name,
    string Code,
    Guid? EnvironmentId,
    string EnvironmentName,
    DistributionMode DistributionMode,
    string EndpointBaseUri,
    string EndpointApiPath,
    bool IsEnabled,
    string? Description);

public sealed record ImportRuntimeNodeCredentialPackageRequest(string CredentialPackageJson);

public sealed record CredentialPackageResponse(string Json);

public sealed record ConnectionValidationResponse(bool Succeeded, string Message);

public sealed record CreateReleaseRequest(
    string Name,
    string? Description,
    IReadOnlyCollection<Guid> ArtifactIds);

public sealed record PlanReleaseRequest(
    IReadOnlyCollection<Guid> RuntimeNodeIds,
    string RolloutGroup = "ManualRelease");

public sealed record ExecuteReleaseRequest(string InitiatedBy = "api");

public sealed record CreateAndExecuteReleaseRequest(
    string Name,
    string? Description,
    IReadOnlyCollection<Guid> ArtifactIds,
    IReadOnlyCollection<Guid> RuntimeNodeIds,
    string RolloutGroup = "ManualRelease",
    string InitiatedBy = "api");

public sealed record ContractReleaseResponse(
    Guid Id,
    string Name,
    string? Description,
    ContractReleaseStatus Status,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? FailedAtUtc,
    IReadOnlyList<Guid> ArtifactIds,
    IReadOnlyList<ReleaseTargetResponse> Targets);

public sealed record ReleaseTargetResponse(
    Guid Id,
    Guid RuntimeNodeId,
    Guid ArtifactId,
    string RolloutGroup,
    ContractReleaseTargetStatus Status,
    ContractReleaseActivationStatus ActivationStatus,
    string CorrelationId);

public sealed record ContractReleaseExecutionResponse(
    ContractReleaseResponse Release,
    IReadOnlyList<DeliveryResultResponse> Results);

public sealed record DeliveryResultResponse(
    Guid ReleaseTargetId,
    bool Succeeded,
    string Status,
    string Message);

public sealed record SecuritySubjectResponse(
    Guid Id,
    string Provider,
    string SubjectId,
    string? DisplayName,
    string? Email,
    bool IsEnabled,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record UpsertSecuritySubjectRequest(
    string Provider,
    string SubjectId,
    string? DisplayName,
    string? Email,
    bool IsEnabled = true);

public sealed record SecurityRoleAssignmentResponse(
    Guid Id,
    string Provider,
    string SubjectId,
    string Role,
    string ScopeType,
    string ScopeId,
    bool IsEnabled,
    KnOwlSecurityAssignmentSource Source,
    DateTime CreatedAtUtc);

public sealed record AssignSecurityRoleRequest(
    string Provider,
    string SubjectId,
    string Role,
    string ScopeType,
    string ScopeId,
    bool IsEnabled = true);

public sealed record SecurityPermissionAssignmentResponse(
    Guid Id,
    string Provider,
    string SubjectId,
    string Permission,
    string ScopeType,
    string ScopeId,
    bool IsEnabled,
    KnOwlSecurityAssignmentSource Source,
    DateTime CreatedAtUtc);

public sealed record AssignSecurityPermissionRequest(
    string Provider,
    string SubjectId,
    string Permission,
    string ScopeType,
    string ScopeId,
    bool IsEnabled = true);

public sealed record SecurityExternalGroupRoleAssignmentResponse(
    Guid Id,
    string Provider,
    string ExternalGroupId,
    string Role,
    string ScopeType,
    string ScopeId,
    bool IsEnabled,
    KnOwlSecurityAssignmentSource Source,
    DateTime CreatedAtUtc);

public sealed record AssignSecurityExternalGroupRoleRequest(
    string Provider,
    string ExternalGroupId,
    string Role,
    string ScopeType,
    string ScopeId,
    bool IsEnabled = true);
