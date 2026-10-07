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
    public void ControlPlaneMapperMapsSchemaTypeIdentity()
    {
        var schemaType = CreateSchemaType();

        var response = MapControlPlane<SchemaTypeDefinition, SchemaTypeResponse>(schemaType);

        Assert.Equal(schemaType.Id, response.Id);
        Assert.Equal("customer", response.Key);
        Assert.Equal("Customer", response.Name);
        Assert.Equal("Customer type", response.Description);
        Assert.False(response.IsSystem);
        Assert.True(response.IsActive);
    }

    [Fact]
    public void ControlPlaneMapperOrdersSchemaTypeVersions()
    {
        var schemaType = CreateSchemaType();

        var response = MapControlPlane<SchemaTypeDefinition, SchemaTypeResponse>(schemaType);

        Assert.Equal(["1.0.0", "2.0.0"], response.Versions.Select(x => x.VersionNumber).ToArray());
    }

    [Fact]
    public void ControlPlaneMapperMapsSchemaTypeVersion()
    {
        var version = new SchemaTypeVersion { Id = Guid.NewGuid(), VersionNumber = "1.0.0", DefinitionJson = """{"type":"object"}""", Comment = "initial", IsActive = false };

        var response = MapControlPlane<SchemaTypeVersion, SchemaTypeVersionResponse>(version);

        Assert.Equal(version.Id, response.Id);
        Assert.Equal("""{"type":"object"}""", response.DefinitionJson);
        Assert.False(response.IsActive);
    }

    [Fact]
    public void ControlPlaneMapperMapsMetadataFieldIdentity()
    {
        var metadata = CreateMetadataField();

        var response = MapControlPlane<ContractFieldMetadataDefinition, MetadataFieldResponse>(metadata);

        Assert.Equal(metadata.Id, response.Id);
        Assert.Equal("trace-id", response.Key);
        Assert.Equal("Trace Id", response.Name);
        Assert.True(response.IsActive);
    }

    [Fact]
    public void ControlPlaneMapperOrdersMetadataFieldVersions()
    {
        var metadata = CreateMetadataField();

        var response = MapControlPlane<ContractFieldMetadataDefinition, MetadataFieldResponse>(metadata);

        Assert.Equal(["1.0.0", "1.1.0"], response.Versions.Select(x => x.VersionNumber).ToArray());
    }

    [Fact]
    public void ControlPlaneMapperMapsMetadataFieldVersion()
    {
        var version = new ContractFieldMetadataVersion { Id = Guid.NewGuid(), VersionNumber = "1.0.0", DefinitionJson = "{}", Comment = "initial", IsActive = false };

        var response = MapControlPlane<ContractFieldMetadataVersion, MetadataFieldVersionResponse>(version);

        Assert.Equal(version.Id, response.Id);
        Assert.Equal("{}", response.DefinitionJson);
        Assert.False(response.IsActive);
    }

    [Fact]
    public void ControlPlaneMapperMapsEventIdentity()
    {
        var @event = CreateEvent();

        var response = MapControlPlane<EventDefinition, EventDefinitionResponse>(@event);

        Assert.Equal(@event.Id, response.Id);
        Assert.Equal("Customer Created", response.Name);
        Assert.Equal("customer.created", response.Topic);
    }

    [Fact]
    public void ControlPlaneMapperOrdersEventVersions()
    {
        var @event = CreateEvent();

        var response = MapControlPlane<EventDefinition, EventDefinitionResponse>(@event);

        Assert.Equal(["1.0.0", "1.0.1"], response.Versions.Select(x => x.VersionNumber).ToArray());
    }

    [Fact]
    public void ControlPlaneMapperMapsEventVersionStatus()
    {
        var version = new EventVersion { Id = Guid.NewGuid(), VersionNumber = "1.0.0", PayloadSchemaJson = "{}", Comment = "initial", Status = ContractVersionStatus.Deployed };

        var response = MapControlPlane<EventVersion, EventVersionResponse>(version);

        Assert.Equal(version.Id, response.Id);
        Assert.Equal(ContractVersionStatus.Deployed, response.Status);
    }

    [Fact]
    public void ControlPlaneMapperMapsCommandIdentity()
    {
        var command = CreateCommand();

        var response = MapControlPlane<CommandDefinition, CommandDefinitionResponse>(command);

        Assert.Equal(command.Id, response.Id);
        Assert.Equal("Create Customer", response.Name);
        Assert.Equal("customer.create", response.Topic);
    }

    [Fact]
    public void ControlPlaneMapperOrdersCommandVersions()
    {
        var command = CreateCommand();

        var response = MapControlPlane<CommandDefinition, CommandDefinitionResponse>(command);

        Assert.Equal(["1.0.0", "2.0.0"], response.Versions.Select(x => x.VersionNumber).ToArray());
    }

    [Fact]
    public void ControlPlaneMapperMapsCommandVersionReplySchema()
    {
        var version = new CommandVersion { Id = Guid.NewGuid(), VersionNumber = "1.0.0", PayloadSchemaJson = "{}", ReplyPayloadSchemaJson = """{"type":"object"}""", Comment = "initial", Status = ContractVersionStatus.Approved };

        var response = MapControlPlane<CommandVersion, CommandVersionResponse>(version);

        Assert.Equal(version.Id, response.Id);
        Assert.Equal("""{"type":"object"}""", response.ReplyDefinitionJson);
    }

    [Fact]
    public void ControlPlaneMapperMapsContractArtifactIdentity()
    {
        var artifact = CreateArtifact();

        var response = MapControlPlane<ContractArtifact, ContractArtifactResponse>(artifact);

        Assert.Equal(artifact.Id, response.Id);
        Assert.Equal(ContractArtifactType.Command, response.ArtifactType);
        Assert.Equal("customer.create", response.Topic);
    }

    [Fact]
    public void ControlPlaneMapperMapsRuntimeEnvironmentState()
    {
        var environment = CreateRuntimeEnvironment();

        var response = MapControlPlane<RuntimeEnvironment, RuntimeEnvironmentResponse>(environment);

        Assert.Equal(environment.Id, response.Id);
        Assert.Equal("dev", response.Code);
        Assert.True(response.IsEnabled);
    }

    [Fact]
    public void ControlPlaneMapperMapsRuntimeNodeDistributionAndCredentials()
    {
        var environment = CreateRuntimeEnvironment();
        var node = CreateRuntimeNode(environment);

        var response = MapControlPlane<RuntimeNode, RuntimeNodeResponse>(node);

        Assert.Equal(DistributionMode.Hybrid, response.DistributionMode);
        Assert.Equal(ConnectionCredentialStatus.Active, response.InboundCredentialStatus);
        Assert.Equal(ConnectionCredentialStatus.Disabled, response.OutboundCredentialStatus);
    }

    [Fact]
    public void ControlPlaneMapperMapsReleaseArtifactIds()
    {
        var artifact = CreateArtifact();
        var release = CreateRelease(artifact, CreateRuntimeNode(CreateRuntimeEnvironment()));

        var response = MapControlPlane<ContractRelease, ContractReleaseResponse>(release);

        Assert.Equal([artifact.Id], response.ArtifactIds.ToArray());
    }

    [Fact]
    public void ControlPlaneMapperMapsReleaseTargets()
    {
        var artifact = CreateArtifact();
        var release = CreateRelease(artifact, CreateRuntimeNode(CreateRuntimeEnvironment()));

        var response = MapControlPlane<ContractRelease, ContractReleaseResponse>(release);

        Assert.Equal(release.Targets.Single().Id, response.Targets.Single().Id);
    }

    [Fact]
    public void ControlPlaneMapperMapsReleaseTargetStatus()
    {
        var target = CreateReleaseTarget(CreateArtifact(), CreateRuntimeNode(CreateRuntimeEnvironment()));

        var response = MapControlPlane<ContractReleaseTarget, ReleaseTargetResponse>(target);

        Assert.Equal(target.Id, response.Id);
        Assert.Equal(ContractReleaseTargetStatus.Delivered, response.Status);
        Assert.Equal(ContractReleaseActivationStatus.Activated, response.ActivationStatus);
    }

    [Fact]
    public void ControlPlaneMapperMapsDeliveryResult()
    {
        var delivery = new RuntimeArtifactDeliveryResult { ReleaseTargetId = Guid.NewGuid(), Succeeded = true, Status = "Delivered", Message = "OK" };

        var response = MapControlPlane<RuntimeArtifactDeliveryResult, DeliveryResultResponse>(delivery);

        Assert.True(response.Succeeded);
        Assert.Equal("Delivered", response.Status);
    }

    [Fact]
    public void ControlPlaneMapperMapsSecuritySubject()
    {
        var subject = new KnOwlSubject { Provider = "oidc", SubjectId = "user-1", DisplayName = "User", Email = "user@example.test" };

        var response = MapControlPlane<KnOwlSubject, SecuritySubjectResponse>(subject);

        Assert.Equal("user@example.test", response.Email);
    }

    [Fact]
    public void ControlPlaneMapperMapsSecurityRoleAssignment()
    {
        var assignment = new KnOwlRoleAssignment { Provider = "oidc", SubjectId = "user-1", Role = "Admin" };

        var response = MapControlPlane<KnOwlRoleAssignment, SecurityRoleAssignmentResponse>(assignment);

        Assert.Equal("Admin", response.Role);
    }

    [Fact]
    public void ControlPlaneMapperMapsSecurityPermissionAssignment()
    {
        var assignment = new KnOwlPermissionAssignment { Provider = "oidc", SubjectId = "user-1", Permission = "contracts.read" };

        var response = MapControlPlane<KnOwlPermissionAssignment, SecurityPermissionAssignmentResponse>(assignment);

        Assert.Equal("contracts.read", response.Permission);
    }

    [Fact]
    public void ControlPlaneMapperMapsSecurityExternalGroupRoleAssignment()
    {
        var assignment = new KnOwlExternalGroupRoleAssignment { Provider = "oidc", ExternalGroupId = "group-1", Role = "Reader" };

        var response = MapControlPlane<KnOwlExternalGroupRoleAssignment, SecurityExternalGroupRoleAssignmentResponse>(assignment);

        Assert.Equal("group-1", response.ExternalGroupId);
    }

    [Fact]
    public void RuntimeMapperMapsArtifactIdentity()
    {
        var artifact = CreateRuntimeArtifact();

        var response = MapRuntime<RuntimeContractArtifact, RuntimeArtifactResponse>(artifact);

        Assert.Equal(artifact.SourceArtifactId, response.SourceArtifactId);
        Assert.Equal("customer.created", response.Topic);
    }

    [Fact]
    public void RuntimeMapperMapsDesignNodeConnectionState()
    {
        var designNode = CreateRuntimeDesignNode();

        var response = MapRuntime<RuntimeDesignNode, RuntimeDesignNodeResponse>(designNode);

        Assert.Equal("Enabled", response.Status);
        Assert.Equal("runtime-1", response.RemoteRuntimeNodeId);
    }

    [Fact]
    public void DocumentationMapperMapsSpaceIdentity()
    {
        var space = new DocumentationSpace { Id = Guid.NewGuid(), Key = "space", Name = "Space", Description = "Docs" };

        var response = MapDocumentation<DocumentationSpace, DocumentationSpaceResponse>(space);

        Assert.Equal("space", response.Key);
    }

    [Fact]
    public void DocumentationMapperMapsTopicParent()
    {
        var topic = new DocumentationTopic { Id = Guid.NewGuid(), SpaceId = Guid.NewGuid(), Key = "topic", Name = "Topic", Description = "Topic docs" };

        var response = MapDocumentation<DocumentationTopic, DocumentationTopicResponse>(topic);

        Assert.Equal(topic.SpaceId, response.SpaceId);
    }

    [Fact]
    public void DocumentationMapperMapsPageTitle()
    {
        var page = new DocumentationPage { Id = Guid.NewGuid(), TopicId = Guid.NewGuid(), Key = "page", Title = "Page", Description = "Page docs" };

        var response = MapDocumentation<DocumentationPage, DocumentationPageResponse>(page);

        Assert.Equal("Page", response.Title);
    }

    [Fact]
    public void DocumentationMapperMapsVersionPublication()
    {
        var version = new DocumentationPageVersion
        {
            Id = Guid.NewGuid(),
            PageId = Guid.NewGuid(),
            VersionNumber = "1.0.0",
            EntryPath = "docs/index.md",
            Status = DocPageVersionStatus.Published,
            PublishedAtUtc = DateTime.UtcNow
        };

        var response = MapDocumentation<DocumentationPageVersion, DocumentationPageVersionResponse>(version);

        Assert.Equal(DocPageVersionStatus.Published, response.Status);
        Assert.Equal("docs/index.md", response.EntryPath);
    }

    private static SchemaTypeDefinition CreateSchemaType()
        => new()
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

    private static ContractFieldMetadataDefinition CreateMetadataField()
        => new()
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

    private static EventDefinition CreateEvent()
        => new()
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

    private static CommandDefinition CreateCommand()
        => new()
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

    private static ContractArtifact CreateArtifact()
        => new()
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

    private static RuntimeEnvironment CreateRuntimeEnvironment()
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "Development",
            Code = "dev",
            Description = "Dev",
            IsEnabled = true,
            UpdatedAtUtc = DateTime.UtcNow
        };

    private static RuntimeNode CreateRuntimeNode(RuntimeEnvironment environment)
        => new()
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

    private static ContractReleaseTarget CreateReleaseTarget(ContractArtifact artifact, RuntimeNode node)
        => new()
        {
            Id = Guid.NewGuid(),
            RuntimeNodeId = node.Id,
            ArtifactId = artifact.Id,
            RolloutGroup = "Manual",
            Status = ContractReleaseTargetStatus.Delivered,
            ActivationStatus = ContractReleaseActivationStatus.Activated,
            CorrelationId = "corr"
        };

    private static ContractRelease CreateRelease(ContractArtifact artifact, RuntimeNode node)
    {
        var target = CreateReleaseTarget(artifact, node);
        return new ContractRelease
        {
            Id = Guid.NewGuid(),
            Name = "Release",
            Description = "Release",
            Status = ContractReleaseStatus.Completed,
            CompletedAtUtc = DateTime.UtcNow,
            Items = [new ContractReleaseItem { ArtifactId = artifact.Id }],
            Targets = [target]
        };
    }

    private static RuntimeContractArtifact CreateRuntimeArtifact()
        => new()
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

    private static RuntimeDesignNode CreateRuntimeDesignNode()
        => new()
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
