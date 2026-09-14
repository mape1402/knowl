using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Distribution;
using KnOwl.ControlPlane.Storage.EntityFramework.Distribution.Repositories;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Distribution;
using KnOwl.Runtime.Application;
using KnOwl.Runtime.Application.Catalog;
using KnOwl.Runtime.Application.Deployments;
using KnOwl.Runtime.Storage.EntityFramework;
using KnOwl.Runtime.Storage.EntityFramework.Data;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Tests;

[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerStorageIntegrationTests
{
    [Fact]
    public async Task ControlPlaneSqlServerMigrationPersistsRuntimeNodeSecurityFields()
    {
        var connectionString = Environment.GetEnvironmentVariable("KNOWL_CONTROL_SQL_INTEGRATION_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var options = new DbContextOptionsBuilder<KnOwlDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly("KnOwl.ControlPlaneHost.Sample"))
            .Options;

        await using var db = new KnOwlDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        var environment = new RuntimeEnvironment
        {
            Id = Guid.NewGuid(),
            Name = "Development",
            Code = "dev",
            IsEnabled = true
        };
        var runtimeNode = new RuntimeNode
        {
            Id = Guid.NewGuid(),
            Name = "Runtime Dev",
            Code = "runtime-dev",
            Environment = environment,
            EnvironmentName = environment.Code,
            DistributionMode = DistributionMode.Hybrid,
            EndpointBaseUri = "https://runtime.example.test",
            EndpointApiPath = "runtime/artifacts/deploy",
            AuthenticationMode = RuntimeAuthenticationMode.None,
            Status = RuntimeNodeStatus.Active,
            IsEnabled = true,
            AccessTokenTtlSeconds = 600,
            InboundClientId = "runtime-client",
            InboundKeyId = "runtime-key",
            InboundSecretHash = "hash",
            InboundAllowedScopes = "release:read artifact:ack",
            InboundCredentialStatus = ConnectionCredentialStatus.Active,
            OutboundClientId = "runtime-outbound-client",
            OutboundKeyId = "runtime-outbound-key",
            ProtectedOutboundSecret = "protected",
            OutboundRequestedScopes = "artifact:push",
            OutboundCredentialStatus = ConnectionCredentialStatus.Active
        };

        db.RuntimeEnvironments.Add(environment);
        db.RuntimeNodes.Add(runtimeNode);
        await db.SaveChangesAsync();

        var repository = new RuntimeNodeRepository(db);
        var loaded = await repository.GetByInboundClientId("runtime-client");

        Assert.NotNull(loaded);
        Assert.Equal(runtimeNode.Id, loaded.Id);
        Assert.Equal(ConnectionCredentialStatus.Active, loaded.InboundCredentialStatus);
        Assert.False(loaded.IsDeleted);
    }

    [Fact]
    public async Task RuntimeSqlServerMigrationPersistsDesignNodesAndArtifacts()
    {
        var connectionString = Environment.GetEnvironmentVariable("KNOWL_RUNTIME_SQL_INTEGRATION_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var services = new ServiceCollection()
            .AddKnOwlRuntimeStorageEntityFramework(connectionString, "KnOwl.RuntimeHost.Sample")
            .AddKnOwlRuntimeApplication()
            .BuildServiceProvider();

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<KnOwlRuntimeDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        var designNode = new RuntimeDesignNode
        {
            Id = Guid.NewGuid(),
            Key = "knowl-control-plane",
            Name = "KnOwl Control Plane",
            EndpointBaseUri = "https://knowl.example.test",
            RemoteRuntimeNodeId = Guid.NewGuid().ToString("N"),
            DistributionMode = DistributionMode.Hybrid,
            IsEnabled = true,
            Status = RuntimeDesignNodeStatus.Enabled,
            InboundClientId = "control-plane-client",
            InboundKeyId = "control-plane-key",
            InboundSecretHash = "hash",
            InboundAllowedScopes = "artifact:push",
            InboundCredentialStatus = ConnectionCredentialStatus.Active
        };

        db.RuntimeDesignNodes.Add(designNode);
        await db.SaveChangesAsync();

        var deployment = scope.ServiceProvider.GetRequiredService<IRuntimeContractDeploymentService>();
        var catalog = scope.ServiceProvider.GetRequiredService<IRuntimeContractCatalogService>();
        var package = new RuntimeArtifactDeliveryPackage
        {
            ReleaseTargetId = Guid.NewGuid(),
            ReleaseId = Guid.NewGuid(),
            RuntimeNodeId = Guid.NewGuid(),
            ArtifactId = Guid.NewGuid(),
            ArtifactType = ContractArtifactType.Event,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Name = "Customer Created",
            Topic = "customer.created",
            VersionNumber = "1.0.0",
            PayloadSchemaJson = "{\"type\":\"object\"}",
            ContentHash = "hash-a",
            EnvironmentKey = "dev",
            CorrelationId = Guid.NewGuid().ToString("N"),
            PromotedBy = "integration",
            PromotedAtUtc = DateTime.UtcNow
        };

        var result = await deployment.DeployArtifact(package, "knowl-control-plane");
        var stored = await catalog.GetExact(ContractArtifactType.Event, "customer.created", "1.0.0");

        Assert.True(result.Accepted);
        Assert.NotNull(stored);
        Assert.Equal(package.ContentHash, stored.ContentHash);
    }
}

