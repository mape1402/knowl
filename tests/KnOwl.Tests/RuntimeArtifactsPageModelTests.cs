using KnOwl.Contracts.Artifacts;
using KnOwl.Runtime.Application.Catalog;
using KnOwl.Runtime.Core;
using RuntimeArtifactsIndexModel = KnOwl.Runtime.WebUI.Pages.Artifacts.IndexModel;

namespace KnOwl.Tests;

public sealed class RuntimeArtifactsPageModelTests
{
    [Fact]
    public void ArtifactTypesExposeOnlyEventAndCommand()
    {
        var model = new RuntimeArtifactsIndexModel(new InMemoryRuntimeContractCatalogService());

        Assert.Equal(["Event", "Command"], model.ArtifactTypes);
    }

    [Fact]
    public async Task OnGetGroupsLegacyCommandRequestAndReplyAsSingleCommand()
    {
        var definitionId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var model = new RuntimeArtifactsIndexModel(new InMemoryRuntimeContractCatalogService(
            CreateArtifact(definitionId, versionId, ContractArtifactType.CommandRequest, "Customer Register Request"),
            CreateArtifact(definitionId, versionId, ContractArtifactType.CommandReply, "Customer Register Reply")));

        await model.OnGet(CancellationToken.None);

        var artifact = Assert.Single(model.FilteredArtifacts);
        Assert.Equal("Command", artifact.ArtifactType);
        Assert.Equal("Customer Register", artifact.Name);
        Assert.Equal(1, model.TotalArtifacts);
    }

    [Fact]
    public async Task OnGetCommandFilterIncludesLogicalCommandArtifacts()
    {
        var model = new RuntimeArtifactsIndexModel(new InMemoryRuntimeContractCatalogService(
            CreateArtifact(Guid.NewGuid(), Guid.NewGuid(), ContractArtifactType.Event, "Customer Registered", topic: "customer.registered"),
            CreateArtifact(Guid.NewGuid(), Guid.NewGuid(), ContractArtifactType.Command, "Customer Register", topic: "customer.register")))
        {
            Type = "Command"
        };

        await model.OnGet(CancellationToken.None);

        var artifact = Assert.Single(model.FilteredArtifacts);
        Assert.Equal("Command", artifact.ArtifactType);
        Assert.Equal("Command", model.Type);
        Assert.Equal(2, model.TotalArtifacts);
    }

    [Fact]
    public async Task OnGetEventFilterSearchesByHashAndTrimsQuery()
    {
        var matching = CreateArtifact(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ContractArtifactType.Event,
            "Shipment Created",
            topic: "shipment.created");
        matching.ContentHash = "abc123";
        var ignored = CreateArtifact(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ContractArtifactType.Command,
            "Shipment Create",
            topic: "shipment.create");
        ignored.ContentHash = "xyz789";
        var model = new RuntimeArtifactsIndexModel(new InMemoryRuntimeContractCatalogService(matching, ignored))
        {
            Type = "event",
            Query = " abc123 "
        };

        await model.OnGet(CancellationToken.None);

        var artifact = Assert.Single(model.FilteredArtifacts);
        Assert.Equal("Event", artifact.ArtifactType);
        Assert.Equal("abc123", artifact.ContentHash);
        Assert.Equal("Event", model.Type);
        Assert.Equal("abc123", model.Query);
    }

    [Fact]
    public async Task OnGetInvalidTypeClearsTypeAndSearchesByVersion()
    {
        var artifact = CreateArtifact(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ContractArtifactType.Event,
            "Billing Event",
            topic: "billing.event",
            versionNumber: "2.1.0");
        var model = new RuntimeArtifactsIndexModel(new InMemoryRuntimeContractCatalogService(artifact))
        {
            Type = "Reply",
            Query = "2.1.0"
        };

        await model.OnGet(CancellationToken.None);

        Assert.Single(model.FilteredArtifacts);
        Assert.Null(model.Type);
    }

    [Fact]
    public async Task OnGetNormalizesLegacyCommandReplyName()
    {
        var definitionId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var model = new RuntimeArtifactsIndexModel(new InMemoryRuntimeContractCatalogService(
            CreateArtifact(definitionId, versionId, ContractArtifactType.CommandReply, "Customer Register Reply")));

        await model.OnGet(CancellationToken.None);

        var artifact = Assert.Single(model.FilteredArtifacts);
        Assert.Equal("Command", artifact.ArtifactType);
        Assert.Equal("Customer Register", artifact.Name);
    }

    private static RuntimeContractArtifact CreateArtifact(
        Guid definitionId,
        Guid versionId,
        ContractArtifactType artifactType,
        string name,
        string topic = "customer.register",
        string versionNumber = "1.0.0")
        => new()
        {
            Id = Guid.NewGuid(),
            SourceArtifactId = Guid.NewGuid(),
            SourceReleaseId = Guid.NewGuid(),
            ArtifactType = artifactType,
            DefinitionId = definitionId,
            VersionId = versionId,
            Name = name,
            Topic = topic,
            VersionNumber = versionNumber,
            Description = "Runtime artifact.",
            PayloadSchemaJson = "{}",
            ContentHash = Guid.NewGuid().ToString("N"),
            DeployedAtUtc = DateTime.UtcNow
        };

    private sealed class InMemoryRuntimeContractCatalogService(params RuntimeContractArtifact[] artifacts) : IRuntimeContractCatalogService
    {
        public Task<IReadOnlyList<RuntimeContractArtifact>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RuntimeContractArtifact>>(artifacts);

        public Task<RuntimeContractArtifact?> GetExact(
            ContractArtifactType artifactType,
            string topic,
            string versionNumber,
            CancellationToken cancellationToken = default)
            => Task.FromResult(artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));

        public Task<RuntimeContractArtifact?> GetLatest(
            ContractArtifactType artifactType,
            string topic,
            CancellationToken cancellationToken = default)
            => Task.FromResult(artifacts.Where(x => x.ArtifactType == artifactType && x.Topic == topic).OrderByDescending(x => x.DeployedAtUtc).FirstOrDefault());

        public Task<RuntimeContractArtifact?> GetEvent(
            string eventKey,
            string versionNumber,
            CancellationToken cancellationToken = default)
            => GetExact(ContractArtifactType.Event, eventKey, versionNumber, cancellationToken);

        public async Task<CommandContractArtifacts<RuntimeContractArtifact>?> GetCommand(
            string commandKey,
            string versionNumber,
            CancellationToken cancellationToken = default)
        {
            var command = await GetExact(ContractArtifactType.Command, commandKey, versionNumber, cancellationToken);
            if (command is not null)
            {
                return new CommandContractArtifacts<RuntimeContractArtifact>(commandKey, versionNumber, command, null);
            }

            var request = await GetExact(ContractArtifactType.CommandRequest, commandKey, versionNumber, cancellationToken);
            if (request is null)
            {
                return null;
            }

            var reply = await GetExact(ContractArtifactType.CommandReply, commandKey, versionNumber, cancellationToken);
            return new CommandContractArtifacts<RuntimeContractArtifact>(commandKey, versionNumber, request, reply);
        }
    }
}
