using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using System.Net;
using System.Net.Http.Json;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Application.Distribution;
using KnOwl.ControlPlane.Application.Distribution.Security;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Storage.EntityFramework.Distribution;
using KnOwl.Runtime.Distribution;
using KnOwl.Runtime.Application;
using KnOwl.Runtime.Application.Catalog;
using KnOwl.Runtime.Application.Security;
using KnOwl.Runtime.Storage.EntityFramework;
using KnOwl.Runtime.Storage.EntityFramework.Data;
using KnOwl.ControlPlane.Storage.EntityFramework;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Tests;

[Collection(SqlServerIntegrationCollection.Name)]
public sealed class DistributionRuntimeE2ETests
{
    [Fact]
    public async Task ControlPlanePushDeploysArtifactIntoRuntimeHost()
    {
        var controlConnectionString = Environment.GetEnvironmentVariable("KNOWL_CONTROL_SQL_INTEGRATION_CONNECTION");
        var runtimeConnectionString = Environment.GetEnvironmentVariable("KNOWL_RUNTIME_SQL_INTEGRATION_CONNECTION");
        var runtimeBaseUrl = Environment.GetEnvironmentVariable("KNOWL_E2E_RUNTIME_BASE_URL")?.TrimEnd('/');
        var controlBaseUrl = Environment.GetEnvironmentVariable("KNOWL_E2E_CONTROL_BASE_URL")?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(controlConnectionString) ||
            string.IsNullOrWhiteSpace(runtimeConnectionString) ||
            string.IsNullOrWhiteSpace(runtimeBaseUrl) ||
            string.IsNullOrWhiteSpace(controlBaseUrl))
        {
            return;
        }

