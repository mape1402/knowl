using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Application;
using KnOwl.Runtime.Application.Catalog;
using KnOwl.Runtime.Application.Deployments;
using KnOwl.Runtime.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Tests;

public sealed class RuntimeContractDeploymentServiceTests
{
    [Fact]
    public async Task DeployArtifactCopiesPackageToRuntimeStorage()
    {
        var package = CreatePackage("hash-a");
        RuntimeArtifactRepository runtime = new();

        using var provider = CreateProvider(runtime);
        var service = provider.GetRequiredService<IRuntimeContractDeploymentService>();

        var result = await service.DeployArtifact(package, "control-plane");

        Assert.True(result.Accepted);
        Assert.Equal("Ready", result.Status);
        Assert.Single(runtime.Items);
    }

    [Fact]
    public async Task DeployArtifactIsIdempotentWhenHashMatches()
    {
        var package = CreatePackage("hash-a");
        RuntimeArtifactRepository runtime = new();

        using var provider = CreateProvider(runtime);
        var service = provider.GetRequiredService<IRuntimeContractDeploymentService>();

        await service.DeployArtifact(package, "control-plane");
        await service.DeployArtifact(package, "control-plane");

        Assert.Single(runtime.Items);
    }

    [Fact]
    public async Task DeployArtifactRejectsSameIdentityWithDifferentHash()
    {
        var package = CreatePackage("hash-b");
        RuntimeArtifactRepository runtime = new();
        runtime.Items.Add(new RuntimeContractArtifact
        {
            ArtifactType = ContractArtifactType.Event,
            Topic = "customer.created",
            VersionNumber = "1.0.0",
            ContentHash = "hash-a",
            Name = "customer.created",
            PayloadSchemaJson = "{}"
        });

        using var provider = CreateProvider(runtime);
        var service = provider.GetRequiredService<IRuntimeContractDeploymentService>();

        var result = await service.DeployArtifact(package, "control-plane");

        Assert.False(result.Accepted);
        Assert.Equal("Rejected", result.Status);
        Assert.Single(runtime.Items);
    }

    [Fact]
    public async Task CatalogReturnsExactAndLatestArtifacts()
    {
        RuntimeArtifactRepository runtime = new();
        runtime.Items.Add(new RuntimeContractArtifact
        {
            ArtifactType = ContractArtifactType.Event,
            Topic = "customer.created",
            VersionNumber = "1.0.0",
            ContentHash = "hash-a",
            Name = "customer.created",
            PayloadSchemaJson = "{}",
            DeployedAtUtc = DateTime.UtcNow.AddMinutes(-5)
        });
        runtime.Items.Add(new RuntimeContractArtifact
        {
            ArtifactType = ContractArtifactType.Event,
            Topic = "customer.created",
            VersionNumber = "1.1.0",
            ContentHash = "hash-b",
            Name = "customer.created",
            PayloadSchemaJson = "{}",
            DeployedAtUtc = DateTime.UtcNow
        });

        using var provider = CreateProvider(runtime);
        var catalog = provider.GetRequiredService<IRuntimeContractCatalogService>();

        var exact = await catalog.GetExact(ContractArtifactType.Event, "customer.created", "1.0.0");
        var latest = await catalog.GetLatest(ContractArtifactType.Event, "customer.created");

        Assert.Equal("1.0.0", exact?.VersionNumber);
        Assert.Equal("1.1.0", latest?.VersionNumber);
    }

    private static RuntimeArtifactDeliveryPackage CreatePackage(string hash)
    {
        return new RuntimeArtifactDeliveryPackage
        {
            ReleaseTargetId = Guid.NewGuid(),
            ReleaseId = Guid.NewGuid(),
            RuntimeNodeId = Guid.NewGuid(),
            ArtifactId = Guid.NewGuid(),
            ArtifactType = ContractArtifactType.Event,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Name = "customer.created",
            Topic = "customer.created",
            VersionNumber = "1.0.0",
            PayloadSchemaJson = "{}",
            ContentHash = hash,
            EnvironmentKey = "dev",
            CorrelationId = Guid.NewGuid().ToString("N"),
            PromotedBy = "tests",
            PromotedAtUtc = DateTime.UtcNow
        };
    }

    private static ServiceProvider CreateProvider(IRuntimeContractArtifactRepository runtime)
    {
        return new ServiceCollection()
            .AddSingleton(runtime)
            .AddKnOwlRuntimeApplication()
            .BuildServiceProvider();
    }

    private sealed class RuntimeArtifactRepository : IRuntimeContractArtifactRepository
    {
        public List<RuntimeContractArtifact> Items { get; } = [];
        public Task<IReadOnlyList<RuntimeContractArtifact>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RuntimeContractArtifact>>(Items);
        public Task<RuntimeContractArtifact?> GetByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));
        public Task<RuntimeContractArtifact?> GetLatest(ContractArtifactType artifactType, string topic, CancellationToken cancellationToken = default) => Task.FromResult(Items.Where(x => x.ArtifactType == artifactType && x.Topic == topic).OrderByDescending(x => x.DeployedAtUtc).FirstOrDefault());

        public Task Create(RuntimeContractArtifact artifact, CancellationToken cancellationToken = default)
        {
            Items.Add(artifact);
            return Task.CompletedTask;
        }
    }
}

