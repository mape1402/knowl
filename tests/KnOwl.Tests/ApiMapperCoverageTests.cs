using System.Reflection;
using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Api;
using KnOwl.ControlPlane.Api.Contracts;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.Documentation;
using KnOwl.Documentation.Api;
using KnOwl.Runtime.Api;
using KnOwl.Runtime.Api.Contracts;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Distribution;
using KnOwl.Security.Storage;

namespace KnOwl.Tests;

public sealed class ApiMapperCoverageTests
{
    [Fact]
    public void ControlPlaneMapperMapsDesignCatalogResponses()
    {
        var schemaType = new SchemaTypeDefinition
        {
            Id = Guid.NewGuid(),
            Key = "customer",
            Name = "Customer",
            Description = "Customer type",
            IsSystem = false,
            IsActive = true,
            Versions =
            [
                new SchemaTypeVersion { Id = Guid.NewGuid(), VersionNumber = "2.0.0", DefinitionJson = """{"type":"object"}""", Comment = "second", IsActive = false },
                new SchemaTypeVersion { Id = Guid.NewGuid(), VersionNumber = "1.0.0", DefinitionJson = """{"type":"object"}""", Comment = "first", IsActive = true }
            ]
        };
        var schemaResponse = MapControlPlane<SchemaTypeDefinition, SchemaTypeResponse>(schemaType);
        Assert.Equal("customer", schemaResponse.Key);
        Assert.Equal(["1.0.0", "2.0.0"], schemaResponse.Versions.Select(x => x.VersionNumber).ToArray());

        var metadata = new ContractFieldMetadataDefinition
        {
            Id = Guid.NewGuid(),
            Key = "trace-id",
            Name = "Trace Id",
            Description = "Correlation",
            IsActive = true,
            Versions =
            [
                new ContractFieldMetadataVersion { Id = Guid.NewGuid(), VersionNumber = "1.1.0", DefinitionJson = "{}", Comment = "next" },
                new ContractFieldMetadataVersion { Id = Guid.NewGuid(), VersionNumber = "1.0.0", DefinitionJson = "{}", Comment = "initial" }
            ]
        };
        var metadataResponse = MapControlPlane<ContractFieldMetadataDefinition, MetadataFieldResponse>(metadata);
        Assert.Equal("trace-id", metadataResponse.Key);
        Assert.Equal(["1.0.0", "1.1.0"], metadataResponse.Versions.Select(x => x.VersionNumber).ToArray());

        var @event = new EventDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Customer Created",
            Topic = "customer.created",
            Description = "Event",
            Versions =
            [
                new EventVersion { Id = Guid.NewGuid(), VersionNumber = "1.0.1", PayloadSchemaJson = "{}", Comment = "patch", Status = ContractVersionStatus.InReview },
                new EventVersion { Id = Guid.NewGuid(), VersionNumber = "1.0.0", PayloadSchemaJson = "{}", Comment = "initial", Status = ContractVersionStatus.Deployed }
            ]
        };
        var eventResponse = MapControlPlane<EventDefinition, EventDefinitionResponse>(@event);
        Assert.Equal("customer.created", eventResponse.Topic);
        Assert.Equal(ContractVersionStatus.Deployed, eventResponse.Versions[0].Status);

