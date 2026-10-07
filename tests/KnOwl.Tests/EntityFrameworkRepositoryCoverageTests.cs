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
    public async Task EventRepositoryPersistsVersionLifecycleAndSoftDelete()
    {
        await using var db = CreateControlPlaneContext();
        var repository = new EventRepository(db);
        var now = DateTime.UtcNow;
        var definition = new EventDefinition { Name = "Customer Created", Topic = "customer.created", Description = "Created" };

        await repository.Create(definition);
        await repository.AddVersion(definition.Id, new EventVersion { VersionNumber = "1.0.0", PayloadSchemaJson = "{}", Status = ContractVersionStatus.Draft });
        db.ChangeTracker.Clear();
        await repository.UpdateDefinition(definition.Id, "Customer Updated", "customer.updated", null, now);
        var version = Assert.Single((await repository.GetById(definition.Id, includeVersions: true))!.Versions);
        db.ChangeTracker.Clear();
        await repository.UpdateDraftVersion(version.Id, """{"type":"object"}""", "Draft", now);
        await repository.UpdateVersionStatus(version.Id, ContractVersionStatus.InReview, now);
        await repository.UpdateVersionStatus(version.Id, ContractVersionStatus.Approved, now);
        await repository.UpdateVersionStatus(version.Id, ContractVersionStatus.Deployed, now);
        await repository.UpdateVersionStatus(version.Id, ContractVersionStatus.Deprecated, now);
        await repository.UpdateVersionStatus(version.Id, ContractVersionStatus.Archived, now);
        await repository.Delete(definition.Id);

        Assert.NotEmpty(await repository.GetAllWithVersions());
        Assert.NotNull(await repository.GetVersionById(version.Id));
        Assert.True(await repository.VersionExists(definition.Id, "1.0.0"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.UpdateDefinition(Guid.NewGuid(), "x", "x", null, now));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.UpdateVersionStatus(Guid.NewGuid(), ContractVersionStatus.InReview, now));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.UpdateDraftVersion(Guid.NewGuid(), "{}", "Missing", now));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateDraftVersion(version.Id, "{}", "Not draft", now));
        Assert.Null(await repository.GetById(Guid.NewGuid(), includeVersions: true));
    }

    [Fact]
    public async Task CommandRepositoryPersistsVersionLifecycleAndSoftDelete()
    {
        await using var db = CreateControlPlaneContext();
        var repository = new CommandRepository(db);
        var now = DateTime.UtcNow;
        var command = new CommandDefinition { Name = "Create Customer", Topic = "customer.create", Description = "Create" };

        await repository.Create(command);
        await repository.AddVersion(command.Id, new CommandVersion { VersionNumber = "1.0.0", PayloadSchemaJson = "{}", Status = ContractVersionStatus.Draft });
        db.ChangeTracker.Clear();
        await repository.UpdateDefinition(command.Id, "Create Customer V2", "customer.create.v2", "Updated", now);
        var version = Assert.Single((await repository.GetById(command.Id, includeVersions: true))!.Versions);
        db.ChangeTracker.Clear();
        await repository.UpdateDraftVersion(version.Id, """{"request":true}""", """{"reply":true}""", "Draft", now);
        await repository.UpdateVersionStatus(version.Id, ContractVersionStatus.InReview, now);
        await repository.UpdateVersionStatus(version.Id, ContractVersionStatus.Approved, now);
        await repository.UpdateVersionStatus(version.Id, ContractVersionStatus.Deployed, now);
        await repository.UpdateVersionStatus(version.Id, ContractVersionStatus.Deprecated, now);
        await repository.UpdateVersionStatus(version.Id, ContractVersionStatus.Archived, now);
        Assert.NotEmpty(await repository.GetAllWithVersions());
        await repository.Delete(command.Id);

        Assert.NotNull(await repository.GetVersionById(version.Id));
        Assert.True(await repository.VersionExists(command.Id, "1.0.0"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.AddVersion(Guid.NewGuid(), new CommandVersion { VersionNumber = "9.9.9", PayloadSchemaJson = "{}" }));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.UpdateVersionStatus(Guid.NewGuid(), ContractVersionStatus.InReview, now));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.UpdateDraftVersion(Guid.NewGuid(), "{}", null, "Missing", now));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateDraftVersion(version.Id, "{}", null, "Not draft", now));
        Assert.Null(await repository.GetById(Guid.NewGuid(), includeVersions: true));
    }

    [Fact]
    public async Task SchemaTypeRepositoryPersistsActiveVersionsAndKeyChecks()
    {
        await using var db = CreateControlPlaneContext();
        var repository = new SchemaTypeRepository(db);
        var now = DateTime.UtcNow;
        var schema = new SchemaTypeDefinition { Key = "customer-ref", Name = "Customer Ref", Description = "Ref", IsActive = true };

        await repository.Create(schema);
        await repository.AddVersion(schema.Id, new SchemaTypeVersion { VersionNumber = "1.0.0", DefinitionJson = "{}", IsActive = true });
        db.ChangeTracker.Clear();
        var activeVersion = Assert.Single(await repository.GetActiveVersionsWithDefinitions());
        Assert.Equal(schema.Id, activeVersion.SchemaTypeDefinitionId);
        Assert.NotNull(activeVersion.SchemaTypeDefinition);
        Assert.NotNull(await repository.GetVersionById(activeVersion.Id));
        db.ChangeTracker.Clear();
        await repository.UpdateDefinition(schema.Id, "customer-ref", "Customer Reference", null, false, now);
        var version = Assert.Single((await repository.GetById(schema.Id, includeVersions: true))!.Versions);
        db.ChangeTracker.Clear();
        await repository.SetVersionActive(schema.Id, version.Id, false, now);

        Assert.NotEmpty(await repository.GetAllWithVersions());
        Assert.Empty(await repository.GetActiveVersionsWithDefinitions());
        Assert.True(await repository.KeyExists("customer-ref"));
        Assert.False(await repository.KeyExists("customer-ref", schema.Id));
        Assert.True(await repository.VersionExists(schema.Id, "1.0.0"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.UpdateDefinition(Guid.NewGuid(), "x", "x", null, true, now));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.SetVersionActive(schema.Id, Guid.NewGuid(), true, now));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.SetVersionActive(Guid.NewGuid(), Guid.NewGuid(), true, now));
    }

    [Fact]
    public async Task MetadataRepositoryUpsertsVersionsAndActiveState()
    {
        await using var db = CreateControlPlaneContext();
        var repository = new ContractFieldMetadataRepository(db);
        var now = DateTime.UtcNow;
        var metadata = new ContractFieldMetadataDefinition { Key = "trace-id", Name = "Trace Id", Description = "Trace", IsActive = true };

        await repository.Create(metadata);
        await repository.UpsertVersion(metadata.Id, new ContractFieldMetadataVersion { VersionNumber = "1.0.0", DefinitionJson = "{}", IsActive = true });
        await repository.UpsertVersion(metadata.Id, new ContractFieldMetadataVersion
        {
            VersionNumber = "1.0.0",
            DefinitionJson = """{"dataType":"string"}""",
            Comment = "Updated existing metadata version",
            IsActive = false,
            UpdatedAtUtc = now
        });
        db.ChangeTracker.Clear();
        await repository.UpdateDefinition(metadata.Id, "trace-id", "Trace", null, false, now);
        var version = Assert.Single((await repository.GetById(metadata.Id, includeVersions: true))!.Versions);
        Assert.Equal("Updated existing metadata version", version.Comment);
        db.ChangeTracker.Clear();
        await repository.SetVersionActive(metadata.Id, version.Id, false, now);

        Assert.NotEmpty(await repository.GetAllWithVersions());
        Assert.Empty(await repository.GetActiveWithVersions());
        Assert.True(await repository.KeyExists("trace-id"));
        Assert.False(await repository.KeyExists("trace-id", metadata.Id));
        Assert.True(await repository.VersionExists(metadata.Id, "1.0.0"));
        Assert.Null(await repository.GetById(Guid.NewGuid(), includeVersions: true));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.SetVersionActive(Guid.NewGuid(), Guid.NewGuid(), true, now));
    }

    [Fact]
    public async Task ContractArtifactRepositoryPersistsAndQueriesDeployedArtifacts()
    {
        await using var db = CreateControlPlaneContext();
        var repository = new ContractArtifactRepository(db);
        var artifact = CreateArtifact("customer.created", ContractVersionStatus.Deployed.ToString());

        await repository.Create(artifact);

        Assert.NotEmpty(await repository.GetAll());
        Assert.NotEmpty(await repository.GetDeployed());
        Assert.NotNull(await repository.GetById(artifact.Id));
        Assert.Single(await repository.GetByIds([artifact.Id]));
        Assert.NotNull(await repository.GetBySourceVersion(artifact.ArtifactType, artifact.VersionId));
        Assert.NotNull(await repository.GetByIdentity(artifact.ArtifactType, artifact.Topic, artifact.VersionNumber));
        Assert.NotNull(await repository.GetDeployedByIdentity(artifact.ArtifactType, artifact.Topic, artifact.VersionNumber));
        Assert.NotNull(await repository.GetLatestDeployed(artifact.ArtifactType, artifact.Topic));
    }

    [Fact]
    public async Task RuntimeEnvironmentRepositoryUpdatesAndFiltersEnabledEnvironments()
    {
        await using var db = CreateControlPlaneContext();
        var repository = new RuntimeEnvironmentRepository(db);
        var environment = new RuntimeEnvironment { Name = "Development", Code = "dev", IsEnabled = true };

        await repository.Create(environment);
        db.ChangeTracker.Clear();
        await repository.Update(new RuntimeEnvironment
        {
            Id = environment.Id,
            Name = "QA",
            Code = environment.Code,
            IsEnabled = false,
            CreatedAtUtc = environment.CreatedAtUtc
        });

        Assert.NotEmpty(await repository.GetAll());
        Assert.Empty(await repository.GetEnabled());
        Assert.NotNull(await repository.GetById(environment.Id));
    }

    [Fact]
    public async Task RuntimeNodeRepositoryUpdatesDisablesAndSoftDeletesNodes()
    {
        await using var db = CreateControlPlaneContext();
        var environmentRepository = new RuntimeEnvironmentRepository(db);
        var repository = new RuntimeNodeRepository(db);
        var now = DateTime.UtcNow;
        var environment = new RuntimeEnvironment { Name = "Development", Code = "dev", IsEnabled = true };
        await environmentRepository.Create(environment);
        var node = CreateRuntimeNode(environment);
        await repository.Create(node);

        db.ChangeTracker.Clear();
        var storedNode = await repository.GetById(node.Id);
        var allNodesBeforeDelete = await repository.GetAll();
        var activeNodesBeforeDelete = await repository.GetActiveEnabled();
        storedNode!.Description = "Updated runtime node";
        await repository.Update(storedNode);
        await repository.SetIsEnabled(node.Id, false, now);
        await repository.Delete(node.Id, now);

        Assert.Single(allNodesBeforeDelete);
        Assert.Equal(environment.Id, allNodesBeforeDelete.Single().Environment?.Id);
        Assert.Single(activeNodesBeforeDelete);
        Assert.Equal(environment.Id, activeNodesBeforeDelete.Single().Environment?.Id);
        Assert.Empty(await repository.GetActiveEnabled());
        Assert.NotNull(await repository.GetByCode(" runtime "));
        Assert.NotNull(await repository.GetByInboundClientId(" inbound-client "));
        Assert.Empty(await repository.GetAll());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.SetIsEnabled(Guid.NewGuid(), true, now));
    }

    [Fact]
    public async Task ContractReleaseRepositoryHydratesGraphAndUpdatesStatus()
    {
        await using var db = CreateControlPlaneContext();
        var releaseRepository = new ContractReleaseRepository(db);
        var targetRepository = new ContractReleaseTargetRepository(db);
        var graph = await SeedDistributionGraph(db);
        var now = DateTime.UtcNow;

        await targetRepository.AddAttempt(new ContractReleaseAttempt { ReleaseTargetId = graph.Target.Id, Action = "Pull", InitiatedBy = "runtime", Succeeded = true, StartedAtUtc = now });
        await releaseRepository.UpdateStatus(graph.Release.Id, ContractReleaseStatus.Deployed, now);
        await releaseRepository.UpdateStatus(graph.Release.Id, ContractReleaseStatus.Completed, now);
        await releaseRepository.UpdateStatus(graph.Release.Id, ContractReleaseStatus.Failed, now);
        await releaseRepository.UpdateStatus(graph.Release.Id, ContractReleaseStatus.Canceled, now);

        var hydratedRelease = await releaseRepository.GetById(graph.Release.Id, includeItems: true, includeTargets: true);
        var hydratedTarget = Assert.Single(hydratedRelease!.Targets);

        Assert.Equal(graph.Artifact.Topic, hydratedRelease.Items.Single().Artifact!.Topic);
        Assert.Equal(graph.Environment.Id, hydratedTarget.RuntimeNode!.Environment!.Id);
        Assert.NotEmpty(hydratedTarget.Attempts);
        Assert.NotEmpty(await releaseRepository.GetAll());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => releaseRepository.UpdateStatus(Guid.NewGuid(), ContractReleaseStatus.Deployed, now));
    }

    [Fact]
    public async Task ContractReleaseTargetRepositoryTracksAttemptsPendingAndBulkTargets()
    {
        await using var db = CreateControlPlaneContext();
        var repository = new ContractReleaseTargetRepository(db);
        var graph = await SeedDistributionGraph(db);
        var now = DateTime.UtcNow;

        await repository.AddAttempt(new ContractReleaseAttempt { ReleaseTargetId = graph.Target.Id, Action = "Pull", InitiatedBy = "runtime", Succeeded = true, StartedAtUtc = now });
        var targetList = await repository.GetByRelease(graph.Release.Id);
        var pending = await repository.GetPendingForRuntimeNode(graph.Node.Id);
        var includedTarget = await repository.GetById(graph.Target.Id, includeArtifact: true);
        includedTarget!.Status = ContractReleaseTargetStatus.Delivered;
        includedTarget.RuntimeVersionApplied = "runtime-1.0.0";
        await repository.Update(includedTarget);
        await repository.CreateMany([new ContractReleaseTarget
        {
            Id = Guid.NewGuid(),
            ReleaseId = graph.Release.Id,
            ReleaseItemId = graph.Item.Id,
            RuntimeNodeId = graph.Node.Id,
            ArtifactId = graph.Artifact.Id,
            RolloutGroup = "manual",
            Status = ContractReleaseTargetStatus.PushScheduled,
            CorrelationId = "corr-2"
        }]);

        Assert.Single(targetList);
        Assert.Single(pending);
        Assert.NotNull(includedTarget.Artifact);
    }

    [Fact]
    public async Task DocumentationRepositoryUpsertsMetadataAndVersionAssets()
    {
        await using var db = CreateDocumentationContext();
        var repository = new DocumentationRepository(db);
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
        var publishedVersion = await repository.AddVersion(new DocumentationPageVersion
        {
            PageId = page.Id,
            VersionNumber = "1.1.0",
            EntryPath = "docs/latest.md",
            ContentHash = "hash-latest",
            Status = DocPageVersionStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddMinutes(1)
        });
        await repository.AddAsset(new DocumentationAsset
        {
            PageVersionId = publishedVersion.Id,
            LogicalPath = "docs/latest.md",
            FileName = "latest.md",
            ContentType = "text/markdown",
            ContentHash = "hash-latest",
            StorageKey = "asset-latest"
        });
        var latestPublished = await repository.GetLatestPublishedVersion("knowl", "guides", "intro", includeAssets: true);

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
        Assert.Null(await repository.GetLatestPublishedVersion("missing", "guides", "intro"));
        Assert.NotNull(latestPublished);
        Assert.Single(latestPublished!.Assets);
        Assert.NotNull(await repository.GetAsset(asset.Id));
        Assert.NotNull(await repository.GetAssetByLogicalPath(version.Id, "docs/index.md"));
    }

    [Fact]
    public async Task DocumentationContentStoreSavesOverwritesOpensAndDeletesContent()
    {
        await using var db = CreateDocumentationContext();
        var content = new DatabaseDocumentationContentStore(db);

        await content.Save("asset", new MemoryStream("# Title"u8.ToArray()), "text/markdown");
        await content.Save("asset", new MemoryStream("Updated"u8.ToArray()), "text/plain");
        await using var opened = await content.Open("asset");
        using var reader = new StreamReader(opened, Encoding.UTF8);
        var openedText = await reader.ReadToEndAsync();
        await content.Delete("asset");

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
    public async Task RuntimeArtifactRepositoryPersistsAndQueriesArtifacts()
    {
        await using var db = CreateRuntimeContext();
        var repository = new RuntimeContractArtifactRepository(db);
        var artifact = CreateRuntimeArtifact("customer.created");
        var olderArtifact = CreateRuntimeArtifact("customer.created", DateTime.UtcNow.AddDays(-1), "0.9.0");

        await repository.Create(olderArtifact);
        await repository.Create(artifact);

        Assert.Equal(2, (await repository.GetAll()).Count);
        Assert.Equal(artifact.Id, (await repository.GetByIdentity(artifact.ArtifactType, artifact.Topic, artifact.VersionNumber))!.Id);
        Assert.Equal(artifact.Id, (await repository.GetLatest(artifact.ArtifactType, artifact.Topic))!.Id);
    }

    [Fact]
    public async Task RuntimeDesignNodeRepositoryUpsertsAndFindsConnections()
    {
        await using var db = CreateRuntimeContext();
        var repository = new RuntimeDesignNodeRepository(db);
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

        await repository.Upsert(designNode);
        designNode.Name = "Control Plane Updated";
        await repository.Upsert(designNode);

        Assert.Single(await repository.GetAll());
        Assert.NotNull(await repository.GetById(designNode.Id));
        Assert.NotNull(await repository.GetByKey(" control-plane "));
        Assert.NotNull(await repository.GetByInboundClientId(" inbound-client "));
    }

    private static async Task<DistributionGraph> SeedDistributionGraph(KnOwlDbContext db)
    {
        var artifactRepository = new ContractArtifactRepository(db);
        var environmentRepository = new RuntimeEnvironmentRepository(db);
        var nodeRepository = new RuntimeNodeRepository(db);
        var releaseRepository = new ContractReleaseRepository(db);
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
        return new DistributionGraph(artifact, environment, node, release, item, target);
    }

    private sealed record DistributionGraph(
        ContractArtifact Artifact,
        RuntimeEnvironment Environment,
        RuntimeNode Node,
        ContractRelease Release,
        ContractReleaseItem Item,
        ContractReleaseTarget Target);

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
