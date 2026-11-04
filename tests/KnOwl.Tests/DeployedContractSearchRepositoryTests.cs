using System.Text.Json;
using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using KnOwl.ControlPlane.Storage.EntityFramework.Distribution.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.Tests;

public sealed class DeployedContractSearchRepositoryTests
{
    [Fact]
    public async Task SearchFiltersCaseInsensitivelyAndReturnsOnlySummaries()
    {
        await using var db = CreateContext();
        var matching = CreateArtifact(
            ContractArtifactType.Event,
            "customers.created",
            "1.2.0",
            "aBcDef123456",
            DateTime.UtcNow.AddMinutes(-1));
        db.ContractArtifacts.AddRange(
            matching,
            CreateArtifact(ContractArtifactType.Event, "orders.created", "1.0.0", "f00d", DateTime.UtcNow),
            CreateArtifact(ContractArtifactType.Event, "draft.customer", "1.0.0", "draft", DateTime.UtcNow, ContractVersionStatus.Draft));
        await db.SaveChangesAsync();
        var repository = new ContractArtifactRepository(db);

        var byKey = await repository.SearchDeployed(new DeployedContractSearchQuery { Text = "CUSTOM" });
        var byVersion = await repository.SearchDeployed(new DeployedContractSearchQuery { Text = "2.0" });
        var byHash = await repository.SearchDeployed(new DeployedContractSearchQuery { Text = "def123" });
        var byIdentifier = await repository.SearchDeployed(new DeployedContractSearchQuery { Text = matching.Id.ToString() });

        Assert.Equal(matching.Id, Assert.Single(byKey).Id);
        Assert.Equal(matching.Id, Assert.Single(byVersion).Id);
        Assert.Equal(matching.Id, Assert.Single(byHash).Id);
        var result = Assert.Single(byIdentifier);
        Assert.Equal(matching.Id, result.Id);
        Assert.Equal(ContractArtifactType.Event, result.ArtifactType);
        Assert.True(result.HasUsableSchema);
        Assert.DoesNotContain("PayloadSchemaJson", JsonSerializer.Serialize(result), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchGroupsCommandsBeforeApplyingTheLimitAndKeepsExplicitLegacyParts()
    {
        await using var db = CreateContext();
        var now = DateTime.UtcNow;
        var legacyRequest = CreateArtifact(ContractArtifactType.CommandRequest, "customers.register", "1.0.0", "legacy-request", now.AddMinutes(-2));
        var legacyReply = CreateArtifact(ContractArtifactType.CommandReply, "customers.register", "1.0.0", "legacy-reply", now.AddMinutes(-1));
        var modernCommand = CreateArtifact(ContractArtifactType.Command, "orders.submit", "2.0.0", "modern-command", now);
        var supersededModernCommand = CreateArtifact(ContractArtifactType.Command, "payments.submit", "1.0.0", "canonical-command", now.AddMinutes(-4));
        var duplicateLegacyRequest = CreateArtifact(ContractArtifactType.CommandRequest, "payments.submit", "1.0.0", "obsolete-request", now.AddMinutes(-3));
        var duplicateLegacyReply = CreateArtifact(ContractArtifactType.CommandReply, "payments.submit", "1.0.0", "obsolete-reply", now.AddMinutes(-2));
        db.ContractArtifacts.AddRange(
            CreateArtifact(ContractArtifactType.Event, "inventory.updated", "3.0.0", "event", now.AddMinutes(-5)),
            legacyRequest,
            legacyReply,
            modernCommand,
            supersededModernCommand,
            duplicateLegacyRequest,
            duplicateLegacyReply);
        await db.SaveChangesAsync();
        var repository = new ContractArtifactRepository(db);

        var all = await repository.SearchDeployed(new DeployedContractSearchQuery { Limit = 100 });
        var limited = await repository.SearchDeployed(new DeployedContractSearchQuery { Limit = 2 });
        var commands = await repository.SearchDeployed(new DeployedContractSearchQuery { ArtifactType = ContractArtifactType.Command, Limit = 100 });
        var requests = await repository.SearchDeployed(new DeployedContractSearchQuery { ArtifactType = ContractArtifactType.CommandRequest, Limit = 100 });
        var replies = await repository.SearchDeployed(new DeployedContractSearchQuery { ArtifactType = ContractArtifactType.CommandReply, Limit = 100 });

        Assert.Collection(all,
            result => Assert.Equal(modernCommand.Id, result.Id),
            result => Assert.Equal(legacyRequest.Id, result.Id),
            result => Assert.Equal(supersededModernCommand.Id, result.Id),
            result => Assert.Equal("inventory.updated", result.ContractKey));
        Assert.Equal(2, limited.Count);
        Assert.All(commands, result => Assert.Equal(ContractArtifactType.Command, result.ArtifactType));
        Assert.Contains(commands, result => result.Id == legacyRequest.Id && result.ArtifactType == ContractArtifactType.Command);
        Assert.DoesNotContain(commands, result => result.Id == duplicateLegacyRequest.Id);
        Assert.Equal(ContractArtifactType.CommandRequest, Assert.Single(requests, x => x.Id == legacyRequest.Id).ArtifactType);
        Assert.Equal(ContractArtifactType.CommandReply, Assert.Single(replies, x => x.Id == legacyReply.Id).ArtifactType);
    }

    [Fact]
    public async Task SearchUsesDefaultAndMaximumLimitsAfterFiltering()
    {
        await using var db = CreateContext();
        var now = DateTime.UtcNow;
        for (var index = 0; index < DeployedContractSearchQuery.MaximumLimit + 5; index++)
        {
            db.ContractArtifacts.Add(CreateArtifact(
                ContractArtifactType.Event,
                $"orders.created.{index:D3}",
                "1.0.0",
                $"hash-{index:D3}",
                now.AddSeconds(-index)));
        }

        await db.SaveChangesAsync();
        var repository = new ContractArtifactRepository(db);

        var defaultResults = await repository.SearchDeployed(new DeployedContractSearchQuery());
        var maximumResults = await repository.SearchDeployed(new DeployedContractSearchQuery { Limit = 999 });

        Assert.Equal(DeployedContractSearchQuery.DefaultLimit, defaultResults.Count);
        Assert.Equal(DeployedContractSearchQuery.MaximumLimit, maximumResults.Count);
        Assert.Equal("orders.created.000", defaultResults[0].ContractKey);
    }

    private static KnOwlDbContext CreateContext()
        => new(new DbContextOptionsBuilder<KnOwlDbContext>()
            .UseInMemoryDatabase($"deployed-contract-search-{Guid.NewGuid():N}")
            .Options);

    private static ContractArtifact CreateArtifact(
        ContractArtifactType artifactType,
        string key,
        string version,
        string hash,
        DateTime createdAtUtc,
        ContractVersionStatus status = ContractVersionStatus.Deployed)
        => new()
        {
            Id = Guid.NewGuid(),
            ArtifactType = artifactType,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Name = key,
            ContractKey = key,
            VersionNumber = version,
            PayloadSchemaJson = "{\"type\":\"object\"}",
            ContentHash = hash,
            SourceStatus = status.ToString(),
            CreatedAtUtc = createdAtUtc
        };
}
