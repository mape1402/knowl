using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Application.Distribution.ArtifactDelivery;
using KnOwl.ControlPlane.Application.Distribution.Security;
using KnOwl.ControlPlane.Bootstrap.Distribution;
using KnOwl.Runtime.Application.ArtifactDelivery;
using KnOwl.Runtime.Application.Catalog;
using KnOwl.Runtime.Application.Deployments;
using KnOwl.Runtime.Application.Security;
using KnOwl.Runtime.Bootstrap.Runtime;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Distribution;
using KnOwl.Runtime.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Tests;

public sealed class BootstrapEndpointCoverageTests
{
    [Fact]
    public async Task RuntimeBootstrapEndpointsExecuteSuccessAndFailureBranches()
    {
        await using var fixture = await RuntimeBootstrapFixture.Start();
        var client = fixture.Client;

        await AssertStatus(client.PostAsJsonAsync("/runtime/distribution/connect/token", new ConnectionTokenRequest { ClientId = "good" }), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync("/runtime/distribution/connect/token", new ConnectionTokenRequest { ClientId = "bad" }), HttpStatusCode.BadRequest);

        using var validRequest = new HttpRequestMessage(HttpMethod.Get, "/runtime/distribution/connect/validate");
        validRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "valid");
        await AssertStatus(client.SendAsync(validRequest), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/runtime/distribution/connect/validate"), HttpStatusCode.Unauthorized);

        await AssertStatus(PostJson(client, "/runtime/artifacts/deploy", CreatePackage("customer.created")), HttpStatusCode.OK);
        await AssertStatus(PostJson(client, "/runtime/artifacts/deploy", CreatePackage("reject")), HttpStatusCode.BadRequest);
        await AssertStatus(client.PostAsync("/runtime/artifacts/deploy", new StringContent("null", Encoding.UTF8, "application/json")), HttpStatusCode.BadRequest);
        using var unauthorizedDeploy = new HttpRequestMessage(HttpMethod.Post, "/runtime/artifacts/deploy")
        {
            Content = JsonContent.Create(CreatePackage("customer.created"))
        };
        unauthorizedDeploy.Headers.Add("X-Auth", "fail");
        await AssertStatus(client.SendAsync(unauthorizedDeploy), HttpStatusCode.Unauthorized);

        await AssertStatus(client.GetAsync("/runtime/distribution/control-planes"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/runtime/distribution/control-planes/cp/artifacts/pending"), HttpStatusCode.OK);
        await AssertStatus(client.PostAsync($"/runtime/distribution/control-planes/cp/artifacts/{Guid.NewGuid()}/apply", null), HttpStatusCode.OK);
        await AssertStatus(client.PostAsync($"/runtime/distribution/control-planes/reject/artifacts/{Guid.NewGuid()}/apply", null), HttpStatusCode.BadRequest);

        await AssertStatus(client.GetAsync("/runtime/distribution/design-nodes"), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync("/runtime/distribution/design-nodes", new
        {
            key = "cp",
            name = "Control Plane",
            distributionMode = DistributionMode.Hybrid,
            endpointBaseUri = "https://control.example.test",
            remoteRuntimeNodeId = "runtime-1",
            isEnabled = true
        }), HttpStatusCode.OK);
        var designNodeId = fixture.State.Nodes.Single().Id;
        await AssertStatus(client.PostAsJsonAsync($"/runtime/distribution/design-nodes/{designNodeId}/credentials/generate", new { issuerBaseUrl = "https://runtime.example.test" }), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync($"/runtime/distribution/design-nodes/{designNodeId}/credentials/import", new { package = "{}" }), HttpStatusCode.OK);

        await AssertStatus(client.GetAsync("/runtime/contracts/artifacts"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/runtime/contracts/events/customer.created/versions/1.0.0"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/runtime/contracts/events/missing/versions/1.0.0"), HttpStatusCode.NotFound);
        await AssertStatus(client.GetAsync("/runtime/contracts/commands/customer.create/versions/1.0.0"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/runtime/contracts/commands/missing/versions/1.0.0"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ControlPlaneArtifactDeliveryBootstrapEndpointsExecuteSuccessAndFailureBranches()
    {
        await using var fixture = await ControlPlaneDeliveryBootstrapFixture.Start();
        var client = fixture.Client;
        var nodeId = fixture.State.RuntimeNodeId;

        await AssertStatus(client.PostAsJsonAsync("/distribution/connect/token", new ConnectionTokenRequest { ClientId = "good" }), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync("/distribution/connect/token", new ConnectionTokenRequest { ClientId = "bad" }), HttpStatusCode.BadRequest);

        using var validRequest = new HttpRequestMessage(HttpMethod.Get, $"/distribution/runtime-nodes/{nodeId}/connect/validate");
        validRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "valid");
        await AssertStatus(client.SendAsync(validRequest), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync($"/distribution/runtime-nodes/{nodeId}/connect/validate"), HttpStatusCode.Unauthorized);

        await AssertStatus(client.PostAsJsonAsync($"/distribution/runtime-nodes/{nodeId}/credentials/generate", new { issuerBaseUrl = "https://control.example.test" }), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync($"/distribution/runtime-nodes/{nodeId}/credentials/import", new { package = "{}" }), HttpStatusCode.OK);

        await AssertStatus(client.GetAsync($"/distribution/artifacts/runtime-nodes/{nodeId}/pending"), HttpStatusCode.OK);
        using var unauthorizedPending = new HttpRequestMessage(HttpMethod.Get, $"/distribution/artifacts/runtime-nodes/{nodeId}/pending");
        unauthorizedPending.Headers.Add("X-Auth", "fail");
        await AssertStatus(client.SendAsync(unauthorizedPending), HttpStatusCode.Unauthorized);

        await AssertStatus(client.GetAsync($"/distribution/artifacts/runtime-nodes/{nodeId}/targets/{fixture.State.TargetId}"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync($"/distribution/artifacts/runtime-nodes/{nodeId}/targets/{fixture.State.MissingTargetId}"), HttpStatusCode.NotFound);
        await AssertStatus(client.GetAsync($"/distribution/artifacts/runtime-nodes/{nodeId}/targets/{fixture.State.InvalidTargetId}"), HttpStatusCode.BadRequest);

        await AssertStatus(client.PostAsJsonAsync($"/distribution/artifacts/runtime-nodes/{nodeId}/targets/{fixture.State.TargetId}/ack", new RuntimeArtifactPullAckRequest { RuntimeArtifactId = "runtime-artifact", RuntimeArtifactStatus = "Ready" }), HttpStatusCode.OK);
        using var unauthorizedAck = new HttpRequestMessage(HttpMethod.Post, $"/distribution/artifacts/runtime-nodes/{nodeId}/targets/{fixture.State.TargetId}/ack")
        {
            Content = JsonContent.Create(new RuntimeArtifactPullAckRequest { RuntimeArtifactId = "runtime-artifact", RuntimeArtifactStatus = "Ready" })
        };
        unauthorizedAck.Headers.Add("X-Auth", "fail");
        await AssertStatus(client.SendAsync(unauthorizedAck), HttpStatusCode.Unauthorized);

        await AssertStatus(client.PostAsync($"/distribution/artifacts/targets/{fixture.State.TargetId}/push", null), HttpStatusCode.OK);
        await AssertStatus(client.PostAsync($"/distribution/artifacts/targets/{fixture.State.FailedPushTargetId}/push", null), HttpStatusCode.BadRequest);
    }

    private static Task<HttpResponseMessage> PostJson(HttpClient client, string url, RuntimeArtifactDeliveryPackage package)
        => client.PostAsync(url, new StringContent(JsonSerializer.Serialize(package, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8, "application/json"));

    private static async Task AssertStatus(Task<HttpResponseMessage> task, HttpStatusCode expected)
    {
        using var response = await task;
        Assert.Equal(expected, response.StatusCode);
    }

    private sealed class RuntimeBootstrapFixture : IAsyncDisposable
    {
        private readonly WebApplication app;

        private RuntimeBootstrapFixture(WebApplication app, HttpClient client, RuntimeBootstrapState state)
        {
            this.app = app;
            Client = client;
            State = state;
        }

        public HttpClient Client { get; }
        public RuntimeBootstrapState State { get; }

        public static async Task<RuntimeBootstrapFixture> Start()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            var state = new RuntimeBootstrapState();
            builder.Services.AddSingleton(state);
            builder.Services.AddSingleton<IRuntimeConnectionTokenIssuer>(state);
            builder.Services.AddSingleton<IRuntimeConnectionTokenValidator>(state);
            builder.Services.AddSingleton<IRuntimeArtifactDeliveryEndpointAuthenticator>(state);
            builder.Services.AddSingleton<IRuntimeContractDeploymentService>(state);
            builder.Services.AddSingleton<IControlPlaneArtifactPullService>(state);
            builder.Services.AddSingleton<IRuntimeDesignNodeRepository>(state);
            builder.Services.AddSingleton<IRuntimeDesignNodeConnectionService>(state);
            builder.Services.AddSingleton<IRuntimeContractCatalogService>(state);

            var app = builder.Build();
            app.MapKnOwlRuntimeEndpoints();
            await app.StartAsync();
            return new RuntimeBootstrapFixture(app, app.GetTestClient(), state);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }

    private sealed class ControlPlaneDeliveryBootstrapFixture : IAsyncDisposable
    {
        private readonly WebApplication app;

        private ControlPlaneDeliveryBootstrapFixture(WebApplication app, HttpClient client, ControlPlaneDeliveryBootstrapState state)
        {
            this.app = app;
            Client = client;
            State = state;
        }

        public HttpClient Client { get; }
        public ControlPlaneDeliveryBootstrapState State { get; }

        public static async Task<ControlPlaneDeliveryBootstrapFixture> Start()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            var state = new ControlPlaneDeliveryBootstrapState();
            builder.Services.AddSingleton(state);
            builder.Services.AddSingleton<IControlPlaneConnectionTokenIssuer>(state);
            builder.Services.AddSingleton<IControlPlaneConnectionTokenValidator>(state);
            builder.Services.AddSingleton<IRuntimeNodeConnectionInteractionService>(state);
            builder.Services.AddSingleton<IArtifactDeliveryEndpointAuthenticator>(state);
            builder.Services.AddSingleton<IArtifactDeliveryInteractionService>(state);

            var app = builder.Build();
            app.MapKnOwlArtifactDeliveryEndpoints();
            await app.StartAsync();
            return new ControlPlaneDeliveryBootstrapFixture(app, app.GetTestClient(), state);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }

    private sealed class RuntimeBootstrapState :
        IRuntimeConnectionTokenIssuer,
        IRuntimeConnectionTokenValidator,
        IRuntimeArtifactDeliveryEndpointAuthenticator,
        IRuntimeContractDeploymentService,
        IControlPlaneArtifactPullService,
        IRuntimeDesignNodeRepository,
        IRuntimeDesignNodeConnectionService,
        IRuntimeContractCatalogService
    {
        private readonly RuntimeContractArtifact eventArtifact = CreateArtifact(ContractArtifactType.Event, "customer.created");
        private readonly RuntimeContractArtifact commandArtifact = CreateArtifact(ContractArtifactType.Command, "customer.create");

        public List<RuntimeDesignNode> Nodes { get; } = [];

        Task<ConnectionTokenResponse> IRuntimeConnectionTokenIssuer.Issue(ConnectionTokenRequest request, CancellationToken cancellationToken)
        {
            if (request.ClientId == "bad")
            {
                throw new InvalidOperationException("Bad client");
            }

            return Task.FromResult(CreateToken());
        }

        Task<ConnectionTokenValidationResult> IRuntimeConnectionTokenValidator.Validate(string token, IReadOnlyCollection<ArtifactDeliveryScope> requiredScopes, CancellationToken cancellationToken)
            => Task.FromResult(token == "valid"
                ? ConnectionTokenValidationResult.Success(new ConnectionTokenPrincipal { NodeKey = "cp", ClientId = "client", Scopes = requiredScopes })
                : ConnectionTokenValidationResult.Failure("Invalid token"));

        Task<RuntimeArtifactDeliveryEndpointAuthenticationResult> IRuntimeArtifactDeliveryEndpointAuthenticator.Authenticate(HttpRequest request, string body, CancellationToken cancellationToken)
            => Task.FromResult(request.Headers["X-Auth"].ToString() == "fail"
                ? RuntimeArtifactDeliveryEndpointAuthenticationResult.Failure("No")
                : RuntimeArtifactDeliveryEndpointAuthenticationResult.Success("cp"));

        Task<RuntimeArtifactDeploymentResult> IRuntimeContractDeploymentService.DeployArtifact(RuntimeArtifactDeliveryPackage package, string sourceKey, CancellationToken cancellationToken)
            => Task.FromResult(new RuntimeArtifactDeploymentResult
            {
                Accepted = package.Topic != "reject",
                RuntimeArtifactId = "runtime-artifact",
                Status = package.Topic == "reject" ? "Rejected" : "Ready",
                Message = package.Topic == "reject" ? "Rejected" : "Accepted"
            });

        Task<IReadOnlyCollection<ControlPlaneDistributionSource>> IControlPlaneArtifactPullService.GetSources(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ControlPlaneDistributionSource>>([new() { Key = "cp", Name = "Control Plane", IsEnabled = true }]);

        Task<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>> IControlPlaneArtifactPullService.GetPending(string sourceKey, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>>([CreatePackage("customer.created")]);

        Task<RuntimeArtifactDeploymentResult> IControlPlaneArtifactPullService.Apply(string sourceKey, Guid releaseTargetId, CancellationToken cancellationToken)
            => Task.FromResult(new RuntimeArtifactDeploymentResult
            {
                Accepted = sourceKey != "reject",
                RuntimeArtifactId = "runtime-artifact",
                Status = sourceKey == "reject" ? "Rejected" : "Ready",
                Message = "Applied"
            });

        Task<RuntimeArtifactDeploymentResult> IControlPlaneArtifactPullService.ApplyPackage(string sourceKey, RuntimeArtifactDeliveryPackage package, CancellationToken cancellationToken)
            => Task.FromResult(new RuntimeArtifactDeploymentResult { Accepted = true, RuntimeArtifactId = "runtime-artifact", Status = "Ready" });

        Task<IReadOnlyList<RuntimeDesignNode>> IRuntimeDesignNodeRepository.GetAll(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<RuntimeDesignNode>>(Nodes);

        Task<RuntimeDesignNode?> IRuntimeDesignNodeRepository.GetById(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Nodes.FirstOrDefault(x => x.Id == id));

        Task<RuntimeDesignNode?> IRuntimeDesignNodeRepository.GetByKey(string key, CancellationToken cancellationToken)
            => Task.FromResult(Nodes.FirstOrDefault(x => x.Key == key));

        Task<RuntimeDesignNode?> IRuntimeDesignNodeRepository.GetByInboundClientId(string clientId, CancellationToken cancellationToken)
            => Task.FromResult(Nodes.FirstOrDefault(x => x.InboundClientId == clientId));

        Task IRuntimeDesignNodeRepository.Upsert(RuntimeDesignNode designNode, CancellationToken cancellationToken)
        {
            Nodes.RemoveAll(x => x.Id == designNode.Id);
            Nodes.Add(designNode);
            return Task.CompletedTask;
        }

        Task<RuntimeDesignNode> IRuntimeDesignNodeConnectionService.UpsertDesignNode(Guid? id, string key, string name, DistributionMode distributionMode, string endpointBaseUri, string remoteRuntimeNodeId, bool isEnabled, CancellationToken cancellationToken)
        {
            var node = Nodes.FirstOrDefault(x => id.HasValue && x.Id == id.Value) ?? new RuntimeDesignNode { Id = id ?? Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow };
            Nodes.RemoveAll(x => x.Id == node.Id);
            node.Key = key;
            node.Name = name;
            node.DistributionMode = distributionMode;
            node.EndpointBaseUri = endpointBaseUri;
            node.RemoteRuntimeNodeId = remoteRuntimeNodeId;
            node.IsEnabled = isEnabled;
            node.Status = isEnabled ? RuntimeDesignNodeStatus.Enabled : RuntimeDesignNodeStatus.Pending;
            Nodes.Add(node);
            return Task.FromResult(node);
        }

        Task<RuntimeDesignNodeCredentialPackageModel> IRuntimeDesignNodeConnectionService.GenerateCredentialPackage(Guid designNodeId, string issuerBaseUrl, CancellationToken cancellationToken)
            => Task.FromResult(new RuntimeDesignNodeCredentialPackageModel { Json = $"{{\"issuer\":\"{issuerBaseUrl}\"}}", Base64 = "e30=" });

        Task IRuntimeDesignNodeConnectionService.ImportCredentialPackage(ImportRuntimeDesignNodeCredentialPackageInput input, CancellationToken cancellationToken)
            => Task.CompletedTask;

        Task<RuntimeDesignNodeConnectionValidationModel> IRuntimeDesignNodeConnectionService.ValidateConnection(Guid designNodeId, CancellationToken cancellationToken)
            => Task.FromResult(new RuntimeDesignNodeConnectionValidationModel { Succeeded = true, Message = "OK" });

        Task<IReadOnlyList<RuntimeContractArtifact>> IRuntimeContractCatalogService.GetAll(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<RuntimeContractArtifact>>([eventArtifact, commandArtifact]);

        Task<RuntimeContractArtifact?> IRuntimeContractCatalogService.GetExact(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken)
            => Task.FromResult<RuntimeContractArtifact?>(eventArtifact.ArtifactType == artifactType && eventArtifact.Topic == topic ? eventArtifact : null);

        Task<RuntimeContractArtifact?> IRuntimeContractCatalogService.GetLatest(ContractArtifactType artifactType, string topic, CancellationToken cancellationToken)
            => Task.FromResult<RuntimeContractArtifact?>(eventArtifact.ArtifactType == artifactType && eventArtifact.Topic == topic ? eventArtifact : null);

        Task<RuntimeContractArtifact?> IRuntimeContractCatalogService.GetEvent(string eventKey, string versionNumber, CancellationToken cancellationToken)
            => Task.FromResult<RuntimeContractArtifact?>(eventKey == eventArtifact.Topic ? eventArtifact : null);

        Task<CommandContractArtifacts<RuntimeContractArtifact>?> IRuntimeContractCatalogService.GetCommand(string commandKey, string versionNumber, CancellationToken cancellationToken)
            => Task.FromResult<CommandContractArtifacts<RuntimeContractArtifact>?>(commandKey == commandArtifact.Topic ? new(commandKey, versionNumber, commandArtifact, null) : null);
    }

    private sealed class ControlPlaneDeliveryBootstrapState :
        IControlPlaneConnectionTokenIssuer,
        IControlPlaneConnectionTokenValidator,
        IRuntimeNodeConnectionInteractionService,
        IArtifactDeliveryEndpointAuthenticator,
        IArtifactDeliveryInteractionService
    {
        public Guid RuntimeNodeId { get; } = Guid.NewGuid();
        public Guid TargetId { get; } = Guid.NewGuid();
        public Guid MissingTargetId { get; } = Guid.NewGuid();
        public Guid InvalidTargetId { get; } = Guid.NewGuid();
        public Guid FailedPushTargetId { get; } = Guid.NewGuid();

        Task<ConnectionTokenResponse> IControlPlaneConnectionTokenIssuer.Issue(ConnectionTokenRequest request, CancellationToken cancellationToken)
        {
            if (request.ClientId == "bad")
            {
                throw new KeyNotFoundException("Missing client");
            }

            return Task.FromResult(CreateToken());
        }

        Task<ConnectionTokenValidationResult> IControlPlaneConnectionTokenValidator.Validate(string token, Guid runtimeNodeId, IReadOnlyCollection<ArtifactDeliveryScope> requiredScopes, CancellationToken cancellationToken)
            => Task.FromResult(token == "valid"
                ? ConnectionTokenValidationResult.Success(new ConnectionTokenPrincipal { NodeKey = "runtime", ClientId = "client", Scopes = requiredScopes })
                : ConnectionTokenValidationResult.Failure("Invalid token"));

        Task<RuntimeNodeCredentialPackageModel> IRuntimeNodeConnectionInteractionService.GenerateCredentialPackage(Guid runtimeNodeId, string issuerBaseUrl, CancellationToken cancellationToken)
            => Task.FromResult(new RuntimeNodeCredentialPackageModel { Json = $"{{\"issuer\":\"{issuerBaseUrl}\"}}", Base64 = "e30=" });

        Task IRuntimeNodeConnectionInteractionService.ImportCredentialPackage(ImportRuntimeNodeCredentialPackageInput input, CancellationToken cancellationToken)
            => Task.CompletedTask;

        Task<RuntimeNodeConnectionValidationModel> IRuntimeNodeConnectionInteractionService.ValidateConnection(Guid runtimeNodeId, CancellationToken cancellationToken)
            => Task.FromResult(new RuntimeNodeConnectionValidationModel { Succeeded = true, Message = "OK" });

        Task<ArtifactDeliveryEndpointAuthenticationResult> IArtifactDeliveryEndpointAuthenticator.AuthenticateRuntimeNode(HttpRequest request, Guid runtimeNodeId, string body, CancellationToken cancellationToken)
            => Task.FromResult(request.Headers["X-Auth"].ToString() == "fail"
                ? ArtifactDeliveryEndpointAuthenticationResult.Failure("No")
                : ArtifactDeliveryEndpointAuthenticationResult.Success());

        Task<RuntimeArtifactDeliveryResult> IArtifactDeliveryInteractionService.Push(Guid releaseTargetId, string initiatedBy, CancellationToken cancellationToken)
            => Task.FromResult(CreateDeliveryResult(releaseTargetId, releaseTargetId != FailedPushTargetId));

        Task<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>> IArtifactDeliveryInteractionService.GetPendingForPull(Guid runtimeNodeId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>>([CreatePackage("customer.created", RuntimeNodeId, TargetId)]);

        Task<RuntimeArtifactDeliveryPackage> IArtifactDeliveryInteractionService.GetForPull(Guid runtimeNodeId, Guid releaseTargetId, CancellationToken cancellationToken)
        {
            if (releaseTargetId == MissingTargetId)
            {
                throw new KeyNotFoundException();
            }

            if (releaseTargetId == InvalidTargetId)
            {
                throw new InvalidOperationException("Invalid target");
            }

            return Task.FromResult(CreatePackage("customer.created", runtimeNodeId, releaseTargetId));
        }

        Task<RuntimeArtifactDeliveryResult> IArtifactDeliveryInteractionService.AcknowledgePull(Guid runtimeNodeId, Guid releaseTargetId, string runtimeArtifactId, string runtimeArtifactStatus, CancellationToken cancellationToken)
            => Task.FromResult(CreateDeliveryResult(releaseTargetId, true, runtimeArtifactId, runtimeArtifactStatus));
    }

    private static RuntimeArtifactDeliveryPackage CreatePackage(string topic)
        => CreatePackage(topic, Guid.NewGuid(), Guid.NewGuid());

    private static RuntimeArtifactDeliveryPackage CreatePackage(string topic, Guid runtimeNodeId, Guid releaseTargetId)
        => new()
        {
            ReleaseTargetId = releaseTargetId,
            ReleaseId = Guid.NewGuid(),
            RuntimeNodeId = runtimeNodeId,
            ArtifactId = Guid.NewGuid(),
            ArtifactType = topic.Contains("command", StringComparison.OrdinalIgnoreCase) ? ContractArtifactType.Command : ContractArtifactType.Event,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Topic = topic,
            VersionNumber = "1.0.0",
            Name = topic,
            PayloadSchemaJson = "{}",
            ContentHash = "hash"
        };

    private static RuntimeContractArtifact CreateArtifact(ContractArtifactType type, string topic)
        => new()
        {
            Id = Guid.NewGuid(),
            SourceArtifactId = Guid.NewGuid(),
            SourceReleaseId = Guid.NewGuid(),
            ArtifactType = type,
            DefinitionId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Name = topic,
            Topic = topic,
            VersionNumber = "1.0.0",
            PayloadSchemaJson = "{}",
            ContentHash = "hash"
        };

    private static RuntimeArtifactDeliveryResult CreateDeliveryResult(Guid releaseTargetId, bool succeeded, string runtimeArtifactId = "runtime-artifact", string status = "Ready")
        => new()
        {
            Succeeded = succeeded,
            ReleaseTargetId = releaseTargetId,
            RuntimeNodeId = Guid.NewGuid(),
            ArtifactId = Guid.NewGuid(),
            RuntimeArtifactId = runtimeArtifactId,
            Status = succeeded ? status : "Failed",
            Message = succeeded ? "OK" : "Failed"
        };

    private static ConnectionTokenResponse CreateToken()
        => new()
        {
            AccessToken = "token",
            ExpiresIn = 3600,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            Scope = ArtifactDeliveryScope.ConnectionValidate.ToString(),
            KeyId = "key"
        };
}
