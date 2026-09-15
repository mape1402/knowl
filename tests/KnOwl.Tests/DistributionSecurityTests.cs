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
using Microsoft.Extensions.DependencyInjection;

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
}