        using var http = new HttpClient();
        using var healthResponse = await http.GetAsync($"{runtimeBaseUrl}/health");
        using var controlHealthResponse = await http.GetAsync($"{controlBaseUrl}/health");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, controlHealthResponse.StatusCode);

        await using var runtimeProvider = new ServiceCollection()
            .AddKnOwlRuntimeStorageEntityFramework(runtimeConnectionString, "KnOwl.RuntimeHost.Sample")
            .AddKnOwlRuntimeApplication()
            .BuildServiceProvider();
        await using var controlProvider = new ServiceCollection()
            .AddKnOwlControlPlaneStorageEntityFramework(controlConnectionString, "KnOwl.ControlPlaneHost.Sample")
            .AddKnOwlControlPlaneDistributionStorageEntityFramework()
            .AddKnOwlControlPlaneDistributionApplication()
            .BuildServiceProvider();

        await ResetRuntimeDatabase(runtimeProvider);
        await ResetControlPlaneDatabase(controlProvider);

        var controlRuntimeNodeId = Guid.NewGuid();
        var runtimeDesignNodeId = Guid.NewGuid();

        var runtimePackage = await CreateRuntimeInboundCredential(
            runtimeProvider,
            runtimeDesignNodeId,
            controlRuntimeNodeId,
            runtimeBaseUrl);

        var releaseTargetId = await CreateControlPlaneReleaseTarget(
            controlProvider,
            controlRuntimeNodeId);

        using var importResponse = await http.PostAsJsonAsync(
            $"{controlBaseUrl}/distribution/runtime-nodes/{controlRuntimeNodeId}/credentials/import",
            new { Package = runtimePackage.Json });
        var importBody = await importResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        using var pushResponse = await http.PostAsync(
            $"{controlBaseUrl}/distribution/artifacts/targets/{releaseTargetId}/push",
            content: null);
        var pushBody = await pushResponse.Content.ReadAsStringAsync();

        var runtimeCatalog = runtimeProvider.GetRequiredService<IRuntimeContractCatalogService>();
        var runtimeArtifact = await runtimeCatalog.GetExact(ContractArtifactType.Event, "customer.created", "1.0.0");

        var controlDb = controlProvider.GetRequiredService<KnOwlDbContext>();
        var controlTarget = await controlDb.ContractReleaseTargets.AsNoTracking().FirstAsync(x => x.Id == releaseTargetId);
        var release = await controlDb.ContractReleases.AsNoTracking().FirstAsync(x => x.Id == controlTarget.ReleaseId);

        using var catalogResponse = await http.GetAsync($"{runtimeBaseUrl}/runtime/contracts/event/customer.created/versions/1.0.0");
        using var controlCatalogResponse = await http.GetAsync($"{controlBaseUrl}/contracts/event/customer.created/versions/1.0.0");
        using var controlLatestResponse = await http.GetAsync($"{controlBaseUrl}/contracts/event/customer.created/latest");

        Assert.Equal(HttpStatusCode.OK, pushResponse.StatusCode);
        Assert.Contains("Runtime artifact pushed", pushBody);
        Assert.Contains("imported", importBody);
        Assert.Equal(ContractReleaseTargetStatus.Activated, controlTarget.Status);
        Assert.Equal(ContractReleaseStatus.Completed, release.Status);
        Assert.NotNull(runtimeArtifact);
        Assert.Equal("hash-e2e", runtimeArtifact.ContentHash);
        Assert.Equal(HttpStatusCode.OK, catalogResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, controlCatalogResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, controlLatestResponse.StatusCode);
    }

    [Fact]
    public async Task RuntimePullsArtifactFromControlPlaneAndAcknowledgesReleaseTarget()
    {
        var controlConnectionString = Environment.GetEnvironmentVariable("KNOWL_CONTROL_SQL_INTEGRATION_CONNECTION");
        var runtimeConnectionString = Environment.GetEnvironmentVariable("KNOWL_RUNTIME_SQL_INTEGRATION_CONNECTION");
        var runtimeBaseUrl = Environment.GetEnvironmentVariable("KNOWL_E2E_RUNTIME_BASE_URL")?.TrimEnd('/');
        var controlBaseUrl = Environment.GetEnvironmentVariable("KNOWL_E2E_CONTROL_BASE_URL")?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(controlConnectionString) ||
            string.IsNullOrWhiteSpace(runtimeConnectionString) ||
            string.IsNullOrWhiteSpace(runtimeBaseUrl) ||
            string.IsNullOrWhiteSpace(controlBaseUrl))
        {
            return;
        }

        using var http = new HttpClient();
        using var runtimeHealthResponse = await http.GetAsync($"{runtimeBaseUrl}/health");
        using var controlHealthResponse = await http.GetAsync($"{controlBaseUrl}/health");
        Assert.Equal(HttpStatusCode.OK, runtimeHealthResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, controlHealthResponse.StatusCode);

        await using var runtimeProvider = new ServiceCollection()
            .AddKnOwlRuntimeStorageEntityFramework(runtimeConnectionString, "KnOwl.RuntimeHost.Sample")
            .AddKnOwlRuntimeApplication()
            .BuildServiceProvider();
        await using var controlProvider = new ServiceCollection()
            .AddKnOwlControlPlaneStorageEntityFramework(controlConnectionString, "KnOwl.ControlPlaneHost.Sample")
            .AddKnOwlControlPlaneDistributionStorageEntityFramework()
            .AddKnOwlControlPlaneDistributionApplication()
            .BuildServiceProvider();

        await ResetRuntimeDatabase(runtimeProvider);
        await ResetControlPlaneDatabase(controlProvider);

        var controlRuntimeNodeId = Guid.NewGuid();
        var runtimeDesignNodeId = Guid.NewGuid();
        var setup = await CreateControlPlanePullReleaseTarget(
            controlProvider,
            controlRuntimeNodeId,
            controlBaseUrl);

        using var upsertResponse = await http.PostAsJsonAsync(
            $"{runtimeBaseUrl}/runtime/distribution/design-nodes",
            new
            {
                Id = runtimeDesignNodeId,
                Key = "knowl-control-plane",
                Name = "KnOwl Control Plane",
                EndpointBaseUri = controlBaseUrl,
                RemoteRuntimeNodeId = controlRuntimeNodeId.ToString("N"),
                IsEnabled = true
            });
        Assert.Equal(HttpStatusCode.OK, upsertResponse.StatusCode);

        using var runtimeImportResponse = await http.PostAsJsonAsync(
            $"{runtimeBaseUrl}/runtime/distribution/design-nodes/{runtimeDesignNodeId}/credentials/import",
            new { Package = setup.CredentialPackageJson });
        Assert.Equal(HttpStatusCode.OK, runtimeImportResponse.StatusCode);

        using var applyResponse = await http.PostAsync(
            $"{runtimeBaseUrl}/runtime/distribution/control-planes/knowl-control-plane/artifacts/{setup.ReleaseTargetId}/apply",
            content: null);
        var applyBody = await applyResponse.Content.ReadAsStringAsync();

        var runtimeCatalog = runtimeProvider.GetRequiredService<IRuntimeContractCatalogService>();
        var runtimeArtifact = await runtimeCatalog.GetExact(ContractArtifactType.Command, "customer.register", "1.0.0");

        var controlDb = controlProvider.GetRequiredService<KnOwlDbContext>();
        var controlTarget = await controlDb.ContractReleaseTargets.AsNoTracking().FirstAsync(x => x.Id == setup.ReleaseTargetId);
        var release = await controlDb.ContractReleases.AsNoTracking().FirstAsync(x => x.Id == controlTarget.ReleaseId);
        var attempts = await controlDb.ContractReleaseAttempts.AsNoTracking().Where(x => x.ReleaseTargetId == setup.ReleaseTargetId).ToListAsync();
        using var controlCatalogResponse = await http.GetAsync($"{controlBaseUrl}/contracts/command/customer.register/versions/1.0.0");

        Assert.Equal(HttpStatusCode.OK, applyResponse.StatusCode);
        Assert.Contains("Ready", applyBody);
        Assert.NotNull(runtimeArtifact);
        Assert.Equal("hash-pull-e2e", runtimeArtifact.ContentHash);
        Assert.Equal(ContractReleaseTargetStatus.Activated, controlTarget.Status);
        Assert.Equal(ContractReleaseStatus.Completed, release.Status);
        Assert.Contains(attempts, x => x.Action == "Ack" && x.Succeeded);
        Assert.Equal(HttpStatusCode.OK, controlCatalogResponse.StatusCode);
    }

    private static async Task ResetRuntimeDatabase(ServiceProvider provider)
    {
        var db = provider.GetRequiredService<KnOwlRuntimeDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    private static async Task ResetControlPlaneDatabase(ServiceProvider provider)
    {
        var db = provider.GetRequiredService<KnOwlDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    private static async Task<RuntimeDesignNodeCredentialPackageModel> CreateRuntimeInboundCredential(
        ServiceProvider runtimeProvider,
        Guid designNodeId,
        Guid controlRuntimeNodeId,
        string runtimeBaseUrl)
    {
        var runtimeConnection = runtimeProvider.GetRequiredService<IRuntimeDesignNodeConnectionService>();
        await runtimeConnection.UpsertDesignNode(
            designNodeId,
            "knowl-control-plane",
            "KnOwl Control Plane",
            "http://127.0.0.1:18080",
            controlRuntimeNodeId.ToString("N"),
            true);

        return await runtimeConnection.GenerateCredentialPackage(designNodeId, runtimeBaseUrl);
    }

    private static async Task<Guid> CreateControlPlaneReleaseTarget(
        ServiceProvider controlProvider,
        Guid runtimeNodeId)
    {
        var db = controlProvider.GetRequiredService<KnOwlDbContext>();
        var environment = new RuntimeEnvironment
        {
            Id = Guid.NewGuid(),
            Name = "Development",
            Code = "dev",
            IsEnabled = true
        };
        var runtimeNode = new RuntimeNode
        {
            Id = runtimeNodeId,
            Name = "Runtime Dev",
            Code = "runtime-dev",
            Environment = environment,
            EnvironmentName = environment.Code,
            DistributionMode = DistributionMode.Push,
            EndpointBaseUri = "placeholder-overwritten-by-import",
            EndpointApiPath = "runtime/artifacts/deploy",
            AuthenticationMode = RuntimeAuthenticationMode.ClientCredentials,
            Status = RuntimeNodeStatus.Active,
            IsEnabled = true
        };

        db.RuntimeEnvironments.Add(environment);
        db.RuntimeNodes.Add(runtimeNode);
        await db.SaveChangesAsync();

        var release = new ContractRelease
        {
            Id = Guid.NewGuid(),
            Name = $"e2e-{Guid.NewGuid():N}",
            Status = ContractReleaseStatus.InProgress
        };
        var artifact = new ContractArtifact
        {
            Id = Guid.NewGuid(),
            ArtifactType = ContractArtifactType.Event,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Name = "Customer Created",
            Topic = "customer.created",
            VersionNumber = "1.0.0",
            PayloadSchemaJson = "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"string\",\"required\":true}}}",
            ContentHash = "hash-e2e",
            SourceStatus = "Deployed"
        };
        var item = new ContractReleaseItem
        {
            Id = Guid.NewGuid(),
            ReleaseId = release.Id,
            ArtifactId = artifact.Id
        };
        var target = new ContractReleaseTarget
        {
            Id = Guid.NewGuid(),
            ReleaseId = release.Id,
            ReleaseItemId = item.Id,
            RuntimeNodeId = runtimeNodeId,
            ArtifactId = artifact.Id,
            Status = ContractReleaseTargetStatus.PushScheduled,
            ActivationStatus = ContractReleaseActivationStatus.NotActivated,
            RolloutGroup = "E2E",
            CorrelationId = Guid.NewGuid().ToString("N")
        };

        db.ContractArtifacts.Add(artifact);
        db.ContractReleases.Add(release);
        db.ContractReleaseItems.Add(item);
        db.ContractReleaseTargets.Add(target);
        await db.SaveChangesAsync();

        return target.Id;
    }

    private static async Task<(Guid ReleaseTargetId, string CredentialPackageJson)> CreateControlPlanePullReleaseTarget(
        ServiceProvider controlProvider,
        Guid runtimeNodeId,
        string controlBaseUrl)
    {
        var db = controlProvider.GetRequiredService<KnOwlDbContext>();
        var environment = new RuntimeEnvironment
        {
            Id = Guid.NewGuid(),
            Name = "Development",
            Code = "dev",
            IsEnabled = true
        };
        var runtimeNode = new RuntimeNode
        {
            Id = runtimeNodeId,
            Name = "Runtime Pull Dev",
            Code = "runtime-pull-dev",
            Environment = environment,
            EnvironmentName = environment.Code,
            DistributionMode = DistributionMode.Pull,
            EndpointBaseUri = string.Empty,
            EndpointApiPath = "runtime/artifacts/deploy",
            AuthenticationMode = RuntimeAuthenticationMode.ClientCredentials,
            Status = RuntimeNodeStatus.Active,
            IsEnabled = true
        };
        var release = new ContractRelease
        {
            Id = Guid.NewGuid(),
            Name = $"pull-e2e-{Guid.NewGuid():N}",
            Status = ContractReleaseStatus.InProgress
        };
        var artifact = new ContractArtifact
        {
            Id = Guid.NewGuid(),
            ArtifactType = ContractArtifactType.Command,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Name = "Register Customer",
            Topic = "customer.register",
            VersionNumber = "1.0.0",
            PayloadSchemaJson = "{\"type\":\"object\",\"properties\":{\"customerId\":{\"type\":\"string\",\"required\":true}}}",
            ContentHash = "hash-pull-e2e",
            SourceStatus = "Deployed"
        };
        var item = new ContractReleaseItem
        {
            Id = Guid.NewGuid(),
            ReleaseId = release.Id,
            ArtifactId = artifact.Id
        };
        var target = new ContractReleaseTarget
        {
            Id = Guid.NewGuid(),
            ReleaseId = release.Id,
            ReleaseItemId = item.Id,
            RuntimeNodeId = runtimeNodeId,
            ArtifactId = artifact.Id,
            Status = ContractReleaseTargetStatus.AvailableForPull,
            ActivationStatus = ContractReleaseActivationStatus.NotActivated,
            RolloutGroup = "E2E",
            AvailableAtUtc = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid().ToString("N")
        };

        db.RuntimeEnvironments.Add(environment);
        db.RuntimeNodes.Add(runtimeNode);
        db.ContractArtifacts.Add(artifact);
        db.ContractReleases.Add(release);
        db.ContractReleaseItems.Add(item);
        db.ContractReleaseTargets.Add(target);
        await db.SaveChangesAsync();

        var connection = controlProvider.GetRequiredService<IRuntimeNodeConnectionInteractionService>();
        var credentialPackage = await connection.GenerateCredentialPackage(runtimeNodeId, controlBaseUrl);
        return (target.Id, credentialPackage.Json);
    }
}

