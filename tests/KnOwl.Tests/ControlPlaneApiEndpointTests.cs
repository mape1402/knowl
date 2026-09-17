using System.Net;
using System.Net.Http.Json;
using KnOwl.ControlPlane.Api;
using KnOwl.ControlPlane.Api.Contracts;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Tests;

public sealed class ControlPlaneApiEndpointTests
{
    [Fact]
    public async Task CreateCommandEndpointStoresRequestAndReplyDefinitions()
    {
        CommandService commands = new();
        await using var app = await CreateApp(commands);
        using var http = CreateClient(app);

        using var response = await http.PostAsJsonAsync("/api/v1/control-plane/commands", new CreateCommandRequest(
            "Reserve Inventory",
            "inventories.reserve",
            "Reserve stock before checkout.",
            "1.0.0",
            "{\"type\":\"object\",\"properties\":{\"sku\":{\"type\":\"string\"}}}",
            "{\"type\":\"object\",\"properties\":{\"accepted\":{\"type\":\"boolean\"}}}",
            "Initial command contract."));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CommandDefinitionResponse>();
        Assert.NotNull(body);
        Assert.Equal("inventories.reserve", body.Topic);
        Assert.Single(body.Versions);
        Assert.Equal("{\"type\":\"object\",\"properties\":{\"sku\":{\"type\":\"string\"}}}", body.Versions[0].RequestDefinitionJson);
        Assert.Equal("{\"type\":\"object\",\"properties\":{\"accepted\":{\"type\":\"boolean\"}}}", body.Versions[0].ReplyDefinitionJson);
        Assert.Equal(body.Id, commands.Items.Single().Id);
    }

    [Fact]
    public async Task CommandEndpointReturnsNotFoundForUnknownCommand()
    {
        await using var app = await CreateApp(new CommandService());
        using var http = CreateClient(app);

        using var response = await http.GetAsync($"/api/v1/control-plane/commands/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<WebApplication> CreateApp(ICommandInteractionService commands)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(commands);

        var app = builder.Build();
        app.MapKnOwlControlPlaneApi();
        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()
            ?? throw new InvalidOperationException("The test server did not expose an address.");
        return new HttpClient { BaseAddress = new Uri(addresses.Addresses.Single()) };
    }

    private sealed class CommandService : ICommandInteractionService
    {
        public List<CommandDefinition> Items { get; } = [];

        public Task<IReadOnlyList<CommandDefinition>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CommandDefinition>>(Items);

        public Task<CommandDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));

        public Task<bool> VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.SingleOrDefault(x => x.Id == commandId)?.Versions.Any(x => x.VersionNumber == versionNumber) == true);

        public Task Create(CommandDefinition commandDefinition, CancellationToken cancellationToken = default)
        {
            foreach (var version in commandDefinition.Versions)
            {
                version.CommandDefinitionId = commandDefinition.Id;
                version.CommandDefinition = commandDefinition;
            }

            Items.Add(commandDefinition);
            return Task.CompletedTask;
        }

        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            var item = Items.Single(x => x.Id == id);
            item.Name = name;
            item.Topic = topic;
            item.Description = description;
            item.UpdatedAtUtc = updatedAtUtc;
            return Task.CompletedTask;
        }

        public Task AddVersion(Guid commandId, CommandVersion version, CancellationToken cancellationToken = default)
        {
            var item = Items.Single(x => x.Id == commandId);
            version.CommandDefinitionId = item.Id;
            version.CommandDefinition = item;
            item.Versions.Add(version);
            return Task.CompletedTask;
        }

        public Task Delete(Guid id, CancellationToken cancellationToken = default)
        {
            var item = Items.Single(x => x.Id == id);
            item.IsActive = false;
            return Task.CompletedTask;
        }
    }
}
