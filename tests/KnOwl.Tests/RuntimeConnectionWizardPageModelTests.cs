using KnOwl.Contracts.Distribution;
using KnOwl.ControlPlane.Application.Distribution.Security;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.Runtime.Application.Security;
using KnOwl.Runtime.Distribution;
using KnOwl.Runtime.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using ControlPlaneRuntimeNodesPage = KnOwl.ControlPlane.WebUI.Pages.Contracts.RuntimeNodes.IndexModel;
using RuntimeControlPlanesPage = KnOwl.Runtime.WebUI.Pages.ControlPlanes.IndexModel;
using RuntimeNodeInput = KnOwl.ControlPlane.WebUI.Pages.Contracts.RuntimeNodes.RuntimeNodeInput;
using ControlPlaneConnectionInput = KnOwl.Runtime.WebUI.Pages.ControlPlanes.ControlPlaneConnectionInput;

namespace KnOwl.Tests;

public sealed class RuntimeConnectionWizardPageModelTests
{
    [Fact]
    public async Task RuntimeControlPlaneWizardRejectsDuplicateKeyBeforeSaving()
    {
        var existing = new RuntimeDesignNode { Id = Guid.NewGuid(), Key = "Demo", Name = "Demo" };
        var repository = new RuntimeDesignNodeRepositoryFake([existing]);
        var model = CreateRuntimeControlPlanesPage(repository);
        model.Input = new ControlPlaneConnectionInput
        {
            Key = "Demo",
            Name = "Duplicate Control Plane",
            DistributionMode = DistributionMode.Hybrid,
            Description = "Duplicate validation"
        };

        var result = await model.OnPostWizardUpsertAsync(CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Control Plane connection key 'Demo' already exists.", ReadMessage(badRequest.Value));
        Assert.Single(repository.Nodes);
    }

    [Fact]
    public async Task RuntimeControlPlaneWizardSavesPendingConnectionWithoutRemoteEndpoint()
    {
        var repository = new RuntimeDesignNodeRepositoryFake([]);
        var model = CreateRuntimeControlPlanesPage(repository);
        model.Input = new ControlPlaneConnectionInput
        {
            Key = "runtime-control",
            Name = "Runtime Control",
            DistributionMode = DistributionMode.Hybrid,
            Description = "Pending wizard connection"
        };

        var result = await model.OnPostWizardUpsertAsync(CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        Assert.Equal("Control Plane connection saved.", ReadMessage(json.Value));
        var node = Assert.Single(repository.Nodes);
        Assert.Equal("runtime-control", node.Key);
        Assert.Equal(RuntimeDesignNodeStatus.Pending, node.Status);
        Assert.False(node.IsEnabled);
        Assert.Equal(string.Empty, node.EndpointBaseUri);
        Assert.Equal(string.Empty, node.RemoteRuntimeNodeId);
    }

    [Fact]
    public async Task ControlPlaneRuntimeNodeWizardRejectsDuplicateCodeBeforeSaving()
    {
        var environment = CreateEnvironment();
        var existing = new RuntimeNode
        {
            Id = Guid.NewGuid(),
            Name = "Demo",
            Code = "Demo",
            EnvironmentId = environment.Id,
            EnvironmentName = environment.Name,
            DistributionMode = DistributionMode.Pull
        };
        var runtimeNodes = new RuntimeNodeRepositoryFake([existing]);
        var model = CreateControlPlaneRuntimeNodesPage(runtimeNodes, [environment]);
        model.Input = new RuntimeNodeInput
        {
            Name = "Duplicate Runtime Node",
            Code = "Demo",
            EnvironmentId = environment.Id,
            DistributionMode = DistributionMode.Pull,
            Status = RuntimeNodeStatus.Active,
            IsEnabled = false
        };

        var result = await model.OnPostWizardUpsertAsync(CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Runtime node code 'Demo' already exists.", ReadMessage(badRequest.Value));
        Assert.Single(runtimeNodes.Nodes);
    }

    [Fact]
    public async Task ControlPlaneRuntimeNodeWizardSavesPendingPullNodeWithoutEndpoint()
    {
        var environment = CreateEnvironment();
        var runtimeNodes = new RuntimeNodeRepositoryFake([]);
        var model = CreateControlPlaneRuntimeNodesPage(runtimeNodes, [environment]);
        model.Input = new RuntimeNodeInput
        {
            Name = "Runtime Node",
            Code = "runtime-node",
            EnvironmentId = environment.Id,
            DistributionMode = DistributionMode.Pull,
            Status = RuntimeNodeStatus.Active,
            IsEnabled = false,
            Description = "Pending wizard node"
        };

        var result = await model.OnPostWizardUpsertAsync(CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        Assert.Equal("Runtime node saved.", ReadMessage(json.Value));
        var node = Assert.Single(runtimeNodes.Nodes);
        Assert.Equal("runtime-node", node.Code);
        Assert.Equal(environment.Id, node.EnvironmentId);
        Assert.Equal(environment.Name, node.EnvironmentName);
        Assert.Equal(DistributionMode.Pull, node.DistributionMode);
        Assert.False(node.IsEnabled);
        Assert.Equal(string.Empty, node.EndpointBaseUri);
    }

    [Fact]
    public async Task ControlPlaneRuntimeNodeDeleteSoftDeletesNode()
    {
        var environment = CreateEnvironment();
        var runtimeNode = new RuntimeNode
        {
            Id = Guid.NewGuid(),
            Name = "Runtime Node",
            Code = "runtime-node",
            EnvironmentId = environment.Id,
            EnvironmentName = environment.Name,
            DistributionMode = DistributionMode.Pull,
            Status = RuntimeNodeStatus.Active,
            IsEnabled = true
        };
        var runtimeNodes = new RuntimeNodeRepositoryFake([runtimeNode]);
        var model = CreateControlPlaneRuntimeNodesPage(runtimeNodes, [environment]);

        var result = await model.OnPostDeleteAsync(runtimeNode.Id, CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.True(runtimeNode.IsDeleted);
        Assert.False(runtimeNode.IsEnabled);
        Assert.NotNull(runtimeNode.DeletedAtUtc);
        Assert.Equal(runtimeNode.DeletedAtUtc, runtimeNode.LastUpdatedAtUtc);
        Assert.Equal("Runtime node deleted.", model.StatusMessage);
    }

    private static RuntimeControlPlanesPage CreateRuntimeControlPlanesPage(RuntimeDesignNodeRepositoryFake repository)
    {
        var model = new RuntimeControlPlanesPage(repository, new NoopRuntimeDesignNodeConnectionService());
        AttachPageContext(model);
        return model;
    }

    private static ControlPlaneRuntimeNodesPage CreateControlPlaneRuntimeNodesPage(
        RuntimeNodeRepositoryFake runtimeNodes,
        IReadOnlyList<RuntimeEnvironment> environments)
    {
        var model = new ControlPlaneRuntimeNodesPage(
            runtimeNodes,
            new RuntimeEnvironmentRepositoryFake(environments),
            new NoopRuntimeNodeConnectionInteractionService());
        AttachPageContext(model);
        return model;
    }

    private static RuntimeEnvironment CreateEnvironment()
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "Development",
            Code = "dev",
            IsEnabled = true
        };

    private static void AttachPageContext(PageModel model)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddOptions()
            .AddMvcCore()
            .Services
            .BuildServiceProvider();

        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services }
        };
    }

    private static string ReadMessage(object? value)
        => value?.GetType().GetProperty("message")?.GetValue(value)?.ToString()
           ?? throw new InvalidOperationException("The response did not include a message.");

    private sealed class RuntimeDesignNodeRepositoryFake(List<RuntimeDesignNode> nodes) : IRuntimeDesignNodeRepository
    {
        public List<RuntimeDesignNode> Nodes => nodes;

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

    private sealed class RuntimeNodeRepositoryFake(List<RuntimeNode> nodes) : IRuntimeNodeRepository
    {
        public List<RuntimeNode> Nodes => nodes;

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
            var node = nodes.Single(x => x.Id == id);
            node.IsEnabled = isEnabled;
            node.LastUpdatedAtUtc = updatedAtUtc;
            return Task.CompletedTask;
        }

        public Task Delete(Guid id, DateTime deletedAtUtc, CancellationToken cancellationToken = default)
        {
            var node = nodes.Single(x => x.Id == id);
            node.IsDeleted = true;
            node.DeletedAtUtc = deletedAtUtc;
            node.IsEnabled = false;
            node.LastUpdatedAtUtc = deletedAtUtc;
            return Task.CompletedTask;
        }
    }

    private sealed class RuntimeEnvironmentRepositoryFake(IReadOnlyList<RuntimeEnvironment> environments) : IRuntimeEnvironmentRepository
    {
        public Task<IReadOnlyList<RuntimeEnvironment>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult(environments);

        public Task<IReadOnlyList<RuntimeEnvironment>> GetEnabled(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RuntimeEnvironment>>(environments.Where(x => x.IsEnabled).ToList());

        public Task<RuntimeEnvironment?> GetById(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(environments.FirstOrDefault(x => x.Id == id));

        public Task Create(RuntimeEnvironment environment, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task Update(RuntimeEnvironment environment, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class NoopRuntimeDesignNodeConnectionService : IRuntimeDesignNodeConnectionService
    {
        public Task<RuntimeDesignNode> UpsertDesignNode(
            Guid? id,
            string key,
            string name,
            DistributionMode distributionMode,
            string endpointBaseUri,
            string remoteRuntimeNodeId,
            bool isEnabled,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new RuntimeDesignNode
            {
                Id = id.GetValueOrDefault(Guid.NewGuid()),
                Key = key,
                Name = name,
                DistributionMode = distributionMode,
                EndpointBaseUri = endpointBaseUri,
                RemoteRuntimeNodeId = remoteRuntimeNodeId,
                IsEnabled = isEnabled
            });

        public Task<RuntimeDesignNodeCredentialPackageModel> GenerateCredentialPackage(Guid designNodeId, string issuerBaseUrl, CancellationToken cancellationToken = default)
            => Task.FromResult(new RuntimeDesignNodeCredentialPackageModel());

        public Task ImportCredentialPackage(ImportRuntimeDesignNodeCredentialPackageInput input, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<RuntimeDesignNodeConnectionValidationModel> ValidateConnection(Guid designNodeId, CancellationToken cancellationToken = default)
            => Task.FromResult(new RuntimeDesignNodeConnectionValidationModel());
    }

    private sealed class NoopRuntimeNodeConnectionInteractionService : IRuntimeNodeConnectionInteractionService
    {
        public Task<RuntimeNodeCredentialPackageModel> GenerateCredentialPackage(Guid runtimeNodeId, string issuerBaseUrl, CancellationToken cancellationToken = default)
            => Task.FromResult(new RuntimeNodeCredentialPackageModel());

        public Task ImportCredentialPackage(ImportRuntimeNodeCredentialPackageInput input, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<RuntimeNodeConnectionValidationModel> ValidateConnection(Guid runtimeNodeId, CancellationToken cancellationToken = default)
            => Task.FromResult(new RuntimeNodeConnectionValidationModel());
    }
}
