using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.ControlPlane.WebUI.Pages.Contracts.ContractArtifacts;

namespace KnOwl.Tests;

public class ContractArtifactsPageModelTests
{
    [Fact]
    public async Task OnGetGroupsCommandArtifactAsSingleLogicalArtifactWithRequestAndReplyDetails()
    {
        var definitionId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var repository = new InMemoryContractArtifactRepository(
            CreateCommandArtifact(definitionId, versionId));
        var model = new IndexModel(repository)
        {
            GroupKey = $"Command:{definitionId:N}"
        };

        await model.OnGetAsync(CancellationToken.None);

        var artifact = Assert.Single(model.SelectedArtifacts);
        Assert.Equal("Command", artifact.ContractKind);
        Assert.Equal("Customer Register", artifact.Name);
        Assert.Equal("Request and reply schemas", artifact.SchemaSummary);
        Assert.Single(artifact.Artifacts);

        var detail = Assert.Single(model.SelectedArtifactDetails);
        Assert.Equal(artifact.Key, detail.Key);
        Assert.Collection(
            detail.Artifacts,
            request => Assert.Equal("Request", request.Label),
            reply => Assert.Equal("Reply", reply.Label));
    }

    [Fact]
    public async Task OnGetGroupsLegacyCommandRequestAndReplyAsSingleLogicalArtifact()
    {
        var definitionId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var repository = new InMemoryContractArtifactRepository(
            CreateArtifact(definitionId, versionId, ContractArtifactType.CommandRequest, "Customer Register Request"),
            CreateArtifact(definitionId, versionId, ContractArtifactType.CommandReply, "Customer Register Reply"));
        var model = new IndexModel(repository)
        {
            GroupKey = $"Command:{definitionId:N}"
        };

        await model.OnGetAsync(CancellationToken.None);

        var artifact = Assert.Single(model.SelectedArtifacts);
        Assert.Equal("Command", artifact.ContractKind);
        Assert.Equal("Customer Register", artifact.Name);
        Assert.Equal("Request and reply schemas", artifact.SchemaSummary);
        Assert.Equal(2, artifact.Artifacts.Count);

        var detail = Assert.Single(model.SelectedArtifactDetails);
        Assert.Equal(artifact.Key, detail.Key);
        Assert.Collection(
            detail.Artifacts,
            request => Assert.Equal("Request", request.Label),
            reply => Assert.Equal("Reply", reply.Label));
    }

    private static ContractArtifact CreateArtifact(
        Guid definitionId,
        Guid versionId,
        ContractArtifactType artifactType,
        string name)
        => new()
        {
            Id = Guid.NewGuid(),
            ArtifactType = artifactType,
            DefinitionId = definitionId,
            VersionId = versionId,
            Name = name,
            Topic = "customer.register",
            VersionNumber = "1.0.0",
            Description = "Registers a customer.",
            PayloadSchemaJson = "{}",
            ContentHash = Guid.NewGuid().ToString("N"),
            SourceStatus = "Deployed",
            CreatedAtUtc = DateTime.UtcNow
        };

    private static ContractArtifact CreateCommandArtifact(Guid definitionId, Guid versionId)
        => new()
        {
            Id = Guid.NewGuid(),
            ArtifactType = ContractArtifactType.Command,
            DefinitionId = definitionId,
            VersionId = versionId,
            Name = "Customer Register",
            Topic = "customer.register",
            VersionNumber = "1.0.0",
            Description = "Registers a customer.",
            PayloadSchemaJson = CommandArtifactPayloadDocument.Compose(
                "{\"type\":\"object\",\"properties\":{\"customerId\":{\"type\":\"string\"}}}",
                "{\"type\":\"object\",\"properties\":{\"accepted\":{\"type\":\"boolean\"}}}"),
            ContentHash = Guid.NewGuid().ToString("N"),
            SourceStatus = "Deployed",
            CreatedAtUtc = DateTime.UtcNow
        };

    private sealed class InMemoryContractArtifactRepository(params ContractArtifact[] artifacts) : IContractArtifactRepository
    {
        private readonly IReadOnlyList<ContractArtifact> artifacts = artifacts;

        public Task<IReadOnlyList<ContractArtifact>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult(artifacts);

        public Task<IReadOnlyList<ContractArtifact>> GetDeployed(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ContractArtifact>>(artifacts.Where(IsDeployed).ToArray());

        public Task<ContractArtifact?> GetById(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(artifacts.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<ContractArtifact>> GetByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ContractArtifact>>(artifacts.Where(x => ids.Contains(x.Id)).ToArray());

        public Task<ContractArtifact?> GetBySourceVersion(ContractArtifactType artifactType, Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult(artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.VersionId == versionId));

        public Task<ContractArtifact?> GetByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));

        public Task<ContractArtifact?> GetDeployedByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(artifacts.FirstOrDefault(x => IsDeployed(x) && x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));

        public Task<ContractArtifact?> GetLatestDeployed(ContractArtifactType artifactType, string topic, CancellationToken cancellationToken = default)
            => Task.FromResult(artifacts.Where(x => IsDeployed(x) && x.ArtifactType == artifactType && x.Topic == topic).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault());

        public Task Create(ContractArtifact artifact, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        private static bool IsDeployed(ContractArtifact artifact)
            => string.Equals(artifact.SourceStatus, "Deployed", StringComparison.OrdinalIgnoreCase);
    }
}
