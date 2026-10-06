using KnOwl.Contracts.Distribution;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Application.Distribution;
using KnOwl.ControlPlane.Application.Distribution.Security;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.Runtime.Distribution;
using KnOwl.Runtime.Application;
using KnOwl.Runtime.Application.Security;
using KnOwl.Runtime.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Headers;

namespace KnOwl.Tests;

public sealed class DistributionSecurityTests
{
    [Fact]
    public async Task ControlPlaneIssuesAndValidatesRuntimeScopedToken()
    {
        var hasher = new Pbkdf2ConnectionSecretHasher();
        var runtimeNode = new RuntimeNode
        {
            Id = Guid.NewGuid(),
            Name = "Runtime Dev",
            Code = "runtime-dev",
            IsEnabled = true,
            Status = RuntimeNodeStatus.Active,
            AccessTokenTtlSeconds = 600,
            InboundClientId = "runtime-client",
            InboundKeyId = "runtime-key",
            InboundSecretHash = hasher.HashSecret("runtime-secret"),
            InboundAllowedScopes = "release:read artifact:ack connection:validate",
            InboundCredentialStatus = ConnectionCredentialStatus.Active
        };

        using var provider = new ServiceCollection()
            .AddSingleton<IRuntimeNodeRepository>(new ControlPlaneRuntimeNodeRepositoryFake([runtimeNode]))
            .AddKnOwlControlPlaneDistributionApplication()
            .BuildServiceProvider();

        var issuer = provider.GetRequiredService<IControlPlaneConnectionTokenIssuer>();
        var validator = provider.GetRequiredService<IControlPlaneConnectionTokenValidator>();

        var token = await issuer.Issue(new ConnectionTokenRequest
        {
            GrantType = "client_credentials",
            ClientId = "runtime-client",
            ClientSecret = "runtime-secret",
            Scope = "release:read artifact:ack"
        });

        var accepted = await validator.Validate(
            token.AccessToken,
            runtimeNode.Id,
            [ArtifactDeliveryScope.ReleaseRead, ArtifactDeliveryScope.ArtifactAcknowledge]);
        var rejected = await validator.Validate(
            token.AccessToken,
            runtimeNode.Id,
            [ArtifactDeliveryScope.ArtifactPush]);

        Assert.True(accepted.Succeeded);
        Assert.False(rejected.Succeeded);
        Assert.Equal(runtimeNode.Id.ToString("N"), accepted.Principal?.NodeId);
    }

    [Fact]
    public async Task RuntimeIssuesAndValidatesControlPlaneScopedToken()
    {
        var hasher = new Pbkdf2ConnectionSecretHasher();
        var designNode = new RuntimeDesignNode
        {
            Id = Guid.NewGuid(),
            Key = "knowl-control-plane",
            Name = "KnOwl Control Plane",
            IsEnabled = true,
            Status = RuntimeDesignNodeStatus.Enabled,
            AccessTokenTtlSeconds = 600,
            InboundClientId = "control-plane-client",
            InboundKeyId = "control-plane-key",
            InboundSecretHash = hasher.HashSecret("control-plane-secret"),
            InboundAllowedScopes = "artifact:push connection:validate",
            InboundCredentialStatus = ConnectionCredentialStatus.Active
        };

        using var provider = new ServiceCollection()
            .AddSingleton<IRuntimeDesignNodeRepository>(new RuntimeDesignNodeRepositoryFake([designNode]))
            .AddKnOwlRuntimeApplication()
            .BuildServiceProvider();

        var issuer = provider.GetRequiredService<IRuntimeConnectionTokenIssuer>();
        var validator = provider.GetRequiredService<IRuntimeConnectionTokenValidator>();

        var token = await issuer.Issue(new ConnectionTokenRequest
        {
            GrantType = "client_credentials",
            ClientId = "control-plane-client",
            ClientSecret = "control-plane-secret",
            Scope = "artifact:push"
        });

        var accepted = await validator.Validate(token.AccessToken, [ArtifactDeliveryScope.ArtifactPush]);
        var rejected = await validator.Validate(token.AccessToken, [ArtifactDeliveryScope.ReleaseRead]);

        Assert.True(accepted.Succeeded);
        Assert.False(rejected.Succeeded);
        Assert.Equal(designNode.Id.ToString("N"), accepted.Principal?.NodeId);
    }