        var command = new CommandDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Create Customer",
            Topic = "customer.create",
            Description = "Command",
            Versions =
            [
                new CommandVersion { Id = Guid.NewGuid(), VersionNumber = "2.0.0", PayloadSchemaJson = "{}", ReplyPayloadSchemaJson = null, Comment = "next", Status = ContractVersionStatus.Draft },
                new CommandVersion { Id = Guid.NewGuid(), VersionNumber = "1.0.0", PayloadSchemaJson = "{}", ReplyPayloadSchemaJson = """{"type":"object"}""", Comment = "initial", Status = ContractVersionStatus.Approved }
            ]
        };
        var commandResponse = MapControlPlane<CommandDefinition, CommandDefinitionResponse>(command);
        Assert.Equal("customer.create", commandResponse.Topic);
        Assert.Equal("""{"type":"object"}""", commandResponse.Versions[0].ReplyDefinitionJson);
    }

    [Fact]
    public void ControlPlaneMapperMapsDistributionAndSecurityResponses()
    {
        var artifact = new ContractArtifact
        {
            Id = Guid.NewGuid(),
            ArtifactType = ContractArtifactType.Command,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Name = "Create Customer",
            Topic = "customer.create",
            VersionNumber = "1.0.0",
            Description = "Artifact",
            PayloadSchemaJson = "{}",
            ContentHash = "hash",
            SourceStatus = "Deployed"
        };
        Assert.Equal(ContractArtifactType.Command, MapControlPlane<ContractArtifact, ContractArtifactResponse>(artifact).ArtifactType);

        var environment = new RuntimeEnvironment
        {
            Id = Guid.NewGuid(),
            Name = "Development",
            Code = "dev",
            Description = "Dev",
            IsEnabled = true,
            UpdatedAtUtc = DateTime.UtcNow
        };
        Assert.Equal("dev", MapControlPlane<RuntimeEnvironment, RuntimeEnvironmentResponse>(environment).Code);

        var node = new RuntimeNode
        {
            Id = Guid.NewGuid(),
            Name = "Runtime",
            Code = "runtime",
            EnvironmentId = environment.Id,
            EnvironmentName = environment.Name,
            DistributionMode = DistributionMode.Hybrid,
            EndpointBaseUri = "https://runtime.example.test",
            EndpointApiPath = "/runtime",
            Status = RuntimeNodeStatus.Active,
            IsEnabled = true,
            Description = "Node",
            InboundCredentialStatus = ConnectionCredentialStatus.Active,
            OutboundCredentialStatus = ConnectionCredentialStatus.Disabled
        };
        var nodeResponse = MapControlPlane<RuntimeNode, RuntimeNodeResponse>(node);
        Assert.Equal(DistributionMode.Hybrid, nodeResponse.DistributionMode);
        Assert.Equal(ConnectionCredentialStatus.Disabled, nodeResponse.OutboundCredentialStatus);

        var target = new ContractReleaseTarget
        {
            Id = Guid.NewGuid(),
            RuntimeNodeId = node.Id,
            ArtifactId = artifact.Id,
            RolloutGroup = "Manual",
            Status = ContractReleaseTargetStatus.Delivered,
            ActivationStatus = ContractReleaseActivationStatus.Activated,
            CorrelationId = "corr"
        };
        var release = new ContractRelease
        {
            Id = Guid.NewGuid(),
            Name = "Release",
            Description = "Release",
            Status = ContractReleaseStatus.Completed,
            CompletedAtUtc = DateTime.UtcNow,
            Items = [new ContractReleaseItem { ArtifactId = artifact.Id }],
            Targets = [target]
        };
        var releaseResponse = MapControlPlane<ContractRelease, ContractReleaseResponse>(release);
        Assert.Equal([artifact.Id], releaseResponse.ArtifactIds.ToArray());
        Assert.Equal(target.Id, Assert.Single(releaseResponse.Targets).Id);

        var delivery = new RuntimeArtifactDeliveryResult
        {
            ReleaseTargetId = target.Id,
            Succeeded = true,
            Status = "Delivered",
            Message = "OK"
        };
        Assert.True(MapControlPlane<RuntimeArtifactDeliveryResult, DeliveryResultResponse>(delivery).Succeeded);

        Assert.Equal("user@example.test", MapControlPlane<KnOwlSubject, SecuritySubjectResponse>(new KnOwlSubject
        {
            Provider = "oidc",
            SubjectId = "user-1",
            DisplayName = "User",
            Email = "user@example.test"
        }).Email);
        Assert.Equal("Admin", MapControlPlane<KnOwlRoleAssignment, SecurityRoleAssignmentResponse>(new KnOwlRoleAssignment
        {
            Provider = "oidc",
            SubjectId = "user-1",
            Role = "Admin"
        }).Role);
        Assert.Equal("contracts.read", MapControlPlane<KnOwlPermissionAssignment, SecurityPermissionAssignmentResponse>(new KnOwlPermissionAssignment
        {
            Provider = "oidc",
            SubjectId = "user-1",
            Permission = "contracts.read"
        }).Permission);
        Assert.Equal("group-1", MapControlPlane<KnOwlExternalGroupRoleAssignment, SecurityExternalGroupRoleAssignmentResponse>(new KnOwlExternalGroupRoleAssignment
        {
            Provider = "oidc",
            ExternalGroupId = "group-1",
            Role = "Reader"
        }).ExternalGroupId);
    }

    [Fact]
    public void RuntimeAndDocumentationMappersMapResponses()
    {
        var runtimeArtifact = new RuntimeContractArtifact
        {
            Id = Guid.NewGuid(),
            SourceArtifactId = Guid.NewGuid(),
            SourceReleaseId = Guid.NewGuid(),
            ArtifactType = ContractArtifactType.Event,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Name = "Customer Created",
            Topic = "customer.created",
            VersionNumber = "1.0.0",
            Description = "Runtime artifact",
            PayloadSchemaJson = "{}",
            ContentHash = "hash"
        };
        var runtimeArtifactResponse = MapRuntime<RuntimeContractArtifact, RuntimeArtifactResponse>(runtimeArtifact);
        Assert.Equal(runtimeArtifact.SourceArtifactId, runtimeArtifactResponse.SourceArtifactId);

        var designNode = new RuntimeDesignNode
        {
            Id = Guid.NewGuid(),
            Key = "control-plane",
            Name = "Control Plane",
            DistributionMode = DistributionMode.Pull,
            EndpointBaseUri = "https://control-plane.example.test",
            RemoteRuntimeNodeId = "runtime-1",
            IsEnabled = true,
            Description = "Source",
            Status = RuntimeDesignNodeStatus.Enabled,
            InboundCredentialStatus = ConnectionCredentialStatus.Active,
            OutboundCredentialStatus = ConnectionCredentialStatus.Active
        };
        var designNodeResponse = MapRuntime<RuntimeDesignNode, RuntimeDesignNodeResponse>(designNode);
        Assert.Equal("Enabled", designNodeResponse.Status);
        Assert.Equal("runtime-1", designNodeResponse.RemoteRuntimeNodeId);

        var space = new DocumentationSpace { Id = Guid.NewGuid(), Key = "space", Name = "Space", Description = "Docs" };
        var topic = new DocumentationTopic { Id = Guid.NewGuid(), SpaceId = space.Id, Key = "topic", Name = "Topic", Description = "Topic docs" };
        var page = new DocumentationPage { Id = Guid.NewGuid(), TopicId = topic.Id, Key = "page", Title = "Page", Description = "Page docs" };
        var version = new DocumentationPageVersion
        {
            Id = Guid.NewGuid(),
            PageId = page.Id,
            VersionNumber = "1.0.0",
            EntryPath = "docs/index.md",
            Status = DocPageVersionStatus.Published,
            PublishedAtUtc = DateTime.UtcNow
        };

        Assert.Equal("space", MapDocumentation<DocumentationSpace, DocumentationSpaceResponse>(space).Key);
        Assert.Equal(space.Id, MapDocumentation<DocumentationTopic, DocumentationTopicResponse>(topic).SpaceId);
        Assert.Equal("Page", MapDocumentation<DocumentationPage, DocumentationPageResponse>(page).Title);
        Assert.Equal(DocPageVersionStatus.Published, MapDocumentation<DocumentationPageVersion, DocumentationPageVersionResponse>(version).Status);
    }

    private static TResponse MapControlPlane<TSource, TResponse>(TSource source)
        => InvokeMapper<TSource, TResponse>(
            typeof(KnOwlControlPlaneApiEndpointRouteBuilderExtensions).Assembly,
            "KnOwl.ControlPlane.Api.Mapping.ControlPlaneApiMapper",
            source);

    private static TResponse MapRuntime<TSource, TResponse>(TSource source)
        => InvokeMapper<TSource, TResponse>(
            typeof(KnOwlRuntimeApiEndpointRouteBuilderExtensions).Assembly,
            "KnOwl.Runtime.Api.Mapping.RuntimeApiMapper",
            source);

    private static TResponse MapDocumentation<TSource, TResponse>(TSource source)
        => InvokeMapper<TSource, TResponse>(
            typeof(KnOwlDocumentationEndpointRouteBuilderExtensions).Assembly,
            "KnOwl.Documentation.Api.DocumentationApiMapper",
            source);

    private static TResponse InvokeMapper<TSource, TResponse>(Assembly assembly, string typeName, TSource source)
    {
        var mapper = assembly.GetType(typeName, throwOnError: true)!;
        var method = mapper.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(x => x.Name == "ToResponse" && x.GetParameters().Single().ParameterType == typeof(TSource));
        return Assert.IsType<TResponse>(method.Invoke(null, [source]));
    }
}
