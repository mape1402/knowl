using System.Net;
using System.Net.Http.Json;
using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Application.Distribution.Catalog;
using KnOwl.ControlPlane.Bootstrap.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Tests;

public sealed class ControlPlaneContractCatalogEndpointTests
{
    [Fact]
    public async Task EventEndpointReturnsDeployedArtifact()
    {
        var expected = CreateArtifact("customer.created", "1.1.0");
        await using var app = await CreateApp(new CatalogService(eventArtifact: expected));
        using var http = CreateClient(app);

        using var response = await http.GetAsync("/contracts/events/customer.created/versions/1.1.0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var artifact = await response.Content.ReadFromJsonAsync<ContractArtifact>();
        Assert.Equal(expected.Id, artifact?.Id);
        Assert.Equal(expected.PayloadSchemaJson, artifact?.PayloadSchemaJson);
    }

    [Fact]
    public async Task EventLatestEndpointReturnsLatestDeployedArtifactWithContractKey()
    {
        var expected = CreateArtifact("customer.created", "1.2.0");
        await using var app = await CreateApp(new CatalogService(latest: expected));
        using var http = CreateClient(app);

        using var response = await http.GetAsync("/contracts/events/customer.created/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"contractKey\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"topic\"", json, StringComparison.OrdinalIgnoreCase);
        var artifact = await response.Content.ReadFromJsonAsync<ContractArtifact>();
        Assert.Equal(expected.Id, artifact?.Id);
        Assert.Equal("customer.created", artifact?.ContractKey);
    }

    [Fact]
    public async Task ExactEndpointReturnsNotFoundWhenArtifactIsNotDeployed()
    {
        await using var app = await CreateApp(new CatalogService());
        using var http = CreateClient(app);

        using var response = await http.GetAsync("/contracts/events/customer.created/versions/1.0.0");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EventLatestEndpointReturnsNotFoundWhenArtifactIsMissing()
    {
        await using var app = await CreateApp(new CatalogService());
        using var http = CreateClient(app);

        using var response = await http.GetAsync("/contracts/events/customer.created/latest");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CommandEndpointReturnsRequestAndOptionalReply()
    {
        var request = CreateArtifact("customer.register", "1.0.0", ContractArtifactType.CommandRequest);
        var reply = CreateArtifact("customer.register", "1.0.0", ContractArtifactType.CommandReply);
        await using var app = await CreateApp(new CatalogService(commandArtifacts: new CommandContractArtifacts<ContractArtifact>("customer.register", "1.0.0", request, reply)));
        using var http = CreateClient(app);

        using var response = await http.GetAsync("/contracts/commands/customer.register/versions/1.0.0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var artifacts = await response.Content.ReadFromJsonAsync<CommandContractArtifacts<ContractArtifact>>();
        Assert.Equal(request.Id, artifacts?.RequestArtifact.Id);
        Assert.Equal(reply.Id, artifacts?.ReplyArtifact?.Id);
    }

    [Fact]
    public async Task CommandLatestEndpointReturnsRequestAndOptionalReply()
    {
        var request = CreateArtifact("customer.register", "1.1.0", ContractArtifactType.CommandRequest);
        var reply = CreateArtifact("customer.register", "1.1.0", ContractArtifactType.CommandReply);
        await using var app = await CreateApp(new CatalogService(latestCommandArtifacts: new CommandContractArtifacts<ContractArtifact>("customer.register", "1.1.0", request, reply)));
        using var http = CreateClient(app);

        using var response = await http.GetAsync("/contracts/commands/customer.register/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"contractKey\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"topic\"", json, StringComparison.OrdinalIgnoreCase);
        var artifacts = await response.Content.ReadFromJsonAsync<CommandContractArtifacts<ContractArtifact>>();
        Assert.Equal("customer.register", artifacts?.CommandKey);
        Assert.Equal("1.1.0", artifacts?.Version);
        Assert.Equal(request.Id, artifacts?.RequestArtifact.Id);
        Assert.Equal(reply.Id, artifacts?.ReplyArtifact?.Id);
    }

    [Fact]
    public async Task CommandLatestEndpointReturnsNotFoundWhenCommandIsMissing()
    {
        await using var app = await CreateApp(new CatalogService());
        using var http = CreateClient(app);

        using var response = await http.GetAsync("/contracts/commands/customer.register/latest");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<WebApplication> CreateApp(IControlPlaneContractCatalogService catalog)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(catalog);

        var app = builder.Build();
        app.MapKnOwlControlPlaneContractCatalogEndpoints();
        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()
            ?? throw new InvalidOperationException("The test server did not expose an address.");
        return new HttpClient { BaseAddress = new Uri(addresses.Addresses.Single()) };
    }

    private static ContractArtifact CreateArtifact(string topic, string version, ContractArtifactType artifactType = ContractArtifactType.Event)
    {
        return new ContractArtifact
        {
            Id = Guid.NewGuid(),
            ArtifactType = artifactType,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Name = topic,
            ContractKey = topic,
            VersionNumber = version,
            PayloadSchemaJson = "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"string\"}}}",
            ContentHash = Guid.NewGuid().ToString("N"),
            SourceStatus = "Deployed",
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private sealed class CatalogService(
        IReadOnlyList<ContractArtifact>? all = null,
        ContractArtifact? exact = null,
        ContractArtifact? latest = null,
        ContractArtifact? eventArtifact = null,
        CommandContractArtifacts<ContractArtifact>? commandArtifacts = null,
        CommandContractArtifacts<ContractArtifact>? latestCommandArtifacts = null) : IControlPlaneContractCatalogService
    {
        public Task<IReadOnlyList<ContractArtifact>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult(all ?? []);

        public Task<ContractArtifact?> GetExact(
            ContractArtifactType artifactType,
            string topic,
            string versionNumber,
            CancellationToken cancellationToken = default)
            => Task.FromResult(exact);

        public Task<ContractArtifact?> GetLatest(
            ContractArtifactType artifactType,
            string topic,
            CancellationToken cancellationToken = default)
            => Task.FromResult(latest);

        public Task<ContractArtifact?> GetEvent(
            string eventKey,
            string versionNumber,
            CancellationToken cancellationToken = default)
            => Task.FromResult(eventArtifact);

        public Task<CommandContractArtifacts<ContractArtifact>?> GetCommand(
            string commandKey,
            string versionNumber,
            CancellationToken cancellationToken = default)
            => Task.FromResult(commandArtifacts);

        public Task<CommandContractArtifacts<ContractArtifact>?> GetLatestCommand(
            string commandKey,
            CancellationToken cancellationToken = default)
            => Task.FromResult(latestCommandArtifacts);
    }
}
