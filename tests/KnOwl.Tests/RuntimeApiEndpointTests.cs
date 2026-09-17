using System.Net;
using System.Net.Http.Json;
using KnOwl.Contracts.Artifacts;
using KnOwl.Runtime.Api;
using KnOwl.Runtime.Api.Contracts;
using KnOwl.Runtime.Application.Catalog;
using KnOwl.Runtime.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Tests;

public sealed class RuntimeApiEndpointTests
{
    [Fact]
    public async Task RuntimeCommandEndpointReturnsRequestAndReplyArtifacts()
    {
        var request = CreateArtifact("inventories.reserve", "1.0.0", ContractArtifactType.CommandRequest);
        var reply = CreateArtifact("inventories.reserve", "1.0.0", ContractArtifactType.CommandReply);
        await using var app = await CreateApp(new CatalogService(commandArtifacts: new CommandContractArtifacts<RuntimeContractArtifact>(
            "inventories.reserve",
            "1.0.0",
            request,
            reply)));
        using var http = CreateClient(app);

        using var response = await http.GetAsync("/api/v1/runtime/artifacts/commands/inventories.reserve/versions/1.0.0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RuntimeCommandArtifactsResponse>();
        Assert.NotNull(body);
        Assert.Equal(request.Id, body.RequestArtifact.Id);
        Assert.Equal(reply.Id, body.ReplyArtifact?.Id);
    }

    [Fact]
    public async Task RuntimeEventEndpointReturnsNotFoundWhenMissing()
    {
        await using var app = await CreateApp(new CatalogService());
        using var http = CreateClient(app);

        using var response = await http.GetAsync("/api/v1/runtime/artifacts/events/customer.created/versions/1.0.0");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<WebApplication> CreateApp(IRuntimeContractCatalogService catalog)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(catalog);

        var app = builder.Build();
        app.MapKnOwlRuntimeApi();
        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()
            ?? throw new InvalidOperationException("The test server did not expose an address.");
        return new HttpClient { BaseAddress = new Uri(addresses.Addresses.Single()) };
    }

    private static RuntimeContractArtifact CreateArtifact(string topic, string version, ContractArtifactType artifactType)
    {
        return new RuntimeContractArtifact
        {
            Id = Guid.NewGuid(),
            SourceArtifactId = Guid.NewGuid(),
            SourceReleaseId = Guid.NewGuid(),
            ArtifactType = artifactType,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Name = topic,
            Topic = topic,
            VersionNumber = version,
            PayloadSchemaJson = "{\"type\":\"object\"}",
            ContentHash = Guid.NewGuid().ToString("N"),
            DeployedAtUtc = DateTime.UtcNow
        };
    }

    private sealed class CatalogService(
        IReadOnlyList<RuntimeContractArtifact>? all = null,
        RuntimeContractArtifact? exact = null,
        RuntimeContractArtifact? latest = null,
        RuntimeContractArtifact? eventArtifact = null,
        CommandContractArtifacts<RuntimeContractArtifact>? commandArtifacts = null) : IRuntimeContractCatalogService
    {
        public Task<IReadOnlyList<RuntimeContractArtifact>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult(all ?? []);

        public Task<RuntimeContractArtifact?> GetExact(
            ContractArtifactType artifactType,
            string topic,
            string versionNumber,
            CancellationToken cancellationToken = default)
            => Task.FromResult(exact);

        public Task<RuntimeContractArtifact?> GetLatest(
            ContractArtifactType artifactType,
            string topic,
            CancellationToken cancellationToken = default)
            => Task.FromResult(latest);

        public Task<RuntimeContractArtifact?> GetEvent(
            string eventKey,
            string versionNumber,
            CancellationToken cancellationToken = default)
            => Task.FromResult(eventArtifact);

        public Task<CommandContractArtifacts<RuntimeContractArtifact>?> GetCommand(
            string commandKey,
            string versionNumber,
            CancellationToken cancellationToken = default)
            => Task.FromResult(commandArtifacts);
    }
}