    [Fact]
    public async Task CredentialPackagesCanBeGeneratedAndImportedByBothSides()
    {
        var runtimeNode = new RuntimeNode
        {
            Id = Guid.NewGuid(),
            Name = "Runtime Dev",
            Code = "runtime-dev",
            DistributionMode = DistributionMode.Hybrid,
            EndpointBaseUri = "https://runtime.example.test",
            IsEnabled = true,
            Status = RuntimeNodeStatus.Active
        };
        var designNode = new RuntimeDesignNode
        {
            Id = Guid.NewGuid(),
            Key = "knowl-control-plane",
            Name = "KnOwl Control Plane",
            EndpointBaseUri = "https://knowl.example.test",
            RemoteRuntimeNodeId = runtimeNode.Id.ToString("N"),
            DistributionMode = DistributionMode.Hybrid,
            IsEnabled = true,
            Status = RuntimeDesignNodeStatus.Enabled
        };

        var runtimeNodeRepository = new ControlPlaneRuntimeNodeRepositoryFake([runtimeNode]);
        var designNodeRepository = new RuntimeDesignNodeRepositoryFake([designNode]);

        using var controlPlane = new ServiceCollection()
            .AddSingleton<IRuntimeNodeRepository>(runtimeNodeRepository)
            .AddKnOwlControlPlaneDistributionApplication()
            .BuildServiceProvider();
        using var runtime = new ServiceCollection()
            .AddSingleton<IRuntimeDesignNodeRepository>(designNodeRepository)
            .AddKnOwlRuntimeApplication()
            .BuildServiceProvider();

        var controlPlaneConnection = controlPlane.GetRequiredService<IRuntimeNodeConnectionInteractionService>();
        var runtimeConnection = runtime.GetRequiredService<IRuntimeDesignNodeConnectionService>();

        var controlPlanePackage = await controlPlaneConnection.GenerateCredentialPackage(
            runtimeNode.Id,
            "https://knowl.example.test");
        await runtimeConnection.ImportCredentialPackage(new ImportRuntimeDesignNodeCredentialPackageInput
        {
            DesignNodeId = designNode.Id,
            Package = controlPlanePackage.Base64
        });

        var runtimePackage = await runtimeConnection.GenerateCredentialPackage(
            designNode.Id,
            "https://runtime.example.test");
        await controlPlaneConnection.ImportCredentialPackage(new ImportRuntimeNodeCredentialPackageInput
        {
            RuntimeNodeId = runtimeNode.Id,
            Package = runtimePackage.Json
        });

        Assert.Equal(ConnectionCredentialStatus.Active, runtimeNode.InboundCredentialStatus);
        Assert.Equal(ConnectionCredentialStatus.Active, runtimeNode.OutboundCredentialStatus);
        Assert.Equal(ConnectionCredentialStatus.Active, designNode.InboundCredentialStatus);
        Assert.Equal(ConnectionCredentialStatus.Active, designNode.OutboundCredentialStatus);
        Assert.NotEmpty(runtimeNode.InboundClientId);
        Assert.NotEmpty(runtimeNode.OutboundClientId);
        Assert.NotEmpty(designNode.InboundClientId);
        Assert.NotEmpty(designNode.OutboundClientId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            controlPlaneConnection.ImportCredentialPackage(new ImportRuntimeNodeCredentialPackageInput
            {
                RuntimeNodeId = runtimeNode.Id,
                Package = controlPlanePackage.Json
            }));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runtimeConnection.ImportCredentialPackage(new ImportRuntimeDesignNodeCredentialPackageInput
            {
                DesignNodeId = designNode.Id,
                Package = runtimePackage.Json
            }));
    }

    [Fact]
    public async Task RuntimeDesignNodeUpsertPersistsSelectedDistributionMode()
    {
        var repository = new RuntimeDesignNodeRepositoryFake([]);
        using var runtime = new ServiceCollection()
            .AddSingleton<IRuntimeDesignNodeRepository>(repository)
            .AddKnOwlRuntimeApplication()
            .BuildServiceProvider();

        var connection = runtime.GetRequiredService<IRuntimeDesignNodeConnectionService>();

        var node = await connection.UpsertDesignNode(
            null,
            "knowl-control-plane",
            "KnOwl Control Plane",
            DistributionMode.Pull,
            "https://knowl.example.test",
            Guid.NewGuid().ToString("N"),
            isEnabled: false);

        var stored = await repository.GetById(node.Id);
        Assert.NotNull(stored);
        Assert.Equal(DistributionMode.Pull, stored.DistributionMode);
        Assert.Equal(RuntimeDesignNodeStatus.Pending, stored.Status);
        Assert.False(stored.IsEnabled);
    }

    [Fact]
    public async Task RuntimeDesignNodeUpsertRejectsMissingRequiredValuesAndUpdatesExistingNode()
    {
        var existing = new RuntimeDesignNode
        {
            Id = Guid.NewGuid(),
            Key = "old",
            Name = "Old",
            Status = RuntimeDesignNodeStatus.Pending
        };
        var repository = new RuntimeDesignNodeRepositoryFake([existing]);
        using var runtime = new ServiceCollection()
            .AddSingleton<IRuntimeDesignNodeRepository>(repository)
            .AddKnOwlRuntimeApplication()
            .BuildServiceProvider();

        var connection = runtime.GetRequiredService<IRuntimeDesignNodeConnectionService>();

        await Assert.ThrowsAsync<ArgumentException>(() => connection.UpsertDesignNode(null, " ", "Name", DistributionMode.Pull, "", "", false));
        await Assert.ThrowsAsync<ArgumentException>(() => connection.UpsertDesignNode(null, "key", " ", DistributionMode.Pull, "", "", false));

        var node = await connection.UpsertDesignNode(
            existing.Id,
            " updated ",
            " Updated ",
            DistributionMode.Hybrid,
            " https://control.example.test/ ",
            " runtime-id ",
            isEnabled: true);

        Assert.Equal(existing.Id, node.Id);
        Assert.Equal("updated", node.Key);
        Assert.Equal("Updated", node.Name);
        Assert.Equal("https://control.example.test", node.EndpointBaseUri);
        Assert.Equal("runtime-id", node.RemoteRuntimeNodeId);
        Assert.Equal(RuntimeDesignNodeStatus.Enabled, node.Status);
    }

    [Fact]
    public async Task ControlPlaneRuntimeNodeConnectionValidationCoversSuccessAndPreconditions()
    {
        var node = new RuntimeNode
        {
            Id = Guid.NewGuid(),
            Name = "Runtime Dev",
            Code = "runtime-dev",
            DistributionMode = DistributionMode.Push,
            EndpointBaseUri = "https://runtime.example.test",
            IsEnabled = true,
            Status = RuntimeNodeStatus.Active,
            OutboundCredentialStatus = ConnectionCredentialStatus.Active
        };
        var repository = new ControlPlaneRuntimeNodeRepositoryFake([node]);
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var provider = CreateControlPlaneConnectionProvider(repository, handler);
        var connection = provider.GetRequiredService<IRuntimeNodeConnectionInteractionService>();

        var success = await connection.ValidateConnection(node.Id);

        Assert.True(success.Succeeded);
        Assert.EndsWith("/runtime/distribution/connect/validate", handler.Request!.RequestUri!.AbsoluteUri, StringComparison.Ordinal);

        node.EndpointBaseUri = "";
        var missingEndpoint = await connection.ValidateConnection(node.Id);
        node.EndpointBaseUri = "https://runtime.example.test";
        node.OutboundCredentialStatus = ConnectionCredentialStatus.Missing;
        var missingCredentials = await connection.ValidateConnection(node.Id);
        node.OutboundCredentialStatus = ConnectionCredentialStatus.Active;
        node.DistributionMode = DistributionMode.Pull;
        var wrongMode = await connection.ValidateConnection(node.Id);
        node.DistributionMode = DistributionMode.Push;
        node.IsDeleted = true;
        var deleted = await connection.ValidateConnection(node.Id);

        Assert.False(missingEndpoint.Succeeded);
        Assert.Contains("endpoint", missingEndpoint.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(missingCredentials.Succeeded);
        Assert.Contains("credentials", missingCredentials.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(wrongMode.Succeeded);
        Assert.Contains("Control Plane can call Runtime", wrongMode.Message);
        Assert.False(deleted.Succeeded);
        Assert.Contains("active and enabled", deleted.Message);
    }

    [Fact]
    public async Task RuntimeDesignNodeConnectionValidationCoversSuccessHttpErrorsAndPreconditions()
    {
        var node = new RuntimeDesignNode
        {
            Id = Guid.NewGuid(),
            Key = "control-plane",
            Name = "Control Plane",
            DistributionMode = DistributionMode.Pull,
            EndpointBaseUri = "https://control.example.test",
            RemoteRuntimeNodeId = "runtime-1",
            IsEnabled = true,
            Status = RuntimeDesignNodeStatus.Enabled,
            OutboundCredentialStatus = ConnectionCredentialStatus.Active
        };
        var repository = new RuntimeDesignNodeRepositoryFake([node]);
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("Control Plane unavailable")
        });
        using var provider = CreateRuntimeConnectionProvider(repository, handler);
        var connection = provider.GetRequiredService<IRuntimeDesignNodeConnectionService>();

        var httpFailure = await connection.ValidateConnection(node.Id);
        handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK);
        var success = await connection.ValidateConnection(node.Id);

        node.RemoteRuntimeNodeId = "";
        var missingRemote = await connection.ValidateConnection(node.Id);
        node.RemoteRuntimeNodeId = "runtime-1";
        node.EndpointBaseUri = "";
        var missingEndpoint = await connection.ValidateConnection(node.Id);
        node.EndpointBaseUri = "https://control.example.test";
        node.OutboundCredentialStatus = ConnectionCredentialStatus.Missing;
        var missingCredentials = await connection.ValidateConnection(node.Id);
        node.OutboundCredentialStatus = ConnectionCredentialStatus.Active;
        node.DistributionMode = DistributionMode.Push;
        var wrongMode = await connection.ValidateConnection(node.Id);
        node.DistributionMode = DistributionMode.Pull;
        node.IsEnabled = false;
        var disabled = await connection.ValidateConnection(node.Id);

        Assert.False(httpFailure.Succeeded);
        Assert.Equal("Control Plane unavailable", httpFailure.Message);
        Assert.True(success.Succeeded);
        Assert.Contains("/distribution/runtime-nodes/runtime-1/connect/validate", handler.Request!.RequestUri!.AbsoluteUri);
        Assert.False(missingRemote.Succeeded);
        Assert.Contains("Remote runtime node id", missingRemote.Message);
        Assert.False(missingEndpoint.Succeeded);
        Assert.Contains("endpoint", missingEndpoint.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(missingCredentials.Succeeded);
        Assert.Contains("credentials", missingCredentials.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(wrongMode.Succeeded);
        Assert.Contains("Runtime can call Control Plane", wrongMode.Message);
        Assert.False(disabled.Succeeded);
        Assert.Contains("enabled", disabled.Message);
    }

    [Fact]
    public async Task ArtifactEndpointAuthenticatorsResolveBearerTokensAndRouteScopes()
    {
        var runtimeNodeId = Guid.NewGuid();
        var controlPlaneValidator = new RecordingControlPlaneTokenValidator();
        var controlPlaneAuthenticator = new ArtifactDeliveryEndpointAuthenticator(controlPlaneValidator);
        var pendingRequest = CreateRequest(HttpMethods.Get, "/distribution/artifacts/runtime-nodes/runtime/pending", "Bearer runtime-token ");
        var readRequest = CreateRequest(HttpMethods.Get, "/distribution/artifacts/runtime-nodes/runtime/targets/id", "not-a-bearer");
        var ackRequest = CreateRequest(HttpMethods.Post, "/distribution/artifacts/runtime-nodes/runtime/targets/id/ack", "Bearer ack-token");

        var pending = await controlPlaneAuthenticator.AuthenticateRuntimeNode(pendingRequest, runtimeNodeId, string.Empty);
        controlPlaneValidator.Next = ConnectionTokenValidationResult.Failure("Denied");
        var read = await controlPlaneAuthenticator.AuthenticateRuntimeNode(readRequest, runtimeNodeId, string.Empty);
        controlPlaneValidator.Next = ConnectionTokenValidationResult.Success(new ConnectionTokenPrincipal());
        var ack = await controlPlaneAuthenticator.AuthenticateRuntimeNode(ackRequest, runtimeNodeId, string.Empty);

        var runtimeValidator = new RecordingRuntimeTokenValidator
        {
            Next = ConnectionTokenValidationResult.Success(new ConnectionTokenPrincipal { NodeKey = "control-plane" })
        };
        var runtimeAuthenticator = new RuntimeArtifactDeliveryEndpointAuthenticator(runtimeValidator);
        var push = await runtimeAuthenticator.Authenticate(CreateRequest(HttpMethods.Post, "/runtime/distribution/artifacts", "Bearer control-token"), string.Empty);
        runtimeValidator.Next = ConnectionTokenValidationResult.Failure("Nope");
        var pushFailure = await runtimeAuthenticator.Authenticate(CreateRequest(HttpMethods.Post, "/runtime/distribution/artifacts", ""), string.Empty);

        Assert.True(pending.Succeeded);
        Assert.False(read.Succeeded);
        Assert.True(ack.Succeeded);
        Assert.Equal("runtime-token", controlPlaneValidator.Calls[0].Token);
        Assert.Contains(ArtifactDeliveryScope.ReleaseRead, controlPlaneValidator.Calls[0].Scopes);
        Assert.Equal(string.Empty, controlPlaneValidator.Calls[1].Token);
        Assert.Contains(ArtifactDeliveryScope.ArtifactRead, controlPlaneValidator.Calls[1].Scopes);
        Assert.Contains(ArtifactDeliveryScope.ArtifactAcknowledge, controlPlaneValidator.Calls[2].Scopes);
        Assert.True(push.Succeeded);
        Assert.Equal("control-plane", push.SourceKey);
        Assert.False(pushFailure.Succeeded);
        Assert.Contains(ArtifactDeliveryScope.ArtifactPush, runtimeValidator.Calls[0].Scopes);
        Assert.Equal(string.Empty, runtimeValidator.Calls[1].Token);
    }

    private static ServiceProvider CreateControlPlaneConnectionProvider(
        IRuntimeNodeRepository repository,
        HttpMessageHandler handler)
    {
        var services = new ServiceCollection()
            .AddSingleton(repository);
        services.AddKnOwlControlPlaneDistributionApplication();
        services.AddHttpClient("KnOwlRuntimeDistribution")
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        services.RemoveAll<IRuntimeAccessTokenProvider>();
        services.AddSingleton<IRuntimeAccessTokenProvider, StubRuntimeAccessTokenProvider>();
        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateRuntimeConnectionProvider(
        IRuntimeDesignNodeRepository repository,
        HttpMessageHandler handler)
    {
        var services = new ServiceCollection()
            .AddSingleton(repository);
        services.AddKnOwlRuntimeApplication();
        services.AddHttpClient("KnOwlControlPlaneDistribution")
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        services.RemoveAll<IControlPlaneAccessTokenProvider>();
        services.AddSingleton<IControlPlaneAccessTokenProvider, StubControlPlaneAccessTokenProvider>();
        return services.BuildServiceProvider();
    }

    private static HttpRequest CreateRequest(string method, string path, string authorization)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Headers.Authorization = authorization;
        return context.Request;
    }

    private sealed class ControlPlaneRuntimeNodeRepositoryFake(List<RuntimeNode> nodes) : IRuntimeNodeRepository
    {
        public Task<IReadOnlyList<RuntimeNode>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RuntimeNode>>(nodes);

        public Task<IReadOnlyList<RuntimeNode>> GetActiveEnabled(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RuntimeNode>>(nodes.Where(x => x.IsEnabled && x.Status == RuntimeNodeStatus.Active).ToList());

        public Task<RuntimeNode?> GetById(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(nodes.FirstOrDefault(x => x.Id == id));

        public Task<RuntimeNode?> GetByCode(string code, CancellationToken cancellationToken = default)
            => Task.FromResult(nodes.FirstOrDefault(x => x.Code == code.Trim()));

        public Task<RuntimeNode?> GetByInboundClientId(string clientId, CancellationToken cancellationToken = default)
            => Task.FromResult(nodes.FirstOrDefault(x => x.InboundClientId == clientId.Trim()));

        public Task Create(RuntimeNode runtimeNode, CancellationToken cancellationToken = default)
        {
            nodes.Add(runtimeNode);
            return Task.CompletedTask;
        }

        public Task Update(RuntimeNode runtimeNode, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SetIsEnabled(Guid id, bool isEnabled, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            var node = nodes.FirstOrDefault(x => x.Id == id)
                ?? throw new KeyNotFoundException($"Runtime node '{id}' was not found.");
            node.IsEnabled = isEnabled;
            node.LastUpdatedAtUtc = updatedAtUtc;
            return Task.CompletedTask;
        }

        public Task Delete(Guid id, DateTime deletedAtUtc, CancellationToken cancellationToken = default)
        {
            var node = nodes.FirstOrDefault(x => x.Id == id)
                ?? throw new KeyNotFoundException($"Runtime node '{id}' was not found.");
            node.IsDeleted = true;
            node.DeletedAtUtc = deletedAtUtc;
            node.IsEnabled = false;
            node.LastUpdatedAtUtc = deletedAtUtc;
            return Task.CompletedTask;
        }
    }

    private sealed class RuntimeDesignNodeRepositoryFake(List<RuntimeDesignNode> nodes) : IRuntimeDesignNodeRepository
    {
        public Task<IReadOnlyList<RuntimeDesignNode>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RuntimeDesignNode>>(nodes);

        public Task<RuntimeDesignNode?> GetById(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(nodes.FirstOrDefault(x => x.Id == id));

        public Task<RuntimeDesignNode?> GetByKey(string key, CancellationToken cancellationToken = default)
            => Task.FromResult(nodes.FirstOrDefault(x => x.Key == key.Trim()));

        public Task<RuntimeDesignNode?> GetByInboundClientId(string clientId, CancellationToken cancellationToken = default)
            => Task.FromResult(nodes.FirstOrDefault(x => x.InboundClientId == clientId.Trim()));

        public Task Upsert(RuntimeDesignNode designNode, CancellationToken cancellationToken = default)
        {
            var index = nodes.FindIndex(x => x.Id == designNode.Id);
            if (index >= 0)
            {
                nodes[index] = designNode;
            }
            else
            {
                nodes.Add(designNode);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class StubRuntimeAccessTokenProvider : IRuntimeAccessTokenProvider
    {
        public Task AttachToken(
            HttpRequestMessage request,
            RuntimeNode node,
            IReadOnlyCollection<ArtifactDeliveryScope> scopes,
            CancellationToken cancellationToken = default)
        {
            Assert.Contains(ArtifactDeliveryScope.ConnectionValidate, scopes);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "runtime-token");
            return Task.CompletedTask;
        }
    }

    private sealed class StubControlPlaneAccessTokenProvider : IControlPlaneAccessTokenProvider
    {
        public Task AttachToken(
            HttpRequestMessage request,
            ControlPlaneDistributionSource source,
            IReadOnlyCollection<ArtifactDeliveryScope> scopes,
            CancellationToken cancellationToken = default)
        {
            Assert.Contains(ArtifactDeliveryScope.ConnectionValidate, scopes);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "control-plane-token");
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } = responder;
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(Responder(request));
        }
    }

    private sealed class RecordingControlPlaneTokenValidator : IControlPlaneConnectionTokenValidator
    {
        public List<(string Token, Guid RuntimeNodeId, IReadOnlyCollection<ArtifactDeliveryScope> Scopes)> Calls { get; } = [];
        public ConnectionTokenValidationResult Next { get; set; } =
            ConnectionTokenValidationResult.Success(new ConnectionTokenPrincipal());

        public Task<ConnectionTokenValidationResult> Validate(
            string token,
            Guid runtimeNodeId,
            IReadOnlyCollection<ArtifactDeliveryScope> requiredScopes,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((token, runtimeNodeId, requiredScopes));
            return Task.FromResult(Next);
        }
    }

    private sealed class RecordingRuntimeTokenValidator : IRuntimeConnectionTokenValidator
    {
        public List<(string Token, IReadOnlyCollection<ArtifactDeliveryScope> Scopes)> Calls { get; } = [];
        public ConnectionTokenValidationResult Next { get; set; } =
            ConnectionTokenValidationResult.Success(new ConnectionTokenPrincipal());

        public Task<ConnectionTokenValidationResult> Validate(
            string token,
            IReadOnlyCollection<ArtifactDeliveryScope> requiredScopes,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((token, requiredScopes));
            return Task.FromResult(Next);
        }
    }
}

