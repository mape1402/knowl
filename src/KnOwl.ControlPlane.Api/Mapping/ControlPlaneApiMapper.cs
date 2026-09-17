using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Api.Contracts;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.Security.Storage;

namespace KnOwl.ControlPlane.Api.Mapping;

internal static class ControlPlaneApiMapper
{
    public static SchemaTypeResponse ToResponse(this SchemaTypeDefinition value)
        => new(
            value.Id,
            value.Key,
            value.Name,
            value.Description,
            value.IsSystem,
            value.IsActive,
            value.Versions.OrderBy(x => x.VersionNumber).Select(x => x.ToResponse()).ToArray());

    public static SchemaTypeVersionResponse ToResponse(this SchemaTypeVersion value)
        => new(value.Id, value.VersionNumber, value.DefinitionJson, value.Comment, value.IsActive, value.CreatedAtUtc, value.UpdatedAtUtc);

    public static MetadataFieldResponse ToResponse(this ContractFieldMetadataDefinition value)
        => new(
            value.Id,
            value.Key,
            value.Name,
            value.Description,
            value.IsActive,
            value.Versions.OrderBy(x => x.VersionNumber).Select(x => x.ToResponse()).ToArray());

    public static MetadataFieldVersionResponse ToResponse(this ContractFieldMetadataVersion value)
        => new(value.Id, value.VersionNumber, value.DefinitionJson, value.Comment, value.IsActive, value.CreatedAtUtc, value.UpdatedAtUtc);

    public static EventDefinitionResponse ToResponse(this EventDefinition value)
        => new(
            value.Id,
            value.Name,
            value.Topic,
            value.Description,
            value.IsActive,
            value.Versions.OrderBy(x => x.VersionNumber).Select(x => x.ToResponse()).ToArray());

    public static EventVersionResponse ToResponse(this EventVersion value)
        => new(value.Id, value.VersionNumber, value.PayloadSchemaJson, value.Comment, value.Status, value.CreatedAtUtc, value.UpdatedAtUtc);

    public static CommandDefinitionResponse ToResponse(this CommandDefinition value)
        => new(
            value.Id,
            value.Name,
            value.Topic,
            value.Description,
            value.IsActive,
            value.Versions.OrderBy(x => x.VersionNumber).Select(x => x.ToResponse()).ToArray());

    public static CommandVersionResponse ToResponse(this CommandVersion value)
        => new(
            value.Id,
            value.VersionNumber,
            value.PayloadSchemaJson,
            value.ReplyPayloadSchemaJson,
            value.Comment,
            value.Status,
            value.CreatedAtUtc,
            value.UpdatedAtUtc);

    public static ContractArtifactResponse ToResponse(this ContractArtifact value)
        => new(
            value.Id,
            value.ArtifactType,
            value.DefinitionId,
            value.VersionId,
            value.Name,
            value.Topic,
            value.VersionNumber,
            value.Description,
            value.PayloadSchemaJson,
            value.ContentHash,
            value.SourceStatus,
            value.CreatedAtUtc);

    public static RuntimeEnvironmentResponse ToResponse(this RuntimeEnvironment value)
        => new(value.Id, value.Name, value.Code, value.Description, value.IsEnabled, value.CreatedAtUtc, value.UpdatedAtUtc);

    public static RuntimeNodeResponse ToResponse(this RuntimeNode value)
        => new(
            value.Id,
            value.Name,
            value.Code,
            value.EnvironmentId,
            value.EnvironmentName,
            value.DistributionMode,
            value.EndpointBaseUri,
            value.EndpointApiPath,
            value.Status,
            value.IsEnabled,
            value.Description,
            value.InboundCredentialStatus,
            value.OutboundCredentialStatus);

    public static ContractReleaseResponse ToResponse(this ContractRelease value)
        => new(
            value.Id,
            value.Name,
            value.Description,
            value.Status,
            value.CreatedAtUtc,
            value.CompletedAtUtc,
            value.FailedAtUtc,
            value.Items.Select(x => x.ArtifactId).ToArray(),
            value.Targets.Select(x => x.ToResponse()).ToArray());

    public static ReleaseTargetResponse ToResponse(this ContractReleaseTarget value)
        => new(
            value.Id,
            value.RuntimeNodeId,
            value.ArtifactId,
            value.RolloutGroup,
            value.Status,
            value.ActivationStatus,
            value.CorrelationId);

    public static DeliveryResultResponse ToResponse(this RuntimeArtifactDeliveryResult value)
        => new(value.ReleaseTargetId, value.Succeeded, value.Status, value.Message);

    public static SecuritySubjectResponse ToResponse(this KnOwlSubject value)
        => new(value.Id, value.Provider, value.SubjectId, value.DisplayName, value.Email, value.IsEnabled, value.CreatedAtUtc, value.UpdatedAtUtc);

    public static SecurityRoleAssignmentResponse ToResponse(this KnOwlRoleAssignment value)
        => new(value.Id, value.Provider, value.SubjectId, value.Role, value.ScopeType, value.ScopeId, value.IsEnabled, value.Source, value.CreatedAtUtc);

    public static SecurityPermissionAssignmentResponse ToResponse(this KnOwlPermissionAssignment value)
        => new(value.Id, value.Provider, value.SubjectId, value.Permission, value.ScopeType, value.ScopeId, value.IsEnabled, value.Source, value.CreatedAtUtc);

    public static SecurityExternalGroupRoleAssignmentResponse ToResponse(this KnOwlExternalGroupRoleAssignment value)
        => new(value.Id, value.Provider, value.ExternalGroupId, value.Role, value.ScopeType, value.ScopeId, value.IsEnabled, value.Source, value.CreatedAtUtc);
}
