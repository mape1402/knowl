using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Application.Distribution;
using KnOwl.ControlPlane.Application.Distribution.Catalog;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Tests;

public sealed class ControlPlaneContractCatalogServiceTests
{
    [Fact]
    public async Task GetAllReturnsOnlyDeployedArtifacts()
    {
        ArtifactRepository artifacts = new(
            CreateArtifact("customer.created", "1.0.0", ContractVersionStatus.Approved, DateTime.UtcNow.AddMinutes(-10)),
            CreateArtifact("customer.created", "1.1.0", ContractVersionStatus.Deployed, DateTime.UtcNow));

        using var provider = CreateProvider(artifacts);
        var catalog = provider.GetRequiredService<IControlPlaneContractCatalogService>();

        var result = await catalog.GetAll();

        var artifact = Assert.Single(result);
        Assert.Equal("1.1.0", artifact.VersionNumber);
    }

    [Fact]
    public async Task GetExactReturnsOnlyDeployedArtifact()
    {
        ArtifactRepository artifacts = new(
            CreateArtifact("customer.created", "1.0.0", ContractVersionStatus.Approved, DateTime.UtcNow.AddMinutes(-10)),
            CreateArtifact("customer.created", "1.1.0", ContractVersionStatus.Deployed, DateTime.UtcNow));

        using var provider = CreateProvider(artifacts);
        var catalog = provider.GetRequiredService<IControlPlaneContractCatalogService>();

        var approved = await catalog.GetExact(ContractArtifactType.Event, "customer.created", "1.0.0");
        var deployed = await catalog.GetExact(ContractArtifactType.Event, "customer.created", "1.1.0");

        Assert.Null(approved);
        Assert.Equal("1.1.0", deployed?.VersionNumber);
    }

    [Fact]
    public async Task GetLatestReturnsNewestDeployedArtifact()
    {
        ArtifactRepository artifacts = new(
            CreateArtifact("customer.created", "1.0.0", ContractVersionStatus.Deployed, DateTime.UtcNow.AddMinutes(-10)),
            CreateArtifact("customer.created", "1.1.0", ContractVersionStatus.Deployed, DateTime.UtcNow),
            CreateArtifact("customer.created", "2.0.0", ContractVersionStatus.Approved, DateTime.UtcNow.AddMinutes(10)));

        using var provider = CreateProvider(artifacts);
        var catalog = provider.GetRequiredService<IControlPlaneContractCatalogService>();

        var latest = await catalog.GetLatest(ContractArtifactType.Event, "customer.created");

        Assert.Equal("1.1.0", latest?.VersionNumber);
    }

    private static ServiceProvider CreateProvider(IContractArtifactRepository artifacts)
    {
        return new ServiceCollection()
            .AddSingleton(artifacts)
            .AddKnOwlControlPlaneDistributionApplication()
            .BuildServiceProvider();
    }

    private static ContractArtifact CreateArtifact(string topic, string version, ContractVersionStatus status, DateTime createdAtUtc)
    {
        return new ContractArtifact
        {
            Id = Guid.NewGuid(),
            ArtifactType = ContractArtifactType.Event,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Name = topic,
            Topic = topic,
            VersionNumber = version,
            PayloadSchemaJson = "{}",
            ContentHash = Guid.NewGuid().ToString("N"),
            SourceStatus = status.ToString(),
            CreatedAtUtc = createdAtUtc
        };
    }

    private sealed class ArtifactRepository(params ContractArtifact[] artifacts) : IContractArtifactRepository
    {
        public List<ContractArtifact> Items { get; } = [.. artifacts];
        public Task<IReadOnlyList<ContractArtifact>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractArtifact>>(Items);
        public Task<IReadOnlyList<ContractArtifact>> GetDeployed(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractArtifact>>(Items.Where(IsDeployed).OrderByDescending(x => x.CreatedAtUtc).ToList());
        public Task<ContractArtifact?> GetById(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<ContractArtifact>> GetByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractArtifact>>(Items.Where(x => ids.Contains(x.Id)).ToList());
        public Task<ContractArtifact?> GetBySourceVersion(ContractArtifactType artifactType, Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => x.ArtifactType == artifactType && x.VersionId == versionId));
        public Task<ContractArtifact?> GetByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));
        public Task<ContractArtifact?> GetDeployedByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => IsDeployed(x) && x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));
        public Task<ContractArtifact?> GetLatestDeployed(ContractArtifactType artifactType, string topic, CancellationToken cancellationToken = default) => Task.FromResult(Items.Where(x => IsDeployed(x) && x.ArtifactType == artifactType && x.Topic == topic).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault());
        public Task Create(ContractArtifact artifact, CancellationToken cancellationToken = default) { Items.Add(artifact); return Task.CompletedTask; }

        private static bool IsDeployed(ContractArtifact artifact)
            => string.Equals(artifact.SourceStatus, ContractVersionStatus.Deployed.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
