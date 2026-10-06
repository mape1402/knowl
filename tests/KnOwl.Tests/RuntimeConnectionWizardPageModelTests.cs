using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Application.Distribution.Security;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.Runtime.Application.Security;
using KnOwl.Runtime.Distribution;
using KnOwl.Runtime.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using ControlPlaneRuntimeNodesPage = KnOwl.ControlPlane.WebUI.Pages.Contracts.RuntimeNodes.IndexModel;
using RuntimeControlPlanesPage = KnOwl.Runtime.WebUI.Pages.ControlPlanes.IndexModel;
using RuntimeNodeInput = KnOwl.ControlPlane.WebUI.Pages.Contracts.RuntimeNodes.RuntimeNodeInput;
using RuntimeNodeCredentialImportInput = KnOwl.ControlPlane.WebUI.Pages.Contracts.RuntimeNodes.RuntimeNodeCredentialImportInput;
using RuntimeNodeCredentialInput = KnOwl.ControlPlane.WebUI.Pages.Contracts.RuntimeNodes.RuntimeNodeCredentialInput;
using ControlPlaneConnectionInput = KnOwl.Runtime.WebUI.Pages.ControlPlanes.ControlPlaneConnectionInput;
using RuntimeCredentialImportInput = KnOwl.Runtime.WebUI.Pages.ControlPlanes.RuntimeCredentialImportInput;

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
    public async Task RuntimeControlPlaneWizardActionsCoverCredentialsValidationAndEnabledState()
    {
        var readyNode = new RuntimeDesignNode
        {
            Id = Guid.NewGuid(),
            Key = "control-plane",
            Name = "Control Plane",
            DistributionMode = DistributionMode.Hybrid,
            EndpointBaseUri = "https://control.example.test",
            RemoteRuntimeNodeId = "runtime-1",
            Status = RuntimeDesignNodeStatus.Enabled,
            IsEnabled = true,
            InboundCredentialStatus = ConnectionCredentialStatus.Active,
            OutboundCredentialStatus = ConnectionCredentialStatus.Active,
            InboundClientId = "inbound-client",
            InboundKeyId = "inbound-key",
            InboundAllowedScopes = "artifact:push",
            InboundCredentialCreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            OutboundClientId = "outbound-client",
            OutboundKeyId = "outbound-key",
            OutboundRequestedScopes = "release:read",
            OutboundCredentialImportedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            Description = "Configured connection"
        };
        var pendingNode = new RuntimeDesignNode
        {
            Id = Guid.NewGuid(),
            Key = "pending",
            Name = "Pending",
            DistributionMode = DistributionMode.Pull,
            Status = RuntimeDesignNodeStatus.Pending,
            IsEnabled = false
        };
        var repository = new RuntimeDesignNodeRepositoryFake([readyNode, pendingNode]);
        var connectionService = new RecordingRuntimeDesignNodeConnectionService();
        var model = CreateRuntimeControlPlanesPage(repository, connectionService);

        Assert.IsType<BadRequestObjectResult>(await model.OnPostWizardGenerateCredentialsAsync(Guid.Empty, CancellationToken.None));

        var generated = Assert.IsType<JsonResult>(await model.OnPostWizardGenerateCredentialsAsync(readyNode.Id, CancellationToken.None));
        Assert.Equal("Runtime credentials generated.", ReadMessage(generated.Value));
        Assert.Equal("http://localhost", connectionService.LastIssuerBaseUrl);

        model.CredentialImportInput = new RuntimeCredentialImportInput
        {
            DesignNodeId = readyNode.Id,
            Package = null!
        };
        Assert.IsType<BadRequestObjectResult>(await model.OnPostWizardImportCredentialsAsync(CancellationToken.None));

        model.CredentialImportInput = new RuntimeCredentialImportInput
        {
            DesignNodeId = readyNode.Id,
            Package = "credential-package"
        };
        var imported = Assert.IsType<JsonResult>(await model.OnPostWizardImportCredentialsAsync(CancellationToken.None));
        Assert.Equal("Control Plane credentials imported.", ReadMessage(imported.Value));
        Assert.Equal("credential-package", connectionService.LastImportedPackage);

        var validation = Assert.IsType<JsonResult>(await model.OnPostWizardValidateConnectionAsync(readyNode.Id, CancellationToken.None));
        Assert.Equal("Connection OK", ReadMessage(validation.Value));

        var redirectValidation = Assert.IsType<RedirectToPageResult>(await model.OnPostValidateConnectionAsync(readyNode.Id, CancellationToken.None));
        Assert.Null(redirectValidation.PageName);
        Assert.Equal("Connection OK", model.StatusMessage);
        Assert.IsType<BadRequestObjectResult>(await model.OnPostWizardValidateConnectionAsync(Guid.Empty, CancellationToken.None));

        var rejectedEnable = Assert.IsType<BadRequestObjectResult>(await model.OnPostWizardSetEnabledAsync(pendingNode.Id, true, CancellationToken.None));
        Assert.Equal("Complete the required credentials before enabling this Control Plane connection.", ReadMessage(rejectedEnable.Value));

        var disabled = Assert.IsType<JsonResult>(await model.OnPostWizardSetEnabledAsync(readyNode.Id, false, CancellationToken.None));
        Assert.Equal("Control Plane connection suspended.", ReadMessage(disabled.Value));
        Assert.False(readyNode.IsEnabled);
        Assert.Equal(RuntimeDesignNodeStatus.Suspended, readyNode.Status);

        var enabled = Assert.IsType<RedirectToPageResult>(await model.OnPostSetEnabledAsync(readyNode.Id, true, CancellationToken.None));
        Assert.Null(enabled.PageName);
        Assert.True(readyNode.IsEnabled);
        Assert.Equal(RuntimeDesignNodeStatus.Enabled, readyNode.Status);
        Assert.Equal("Control Plane connection enabled.", model.StatusMessage);

        var missing = Assert.IsType<RedirectToPageResult>(await model.OnPostSetEnabledAsync(Guid.NewGuid(), false, CancellationToken.None));
        Assert.Null(missing.PageName);
        Assert.Contains("was not found", model.StatusMessage);
        Assert.IsType<BadRequestObjectResult>(await model.OnPostWizardSetEnabledAsync(Guid.Empty, true, CancellationToken.None));
    }

    [Fact]
    public async Task RuntimeControlPlanesSearchFiltersConnectionsByPolicyAndEndpoint()
    {
        var repository = new RuntimeDesignNodeRepositoryFake(
        [
            new RuntimeDesignNode
            {
                Id = Guid.NewGuid(),
                Key = "pull-node",
                Name = "Pull Node",
                DistributionMode = DistributionMode.Pull,
                EndpointBaseUri = string.Empty
            },
            new RuntimeDesignNode
            {
                Id = Guid.NewGuid(),
                Key = "push-node",
                Name = "Push Node",
                DistributionMode = DistributionMode.Push,
                EndpointBaseUri = "https://control-plane.example"
            }
        ]);
        var model = CreateRuntimeControlPlanesPage(repository);
        model.Search = "control-plane.example";

        await model.OnGet(CancellationToken.None);

        var node = Assert.Single(model.DesignNodes);
        Assert.Equal("Push Node", node.Name);
        Assert.Equal(2, model.TotalDesignNodes);
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
    public async Task ControlPlaneRuntimeNodeActionsCoverCredentialsValidationAndEnabledState()
    {
        var environment = CreateEnvironment();
        var readyNode = new RuntimeNode
        {
            Id = Guid.NewGuid(),
            Name = "Runtime",
            Code = "runtime",
            EnvironmentId = environment.Id,
            EnvironmentName = environment.Name,
            DistributionMode = DistributionMode.Hybrid,
            EndpointBaseUri = "https://runtime.example.test",
            Status = RuntimeNodeStatus.Active,
            IsEnabled = false,
            InboundCredentialStatus = ConnectionCredentialStatus.Active,
            OutboundCredentialStatus = ConnectionCredentialStatus.Active,
            InboundClientId = "cp-client",
            InboundKeyId = "cp-key",
            InboundAllowedScopes = "release:read",
            InboundCredentialCreatedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            OutboundClientId = "runtime-client",
            OutboundKeyId = "runtime-key",
            OutboundRequestedScopes = "artifact:push",
            OutboundCredentialImportedAtUtc = DateTime.UtcNow.AddMinutes(-20),
            Description = "Configured runtime"
        };
        var pendingNode = new RuntimeNode
        {
            Id = Guid.NewGuid(),
            Name = "Pending",
            Code = "pending",
            EnvironmentId = environment.Id,
            EnvironmentName = environment.Name,
            DistributionMode = DistributionMode.Push,
            Status = RuntimeNodeStatus.Active,
            IsEnabled = false
        };
        var runtimeNodes = new RuntimeNodeRepositoryFake([readyNode, pendingNode]);
        var connectionService = new RecordingRuntimeNodeConnectionInteractionService();
        var model = CreateControlPlaneRuntimeNodesPage(runtimeNodes, [environment], connectionService);

        model.CredentialInput = new RuntimeNodeCredentialInput { RuntimeNodeId = Guid.Empty };
        Assert.IsType<NotFoundResult>(await model.OnPostGenerateCredentialsAsync(CancellationToken.None));

        model.CredentialInput = new RuntimeNodeCredentialInput { RuntimeNodeId = readyNode.Id };
        var generated = Assert.IsType<JsonResult>(await model.OnPostGenerateCredentialsAsync(CancellationToken.None));
        Assert.Equal("Control Plane credentials generated.", ReadMessage(generated.Value));
        Assert.Equal("http://localhost", connectionService.LastIssuerBaseUrl);

        model.CredentialImportInput = new RuntimeNodeCredentialImportInput
        {
            RuntimeNodeId = Guid.Empty,
            Package = "runtime-package"
        };
        Assert.IsType<NotFoundResult>(await model.OnPostImportCredentialsAsync(CancellationToken.None));

        model.CredentialImportInput = new RuntimeNodeCredentialImportInput
        {
            RuntimeNodeId = readyNode.Id,
            Package = null!
        };
        var importValidation = Assert.IsType<BadRequestObjectResult>(await model.OnPostImportCredentialsAsync(CancellationToken.None));
        Assert.Contains("Package", ReadMessage(importValidation.Value));

        model.CredentialImportInput = new RuntimeNodeCredentialImportInput
        {
            RuntimeNodeId = readyNode.Id,
            Package = "runtime-package"
        };
        var imported = Assert.IsType<JsonResult>(await model.OnPostImportCredentialsAsync(CancellationToken.None));
        Assert.Equal("Runtime credentials imported.", ReadMessage(imported.Value));
        Assert.Equal("runtime-package", connectionService.LastImportedPackage);

        Assert.IsType<NotFoundResult>(await model.OnPostValidateConnectionAsync(Guid.Empty, CancellationToken.None));

        model.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
        var ajaxValidation = Assert.IsType<JsonResult>(await model.OnPostValidateConnectionAsync(readyNode.Id, CancellationToken.None));
        Assert.Equal("Runtime OK", ReadMessage(ajaxValidation.Value));

        model.Request.Headers.Remove("X-Requested-With");
        var redirectValidation = Assert.IsType<RedirectToPageResult>(await model.OnPostValidateConnectionAsync(readyNode.Id, CancellationToken.None));
        Assert.Null(redirectValidation.PageName);
        Assert.Equal("Runtime OK", model.StatusMessage);

        var rejectedEnable = Assert.IsType<RedirectToPageResult>(await model.OnPostSetEnabledAsync(pendingNode.Id, true, CancellationToken.None));
        Assert.Null(rejectedEnable.PageName);
        Assert.Equal("Runtime endpoint is required for Push or Hybrid nodes.", model.StatusMessage);

        var enabled = Assert.IsType<RedirectToPageResult>(await model.OnPostSetEnabledAsync(readyNode.Id, true, CancellationToken.None));
        Assert.Null(enabled.PageName);
        Assert.True(readyNode.IsEnabled);
        Assert.Equal("Runtime node enabled.", model.StatusMessage);

        var disabled = Assert.IsType<JsonResult>(await model.OnPostWizardSetEnabledAsync(readyNode.Id, false, CancellationToken.None));
        Assert.Equal("Runtime node disabled.", ReadMessage(disabled.Value));
        Assert.False(readyNode.IsEnabled);

        Assert.IsType<NotFoundResult>(await model.OnPostWizardSetEnabledAsync(Guid.Empty, true, CancellationToken.None));
        var deleteMissing = Assert.IsType<RedirectToPageResult>(await model.OnPostDeleteAsync(Guid.NewGuid(), CancellationToken.None));
        Assert.Null(deleteMissing.PageName);
        Assert.Contains("was not found", model.StatusMessage);
    }

    [Fact]
    public void RuntimeNodeInputMapsNewExistingAndEmptyDescription()
    {
        var environment = CreateEnvironment();
        var input = new RuntimeNodeInput
        {
            Name = " Runtime ",
            Code = " runtime ",
            EnvironmentId = environment.Id,
            DistributionMode = DistributionMode.Hybrid,
            EndpointBaseUri = " https://runtime.example.test/ ",
            Status = RuntimeNodeStatus.Active,
            IsEnabled = true,
            Description = "   "
        };

        var created = input.ToEntity(environment);
        Assert.Equal("Runtime", created.Name);
        Assert.Equal("runtime", created.Code);
        Assert.Null(created.Description);
        Assert.Equal("https://runtime.example.test/", created.EndpointBaseUri);

        input.Description = " Updated ";
        input.ApplyTo(created, environment);
        Assert.Equal("Updated", created.Description);

        var roundTrip = RuntimeNodeInput.FromEntity(created);
        Assert.Equal(created.Id, roundTrip.Id);
        Assert.Equal(created.Name, roundTrip.Name);
        Assert.Equal(created.Code, roundTrip.Code);
        Assert.Equal(created.EnvironmentId, roundTrip.EnvironmentId);
        Assert.Equal(created.DistributionMode, roundTrip.DistributionMode);
        Assert.Equal(created.EndpointBaseUri, roundTrip.EndpointBaseUri);
        Assert.Equal(created.Status, roundTrip.Status);
        Assert.Equal(created.IsEnabled, roundTrip.IsEnabled);
        Assert.Equal(created.Description, roundTrip.Description);
    }

    [Fact]
    public async Task ControlPlaneRuntimeNodesSearchFiltersNodesByEnvironmentAndReadiness()
    {
        var development = CreateEnvironment();
        var qa = new RuntimeEnvironment
        {
            Id = Guid.NewGuid(),
            Name = "QA",
            Code = "qa",
            IsEnabled = true
        };
        var runtimeNodes = new RuntimeNodeRepositoryFake(
        [
            new RuntimeNode
            {
                Id = Guid.NewGuid(),
                Name = "Development Runtime",
                Code = "dev-runtime",
                EnvironmentId = development.Id,
                EnvironmentName = development.Name,
                DistributionMode = DistributionMode.Pull,
                Status = RuntimeNodeStatus.Active
            },
            new RuntimeNode
            {
                Id = Guid.NewGuid(),
                Name = "QA Runtime",
                Code = "qa-runtime",
                EnvironmentId = qa.Id,
                EnvironmentName = qa.Name,
                DistributionMode = DistributionMode.Hybrid,
                Status = RuntimeNodeStatus.Active,
                EndpointBaseUri = "https://runtime.example"
            }
        ]);
        var model = CreateControlPlaneRuntimeNodesPage(runtimeNodes, [development, qa]);
        model.Search = "runtime.example";

        await model.OnGetAsync(CancellationToken.None);

        var node = Assert.Single(model.RuntimeNodes);
        Assert.Equal("QA Runtime", node.Name);
        Assert.Equal(2, model.TotalRuntimeNodes);
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

    private static RuntimeControlPlanesPage CreateRuntimeControlPlanesPage(
        RuntimeDesignNodeRepositoryFake repository,
        IRuntimeDesignNodeConnectionService? connectionService = null)
    {
        var model = new RuntimeControlPlanesPage(repository, connectionService ?? new NoopRuntimeDesignNodeConnectionService());
        AttachPageContext(model);
        return model;
    }

    private static ControlPlaneRuntimeNodesPage CreateControlPlaneRuntimeNodesPage(
        RuntimeNodeRepositoryFake runtimeNodes,
        IReadOnlyList<RuntimeEnvironment> environments,
        IRuntimeNodeConnectionInteractionService? connectionService = null)
    {
        var model = new ControlPlaneRuntimeNodesPage(
            runtimeNodes,
            new RuntimeEnvironmentRepositoryFake(environments),
            connectionService ?? new NoopRuntimeNodeConnectionInteractionService());
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
            .AddDataAnnotations()
            .Services
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost");
        model.PageContext = new PageContext
        {
            HttpContext = httpContext,
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
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
            var node = nodes.SingleOrDefault(x => x.Id == id)
                ?? throw new KeyNotFoundException($"Runtime node '{id}' was not found.");
            node.IsEnabled = isEnabled;
            node.LastUpdatedAtUtc = updatedAtUtc;
            return Task.CompletedTask;
        }

        public Task Delete(Guid id, DateTime deletedAtUtc, CancellationToken cancellationToken = default)
        {
            var node = nodes.SingleOrDefault(x => x.Id == id)
                ?? throw new KeyNotFoundException($"Runtime node '{id}' was not found.");
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

    private sealed class RecordingRuntimeDesignNodeConnectionService : IRuntimeDesignNodeConnectionService
    {
        public string? LastIssuerBaseUrl { get; private set; }
        public string? LastImportedPackage { get; private set; }

        public Task<RuntimeDesignNode> UpsertDesignNode(
            Guid? id,
            string key,
            string name,
            DistributionMode distributionMode,
            string endpointBaseUri,
            string remoteRuntimeNodeId,
            bool isEnabled,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<RuntimeDesignNodeCredentialPackageModel> GenerateCredentialPackage(Guid designNodeId, string issuerBaseUrl, CancellationToken cancellationToken = default)
        {
            LastIssuerBaseUrl = issuerBaseUrl;
            return Task.FromResult(new RuntimeDesignNodeCredentialPackageModel
            {
                Json = "{\"runtime\":true}",
                Base64 = "e30="
            });
        }

        public Task ImportCredentialPackage(ImportRuntimeDesignNodeCredentialPackageInput input, CancellationToken cancellationToken = default)
        {
            LastImportedPackage = input.Package;
            return Task.CompletedTask;
        }

        public Task<RuntimeDesignNodeConnectionValidationModel> ValidateConnection(Guid designNodeId, CancellationToken cancellationToken = default)
            => Task.FromResult(new RuntimeDesignNodeConnectionValidationModel
            {
                Succeeded = true,
                Message = "Connection OK"
            });
    }

    private sealed class RecordingRuntimeNodeConnectionInteractionService : IRuntimeNodeConnectionInteractionService
    {
        public string? LastIssuerBaseUrl { get; private set; }
        public string? LastImportedPackage { get; private set; }

        public Task<RuntimeNodeCredentialPackageModel> GenerateCredentialPackage(Guid runtimeNodeId, string issuerBaseUrl, CancellationToken cancellationToken = default)
        {
            LastIssuerBaseUrl = issuerBaseUrl;
            return Task.FromResult(new RuntimeNodeCredentialPackageModel
            {
                Json = "{\"controlPlane\":true}",
                Base64 = "e30="
            });
        }

        public Task ImportCredentialPackage(ImportRuntimeNodeCredentialPackageInput input, CancellationToken cancellationToken = default)
        {
            LastImportedPackage = input.Package;
            return Task.CompletedTask;
        }

        public Task<RuntimeNodeConnectionValidationModel> ValidateConnection(Guid runtimeNodeId, CancellationToken cancellationToken = default)
            => Task.FromResult(new RuntimeNodeConnectionValidationModel
            {
                Succeeded = true,
                Message = "Runtime OK"
            });
    }
}
