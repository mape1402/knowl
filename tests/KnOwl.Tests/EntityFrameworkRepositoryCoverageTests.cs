using System.Text;
using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using KnOwl.ControlPlane.Storage.EntityFramework.Distribution.Repositories;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Repositories;
using KnOwl.Documentation;
using KnOwl.Documentation.Storage.EntityFramework.Data;
using KnOwl.Documentation.Storage.EntityFramework.Storage;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Distribution;
using KnOwl.Runtime.Storage.EntityFramework.Data;
using KnOwl.Runtime.Storage.EntityFramework.Repositories;
using KnOwl.Security.Authorization;
using KnOwl.Security.Storage;
using KnOwl.Security.Storage.EntityFramework.Data;
using KnOwl.Security.Storage.EntityFramework.Storage;
using KnOwl.Security.Subjects;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.Tests;

public sealed class EntityFrameworkRepositoryCoverageTests
{
    [Fact]
    public async Task ControlPlaneDesignRepositoriesPersistHydrateAndUpdateVersionState()
    {
        await using var db = CreateControlPlaneContext();
        var eventRepository = new EventRepository(db);
        var commandRepository = new CommandRepository(db);
        var schemaRepository = new SchemaTypeRepository(db);
        var metadataRepository = new ContractFieldMetadataRepository(db);
        var now = DateTime.UtcNow;

        var eventDefinition = new EventDefinition { Name = "Customer Created", Topic = "customer.created", Description = "Created" };
        await eventRepository.Create(eventDefinition);
        var newEventVersion = new EventVersion { VersionNumber = "1.0.0", PayloadSchemaJson = "{}", Status = ContractVersionStatus.Draft };
        await eventRepository.AddVersion(eventDefinition.Id, newEventVersion);
        db.ChangeTracker.Clear();
        await eventRepository.UpdateDefinition(eventDefinition.Id, "Customer Updated", "customer.updated", null, now);
        var eventVersion = Assert.Single((await eventRepository.GetById(eventDefinition.Id, includeVersions: true))!.Versions);
        db.ChangeTracker.Clear();
        await eventRepository.UpdateDraftVersion(eventVersion.Id, """{"type":"object"}""", "Draft", now);
        await eventRepository.UpdateVersionStatus(eventVersion.Id, ContractVersionStatus.InReview, now);
        await eventRepository.UpdateVersionStatus(eventVersion.Id, ContractVersionStatus.Approved, now);
        await eventRepository.UpdateVersionStatus(eventVersion.Id, ContractVersionStatus.Deployed, now);
        await eventRepository.UpdateVersionStatus(eventVersion.Id, ContractVersionStatus.Deprecated, now);
        await eventRepository.UpdateVersionStatus(eventVersion.Id, ContractVersionStatus.Archived, now);
        await eventRepository.Delete(eventDefinition.Id);

        var command = new CommandDefinition { Name = "Create Customer", Topic = "customer.create", Description = "Create" };
        await commandRepository.Create(command);
        var newCommandVersion = new CommandVersion { VersionNumber = "1.0.0", PayloadSchemaJson = "{}", Status = ContractVersionStatus.Draft };
        await commandRepository.AddVersion(command.Id, newCommandVersion);
        db.ChangeTracker.Clear();
        await commandRepository.UpdateDefinition(command.Id, "Create Customer V2", "customer.create.v2", "Updated", now);
        var commandVersion = Assert.Single((await commandRepository.GetById(command.Id, includeVersions: true))!.Versions);
        db.ChangeTracker.Clear();
        await commandRepository.UpdateDraftVersion(commandVersion.Id, """{"request":true}""", """{"reply":true}""", "Draft", now);
        await commandRepository.UpdateVersionStatus(commandVersion.Id, ContractVersionStatus.InReview, now);
        await commandRepository.UpdateVersionStatus(commandVersion.Id, ContractVersionStatus.Approved, now);
        await commandRepository.UpdateVersionStatus(commandVersion.Id, ContractVersionStatus.Deployed, now);
        await commandRepository.UpdateVersionStatus(commandVersion.Id, ContractVersionStatus.Deprecated, now);
        await commandRepository.UpdateVersionStatus(commandVersion.Id, ContractVersionStatus.Archived, now);
        Assert.NotEmpty(await commandRepository.GetAllWithVersions());
        await commandRepository.Delete(command.Id);

        var schema = new SchemaTypeDefinition { Key = "customer-ref", Name = "Customer Ref", Description = "Ref", IsActive = true };
        await schemaRepository.Create(schema);
        var newSchemaVersion = new SchemaTypeVersion { VersionNumber = "1.0.0", DefinitionJson = "{}", IsActive = true };
        await schemaRepository.AddVersion(schema.Id, newSchemaVersion);
        db.ChangeTracker.Clear();
        var activeSchemaVersion = Assert.Single(await schemaRepository.GetActiveVersionsWithDefinitions());
        Assert.Equal(schema.Id, activeSchemaVersion.SchemaTypeDefinitionId);
        Assert.NotNull(activeSchemaVersion.SchemaTypeDefinition);
        Assert.NotNull(await schemaRepository.GetVersionById(activeSchemaVersion.Id));
        db.ChangeTracker.Clear();
        await schemaRepository.UpdateDefinition(schema.Id, "customer-ref", "Customer Reference", null, false, now);
        var schemaVersion = Assert.Single((await schemaRepository.GetById(schema.Id, includeVersions: true))!.Versions);
        db.ChangeTracker.Clear();
        await schemaRepository.SetVersionActive(schema.Id, schemaVersion.Id, false, now);

        var metadata = new ContractFieldMetadataDefinition { Key = "trace-id", Name = "Trace Id", Description = "Trace", IsActive = true };
        await metadataRepository.Create(metadata);
        var newMetadataVersion = new ContractFieldMetadataVersion { VersionNumber = "1.0.0", DefinitionJson = "{}", IsActive = true };
        await metadataRepository.UpsertVersion(metadata.Id, newMetadataVersion);
        db.ChangeTracker.Clear();
        await metadataRepository.UpdateDefinition(metadata.Id, "trace-id", "Trace", null, false, now);
        var metadataVersion = Assert.Single((await metadataRepository.GetById(metadata.Id, includeVersions: true))!.Versions);
        db.ChangeTracker.Clear();
        await metadataRepository.SetVersionActive(metadata.Id, metadataVersion.Id, false, now);

        Assert.NotEmpty(await eventRepository.GetAllWithVersions());
        Assert.NotNull(await eventRepository.GetVersionById(eventVersion.Id));
        Assert.True(await eventRepository.VersionExists(eventDefinition.Id, "1.0.0"));
        Assert.NotNull(await commandRepository.GetVersionById(commandVersion.Id));
        Assert.True(await commandRepository.VersionExists(command.Id, "1.0.0"));
        Assert.NotEmpty(await schemaRepository.GetAllWithVersions());
        Assert.Empty(await schemaRepository.GetActiveVersionsWithDefinitions());
        Assert.True(await schemaRepository.KeyExists("customer-ref"));
        Assert.False(await schemaRepository.KeyExists("customer-ref", schema.Id));
        Assert.True(await schemaRepository.VersionExists(schema.Id, "1.0.0"));
        Assert.NotEmpty(await metadataRepository.GetAllWithVersions());
        Assert.Empty(await metadataRepository.GetActiveWithVersions());
        Assert.True(await metadataRepository.KeyExists("trace-id"));
        Assert.False(await metadataRepository.KeyExists("trace-id", metadata.Id));
        Assert.True(await metadataRepository.VersionExists(metadata.Id, "1.0.0"));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => eventRepository.UpdateDefinition(Guid.NewGuid(), "x", "x", null, now));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => commandRepository.AddVersion(Guid.NewGuid(), new CommandVersion { VersionNumber = "9.9.9", PayloadSchemaJson = "{}" }));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => schemaRepository.UpdateDefinition(Guid.NewGuid(), "x", "x", null, true, now));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => schemaRepository.SetVersionActive(schema.Id, Guid.NewGuid(), true, now));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => schemaRepository.SetVersionActive(Guid.NewGuid(), Guid.NewGuid(), true, now));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => metadataRepository.SetVersionActive(Guid.NewGuid(), Guid.NewGuid(), true, now));
    }

    [Fact]
    public async Task ControlPlaneDistributionRepositoriesHydrateAndUpdateReleaseGraph()
    {
        await using var db = CreateControlPlaneContext();
        var artifactRepository = new ContractArtifactRepository(db);
        var environmentRepository = new RuntimeEnvironmentRepository(db);
        var nodeRepository = new RuntimeNodeRepository(db);
        var releaseRepository = new ContractReleaseRepository(db);
        var targetRepository = new ContractReleaseTargetRepository(db);
        var now = DateTime.UtcNow;

        var artifact = CreateArtifact("customer.created", ContractVersionStatus.Deployed.ToString());
        await artifactRepository.Create(artifact);
        var environment = new RuntimeEnvironment { Name = "Development", Code = "dev", IsEnabled = true };
        await environmentRepository.Create(environment);
        var node = CreateRuntimeNode(environment);
        await nodeRepository.Create(node);
        var release = new ContractRelease { Name = "Release 1", Description = "Release", CreatedAtUtc = now.AddMinutes(-5) };
        var item = new ContractReleaseItem { Id = Guid.NewGuid(), ReleaseId = release.Id, ArtifactId = artifact.Id, CreatedAtUtc = now };
        var target = new ContractReleaseTarget
        {
            Id = Guid.NewGuid(),
            ReleaseId = release.Id,
            ReleaseItemId = item.Id,
            RuntimeNodeId = node.Id,
            ArtifactId = artifact.Id,
            RolloutGroup = "default",
            Status = ContractReleaseTargetStatus.AvailableForPull,
            ActivationStatus = ContractReleaseActivationStatus.NotActivated,
            AssignedAtUtc = now,
            CorrelationId = "corr"
        };
        release.Items.Add(item);
        release.Targets.Add(target);
        await releaseRepository.Create(release);
        await targetRepository.AddAttempt(new ContractReleaseAttempt { ReleaseTargetId = target.Id, Action = "Pull", InitiatedBy = "runtime", Succeeded = true, StartedAtUtc = now });
        await releaseRepository.UpdateStatus(release.Id, ContractReleaseStatus.Deployed, now);
        await releaseRepository.UpdateStatus(release.Id, ContractReleaseStatus.Completed, now);
        await releaseRepository.UpdateStatus(release.Id, ContractReleaseStatus.Failed, now);
        await releaseRepository.UpdateStatus(release.Id, ContractReleaseStatus.Canceled, now);

        var hydratedRelease = await releaseRepository.GetById(release.Id, includeItems: true, includeTargets: true);
        var hydratedTarget = Assert.Single(hydratedRelease!.Targets);
        var targetList = await targetRepository.GetByRelease(release.Id);
        var pending = await targetRepository.GetPendingForRuntimeNode(node.Id);
        var includedTarget = await targetRepository.GetById(target.Id, includeArtifact: true);

        includedTarget!.Status = ContractReleaseTargetStatus.Delivered;
        includedTarget.RuntimeVersionApplied = "runtime-1.0.0";
        await targetRepository.Update(includedTarget);
        await targetRepository.CreateMany([new ContractReleaseTarget
        {
            Id = Guid.NewGuid(),
            ReleaseId = release.Id,
            ReleaseItemId = item.Id,
            RuntimeNodeId = node.Id,
            ArtifactId = artifact.Id,
            RolloutGroup = "manual",
            Status = ContractReleaseTargetStatus.PushScheduled,
            CorrelationId = "corr-2"
        }]);
        db.ChangeTracker.Clear();
        var storedNode = await nodeRepository.GetById(node.Id);
        var allNodesBeforeDelete = await nodeRepository.GetAll();
        var activeNodesBeforeDelete = await nodeRepository.GetActiveEnabled();
        storedNode!.Description = "Updated runtime node";
        await nodeRepository.Update(storedNode);
        await nodeRepository.SetIsEnabled(node.Id, false, now);
        await nodeRepository.Delete(node.Id, now);
        db.ChangeTracker.Clear();
        await environmentRepository.Update(new RuntimeEnvironment
        {
            Id = environment.Id,
            Name = "QA",
            Code = environment.Code,
            IsEnabled = false,
            CreatedAtUtc = environment.CreatedAtUtc
        });

        Assert.Equal(artifact.Topic, hydratedRelease.Items.Single().Artifact!.Topic);
        Assert.Equal(environment.Id, hydratedTarget.RuntimeNode!.Environment!.Id);
        Assert.NotEmpty(hydratedTarget.Attempts);
        Assert.Single(targetList);
        Assert.Single(pending);
        Assert.NotNull(includedTarget.Artifact);
        Assert.NotEmpty(await releaseRepository.GetAll());
        Assert.NotEmpty(await artifactRepository.GetAll());
        Assert.NotEmpty(await artifactRepository.GetDeployed());
        Assert.NotNull(await artifactRepository.GetById(artifact.Id));
        Assert.Single(await artifactRepository.GetByIds([artifact.Id]));
        Assert.NotNull(await artifactRepository.GetBySourceVersion(artifact.ArtifactType, artifact.VersionId));
        Assert.NotNull(await artifactRepository.GetByIdentity(artifact.ArtifactType, artifact.Topic, artifact.VersionNumber));
        Assert.NotNull(await artifactRepository.GetDeployedByIdentity(artifact.ArtifactType, artifact.Topic, artifact.VersionNumber));
        Assert.NotNull(await artifactRepository.GetLatestDeployed(artifact.ArtifactType, artifact.Topic));
        Assert.NotEmpty(await environmentRepository.GetAll());
        Assert.Empty(await environmentRepository.GetEnabled());
        Assert.NotNull(await environmentRepository.GetById(environment.Id));
        Assert.Single(allNodesBeforeDelete);
        Assert.Equal(environment.Id, allNodesBeforeDelete.Single().Environment?.Id);
        Assert.Single(activeNodesBeforeDelete);
        Assert.Equal(environment.Id, activeNodesBeforeDelete.Single().Environment?.Id);
        Assert.Empty(await nodeRepository.GetActiveEnabled());
        Assert.NotNull(await nodeRepository.GetByCode(" runtime "));
        Assert.NotNull(await nodeRepository.GetByInboundClientId(" inbound-client "));
        Assert.Empty(await nodeRepository.GetAll());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => releaseRepository.UpdateStatus(Guid.NewGuid(), ContractReleaseStatus.Deployed, now));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => nodeRepository.SetIsEnabled(Guid.NewGuid(), true, now));
    }

    [Fact]
    public async Task DocumentationStorageRepositoriesPersistMetadataAndContent()
    {
        await using var db = CreateDocumentationContext();
        var repository = new DocumentationRepository(db);
        var content = new DatabaseDocumentationContentStore(db);

        var space = await repository.UpsertSpace(new DocumentationSpace { Key = "knowl", Name = "KnOwl", Description = "Docs" });
        await repository.UpsertSpace(new DocumentationSpace { Key = "knowl", Name = "KnOwl Updated", Description = "Docs updated", IsActive = false });
        var topic = await repository.UpsertTopic(new DocumentationTopic { SpaceId = space.Id, Key = "guides", Name = "Guides", Description = "Guides" });
        await repository.UpsertTopic(new DocumentationTopic { SpaceId = space.Id, Key = "guides", Name = "Guides Updated", Description = "Updated" });
        var page = await repository.UpsertPage(new DocumentationPage { TopicId = topic.Id, Key = "intro", Title = "Intro", Description = "Intro" });
        await repository.UpsertPage(new DocumentationPage { TopicId = topic.Id, Key = "intro", Title = "Intro Updated", Description = "Updated" });
        var version = await repository.AddVersion(new DocumentationPageVersion
        {
            PageId = page.Id,
            VersionNumber = "1.0.0",
            EntryPath = "docs/index.md",
            ContentHash = "hash",
            Status = DocPageVersionStatus.Published,
            PublishedAtUtc = DateTime.UtcNow
        });
        var asset = await repository.AddAsset(new DocumentationAsset
        {
            PageVersionId = version.Id,
            LogicalPath = "docs/index.md",
            FileName = "index.md",
            ContentType = "text/markdown",
            ContentHash = "hash",
            StorageKey = "asset"
        });
        version.Status = DocPageVersionStatus.Archived;
        await repository.UpdateVersion(version);

        await content.Save("asset", new MemoryStream("# Title"u8.ToArray()), "text/markdown");
        await content.Save("asset", new MemoryStream("Updated"u8.ToArray()), "text/plain");
        await using var opened = await content.Open("asset");
        using var reader = new StreamReader(opened, Encoding.UTF8);
        var openedText = await reader.ReadToEndAsync();
        await content.Delete("asset");

        Assert.NotEmpty(await repository.GetSpaces());
        Assert.NotNull(await repository.GetSpace(space.Id));
        Assert.NotNull(await repository.GetSpaceByKey("knowl"));
        Assert.NotEmpty(await repository.GetTopics(space.Id));
        Assert.NotNull(await repository.GetTopic(topic.Id));
        Assert.NotNull(await repository.GetTopicByKey(space.Id, "guides"));
        Assert.NotEmpty(await repository.GetPages(topic.Id));
        Assert.NotNull(await repository.GetPage(page.Id, includeVersions: true));
        Assert.NotNull(await repository.GetPageByPath("knowl", "guides", "intro", includeVersions: true));
        Assert.Null(await repository.GetPageByPath("missing", "guides", "intro"));
        Assert.Null(await repository.GetPageByPath("knowl", "missing", "intro"));
        Assert.NotNull(await repository.GetVersion(version.Id, includeAssets: true));
        Assert.NotNull(await repository.GetVersionByPath("knowl", "guides", "intro", "1.0.0", includeAssets: true));
        Assert.Null(await repository.GetVersionByPath("knowl", "guides", "missing", "1.0.0"));
        Assert.Null(await repository.GetLatestPublishedVersion("knowl", "guides", "intro"));
        Assert.NotNull(await repository.GetAsset(asset.Id));
        Assert.NotNull(await repository.GetAssetByLogicalPath(version.Id, "docs/index.md"));
        Assert.Equal("Updated", openedText);
        Assert.False(await content.Exists("asset"));
        await Assert.ThrowsAsync<FileNotFoundException>(() => content.Open("missing"));
    }

    [Fact]
    public async Task SecurityStorePersistsSubjectsAssignmentsAndBootstrapAdmin()
    {
        await using var db = CreateSecurityContext();
        var store = new KnOwlSecurityStore(db);
        var subject = new KnOwlSubject { Provider = "oidc", SubjectId = "user-1", DisplayName = "User", Email = "user@example.test", IsEnabled = true };

        await store.UpsertSubject(subject);
        await store.UpsertSubject(new KnOwlSubject { Provider = "oidc", SubjectId = "user-1", DisplayName = "User Updated", Email = "updated@example.test", IsEnabled = true });
        await store.AssignRole(new KnOwlRoleAssignment { Provider = "oidc", SubjectId = "user-1", Role = "Reader", IsEnabled = true });
        await store.AssignPermission(new KnOwlPermissionAssignment { Provider = "oidc", SubjectId = "user-1", Permission = "contracts.read", IsEnabled = true });
        await store.AssignExternalGroupRole(new KnOwlExternalGroupRoleAssignment { Provider = "oidc", ExternalGroupId = "group-1", Role = "Admin", IsEnabled = true });
        await store.SynchronizeBootstrapAdmin(new KnOwlExternalSubject("oidc", "admin", "Admin", "admin@example.test", ["group-1"]));
        await store.SynchronizeBootstrapAdmin(new KnOwlExternalSubject("oidc", "admin", "Admin", "admin@example.test", ["group-1"]));

        Assert.NotEmpty(await store.GetSubjects());
        Assert.Equal("updated@example.test", (await store.GetSubject("oidc", "user-1"))!.Email);
        Assert.NotEmpty(await store.GetRoleAssignments());
        Assert.Single(await store.GetRoleAssignments("oidc", "user-1"));
        Assert.NotEmpty(await store.GetPermissionAssignments());
        Assert.Single(await store.GetPermissionAssignments("oidc", "user-1"));
        Assert.NotEmpty(await store.GetExternalGroupRoleAssignments());
        Assert.Single(await store.GetExternalGroupRoleAssignments("oidc", ["group-1"]));
        Assert.Empty(await store.GetExternalGroupRoleAssignments("oidc", []));
        Assert.True(await store.HasEnabledAdmin());
    }

    [Fact]
    public async Task RuntimeStorageRepositoriesPersistArtifactsAndDesignNodes()
    {
        await using var db = CreateRuntimeContext();
        var artifactRepository = new RuntimeContractArtifactRepository(db);
        var designNodeRepository = new RuntimeDesignNodeRepository(db);
        var artifact = CreateRuntimeArtifact("customer.created");
        var olderArtifact = CreateRuntimeArtifact("customer.created", DateTime.UtcNow.AddDays(-1), "0.9.0");
        var designNode = new RuntimeDesignNode
        {
            Key = "control-plane",
            Name = "Control Plane",
            EndpointBaseUri = "https://control.example.test",
            RemoteRuntimeNodeId = "runtime-1",
            DistributionMode = DistributionMode.Hybrid,
            IsEnabled = true,
            Status = RuntimeDesignNodeStatus.Enabled,
            InboundClientId = "inbound-client"
        };

        await artifactRepository.Create(olderArtifact);
        await artifactRepository.Create(artifact);
        await designNodeRepository.Upsert(designNode);
        designNode.Name = "Control Plane Updated";
        await designNodeRepository.Upsert(designNode);

        Assert.Equal(2, (await artifactRepository.GetAll()).Count);
        Assert.Equal(artifact.Id, (await artifactRepository.GetByIdentity(artifact.ArtifactType, artifact.Topic, artifact.VersionNumber))!.Id);
        Assert.Equal(artifact.Id, (await artifactRepository.GetLatest(artifact.ArtifactType, artifact.Topic))!.Id);
        Assert.Single(await designNodeRepository.GetAll());
        Assert.NotNull(await designNodeRepository.GetById(designNode.Id));
        Assert.NotNull(await designNodeRepository.GetByKey(" control-plane "));
        Assert.NotNull(await designNodeRepository.GetByInboundClientId(" inbound-client "));
    }

    private static KnOwlDbContext CreateControlPlaneContext()
        => new(new DbContextOptionsBuilder<KnOwlDbContext>()
            .UseInMemoryDatabase($"control-plane-repo-{Guid.NewGuid():N}")
            .Options);

    private static KnOwlDocumentationDbContext CreateDocumentationContext()
        => new(new DbContextOptionsBuilder<KnOwlDocumentationDbContext>()
            .UseInMemoryDatabase($"docs-repo-{Guid.NewGuid():N}")
            .Options);

    private static KnOwlSecurityDbContext CreateSecurityContext()
        => new(new DbContextOptionsBuilder<KnOwlSecurityDbContext>()
            .UseInMemoryDatabase($"security-repo-{Guid.NewGuid():N}")
            .Options);

    private static KnOwlRuntimeDbContext CreateRuntimeContext()
        => new(new DbContextOptionsBuilder<KnOwlRuntimeDbContext>()
            .UseInMemoryDatabase($"runtime-repo-{Guid.NewGuid():N}")
            .Options);

    private static ContractArtifact CreateArtifact(string topic, string sourceStatus)
        => new()
        {
            ArtifactType = ContractArtifactType.Event,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Name = topic,
            Topic = topic,
            VersionNumber = "1.0.0",
            PayloadSchemaJson = "{}",
            ContentHash = Guid.NewGuid().ToString("N"),
            SourceStatus = sourceStatus
        };

    private static RuntimeNode CreateRuntimeNode(RuntimeEnvironment environment)
        => new()
        {
            Name = "Runtime",
            Code = "runtime",
            EnvironmentId = environment.Id,
            EnvironmentName = environment.Name,
            DistributionMode = DistributionMode.Hybrid,
            EndpointBaseUri = "https://runtime.example.test",
            EndpointApiPath = "/runtime",
            Status = RuntimeNodeStatus.Active,
            IsEnabled = true,
            InboundClientId = "inbound-client",
            InboundCredentialStatus = ConnectionCredentialStatus.Active,
            OutboundCredentialStatus = ConnectionCredentialStatus.Active
        };

    private static RuntimeContractArtifact CreateRuntimeArtifact(string topic, DateTime? deployedAtUtc = null, string versionNumber = "1.0.0")
        => new()
        {
            SourceArtifactId = Guid.NewGuid(),
            SourceReleaseId = Guid.NewGuid(),
            ArtifactType = ContractArtifactType.Event,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Name = topic,
            Topic = topic,
            VersionNumber = versionNumber,
            PayloadSchemaJson = "{}",
            ContentHash = Guid.NewGuid().ToString("N"),
            DeployedAtUtc = deployedAtUtc ?? DateTime.UtcNow
        };
}
