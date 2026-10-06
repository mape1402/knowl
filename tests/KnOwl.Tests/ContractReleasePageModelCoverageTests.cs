using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Application.Distribution.ArtifactDelivery;
using KnOwl.ControlPlane.Application.Distribution.ReleaseBundles;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using ContractReleasesIndexPage = KnOwl.ControlPlane.WebUI.Pages.Contracts.ContractReleases.IndexModel;
using ContractReleasesViewPage = KnOwl.ControlPlane.WebUI.Pages.Contracts.ContractReleases.ViewModel;
using ReleaseInput = KnOwl.ControlPlane.WebUI.Pages.Contracts.ContractReleases.ReleaseInput;

namespace KnOwl.Tests;

public sealed class ContractReleasePageModelCoverageTests
{
    [Fact]
    public async Task ReleaseIndexLoadsCardsOptionsSearchAndEnvironmentGroups()
    {
        var eventArtifact = CreateArtifact("Customer Created", "customer.created", ContractArtifactType.Event);
        var commandVersionId = Guid.NewGuid();
        var commandDefinitionId = Guid.NewGuid();
        var commandArtifact = CreateArtifact(
            "Register Customer Request",
            "customer.register",
            ContractArtifactType.Command,
            commandDefinitionId,
            commandVersionId,
            CommandArtifactPayloadDocument.Compose("{\"type\":\"object\"}", "{\"type\":\"object\"}"));
        var release = CreateRelease(eventArtifact, commandArtifact, ContractReleaseTargetStatus.Failed);
        release.Targets.Single(x => x.ArtifactId == commandArtifact.Id).FailureReason = "Push failed";
        var releases = new ReleaseRepository([release]);
        var artifacts = new ArtifactRepository([eventArtifact, commandArtifact]);
        var runtimeNodes = new RuntimeNodeRepository(
        [
            CreateRuntimeNode("Runtime QA", "runtime-qa", "QA"),
            CreateRuntimeNode("Runtime no env", "runtime-no-env", "")
        ]);
        var model = CreateIndexModel(releases, artifacts, runtimeNodes, new RecordingReleaseExecutionService());
        model.Search = "push";

        await model.OnGetAsync(CancellationToken.None);

        var card = Assert.Single(model.ReleaseCards);
        Assert.Equal(release.Id, card.Id);
        Assert.Equal(2, card.ArtifactCount);
        Assert.Equal(2, card.TargetCount);
        Assert.Equal(1, card.FailedCount);
        Assert.Equal(1, model.TotalReleaseCards);
        Assert.Equal(2, model.ReleaseOptions.Count);
        var commandOption = model.ReleaseOptions.Single(x => x.ContractType == "Command");
        Assert.Equal("Register Customer", commandOption.Name);
        Assert.Equal("Request and reply", commandOption.Details);
        Assert.Contains(model.RuntimeEnvironmentGroups, x => x.Name == "QA");
        Assert.Contains(model.RuntimeEnvironmentGroups, x => x.Name == "No environment");
    }

    [Fact]
    public void ReleaseIndexArtifactTypeOrderCoversEveryBranch()
    {
        var method = typeof(ContractReleasesIndexPage).GetMethod("ArtifactTypeOrder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new InvalidOperationException("ArtifactTypeOrder was not found.");

        Assert.Equal(0, method.Invoke(null, [CreateArtifact("Event", "event", ContractArtifactType.Event)]));
        Assert.Equal(1, method.Invoke(null, [CreateArtifact("Command", "command", ContractArtifactType.Command)]));
        Assert.Equal(2, method.Invoke(null, [CreateArtifact("Command Request", "command", ContractArtifactType.CommandRequest)]));
        Assert.Equal(3, method.Invoke(null, [CreateArtifact("Command Reply", "command", ContractArtifactType.CommandReply)]));
        Assert.Equal(4, method.Invoke(null, [CreateArtifact("Unknown", "unknown", (ContractArtifactType)999)]));
    }

    [Fact]
    public async Task ReleaseIndexPostValidatesSelectionsAndCreatesRelease()
    {
        var artifact = CreateArtifact("Customer Created", "customer.created", ContractArtifactType.Event);
        var runtimeNode = CreateRuntimeNode("Runtime", "runtime", "Development");
        var releases = new ReleaseRepository([]);
        var artifacts = new ArtifactRepository([artifact]);
        var runtimeNodes = new RuntimeNodeRepository([runtimeNode]);
        var execution = new RecordingReleaseExecutionService();
        var model = CreateIndexModel(releases, artifacts, runtimeNodes, execution);

        model.Input = new ReleaseInput();
        Assert.IsType<PageResult>(await model.OnPostAsync(CancellationToken.None));
        Assert.True(model.ShowReleaseModal);
        Assert.Equal(1, model.InitialReleaseStep);

        model.ModelState.Clear();
        model.Input = new ReleaseInput { ArtifactIds = [artifact.Id] };
        Assert.IsType<PageResult>(await model.OnPostAsync(CancellationToken.None));
        Assert.True(model.ShowReleaseModal);
        Assert.Equal(2, model.InitialReleaseStep);

        model.ModelState.Clear();
        model.Input = new ReleaseInput
        {
            Name = "  ",
            Description = "  Release description  ",
            ArtifactIds = [artifact.Id, artifact.Id],
            RuntimeNodeIds = [runtimeNode.Id, runtimeNode.Id]
        };
        var redirect = Assert.IsType<RedirectToPageResult>(await model.OnPostAsync(CancellationToken.None));

        Assert.Null(redirect.PageName);
        Assert.Single(execution.ArtifactIds);
        Assert.Single(execution.RuntimeNodeIds);
        Assert.Equal("Release description", execution.Description);
        Assert.StartsWith("Release ", execution.Name, StringComparison.Ordinal);
        Assert.Contains("Targets: 3", model.StatusMessage);
        Assert.Contains("Succeeded: 2", model.StatusMessage);
        Assert.Contains("Pending pull: 1", model.StatusMessage);
        Assert.Contains("Failed: 0", model.StatusMessage);
    }

    [Fact]
    public async Task ReleaseViewLoadsSearchableArtifactGroupsAndPushActions()
    {
        var eventArtifact = CreateArtifact("Customer Created", "customer.created", ContractArtifactType.Event);
        var commandArtifact = CreateArtifact("Register Customer Reply", "customer.register", ContractArtifactType.CommandReply);
        var release = CreateRelease(eventArtifact, commandArtifact, ContractReleaseTargetStatus.PushScheduled);
        release.Targets.Single(x => x.ArtifactId == commandArtifact.Id).Attempts.Add(new ContractReleaseAttempt
        {
            Id = Guid.NewGuid(),
            ReleaseTargetId = release.Targets.Single(x => x.ArtifactId == commandArtifact.Id).Id,
            Action = "Push",
            InitiatedBy = "test",
            StartedAtUtc = DateTime.UtcNow,
            FinishedAtUtc = DateTime.UtcNow,
            Succeeded = false,
            ErrorMessage = "push-error"
        });
        var releases = new ReleaseRepository([release]);
        var delivery = new RecordingArtifactDeliveryInteractionService();
        var execution = new RecordingReleaseExecutionService();
        var model = CreateViewModel(releases, delivery, execution);

        Assert.IsType<NotFoundResult>(await model.OnGetAsync(null, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await model.OnGetAsync(Guid.NewGuid(), CancellationToken.None));

        model.Search = "push-error";
        Assert.IsType<PageResult>(await model.OnGetAsync(release.Id, CancellationToken.None));

        var group = Assert.Single(model.ArtifactGroups);
        Assert.Equal("Register Customer", group.ContractName);
        Assert.Equal("Command", group.ContractKind);
        Assert.Single(group.Targets);

        var targetId = group.Targets.Single().Id;
        delivery.PushResult = new RuntimeArtifactDeliveryResult
        {
            ReleaseTargetId = targetId,
            Succeeded = true,
            Message = "Sent"
        };
        var pushRedirect = Assert.IsType<RedirectToPageResult>(await model.OnPostPushTargetAsync(release.Id, targetId, CancellationToken.None));
        Assert.Equal(release.Id, pushRedirect.RouteValues!["id"]);
        Assert.Equal("Sent", model.StatusMessage);

        delivery.PushResult = new RuntimeArtifactDeliveryResult
        {
            ReleaseTargetId = targetId,
            Succeeded = false,
            Message = "Runtime rejected"
        };
        await model.OnPostPushTargetAsync(release.Id, targetId, CancellationToken.None);
        Assert.Equal("Push failed: Runtime rejected", model.StatusMessage);

        var allRedirect = Assert.IsType<RedirectToPageResult>(await model.OnPostPushAllAsync(release.Id, CancellationToken.None));
        Assert.Equal(release.Id, allRedirect.RouteValues!["id"]);
        Assert.Contains("Distribution completed", model.StatusMessage);
        Assert.Equal(release.Id, execution.ExecutedReleaseId);
    }

    [Fact]
    public async Task ReleaseViewCoversArtifactOrderingAndNullFallbacks()
    {
        var commandDefinitionId = Guid.NewGuid();
        var commandVersionId = Guid.NewGuid();
        var command = CreateArtifact("Register Customer", "customer.register", ContractArtifactType.Command, commandDefinitionId, commandVersionId);
        var request = CreateArtifact("Register Customer Request", "customer.register", ContractArtifactType.CommandRequest, commandDefinitionId, commandVersionId);
        var release = new ContractRelease
        {
            Id = Guid.NewGuid(),
            Name = "Release",
            Status = ContractReleaseStatus.InProgress,
            CreatedAtUtc = DateTime.UtcNow
        };
        var commandItem = new ContractReleaseItem { Id = Guid.NewGuid(), Release = release, ReleaseId = release.Id, Artifact = command, ArtifactId = command.Id };
        var requestItem = new ContractReleaseItem { Id = Guid.NewGuid(), Release = release, ReleaseId = release.Id, Artifact = request, ArtifactId = request.Id };
        var unknownItem = new ContractReleaseItem { Id = Guid.NewGuid(), Release = release, ReleaseId = release.Id, ArtifactId = Guid.NewGuid() };
        release.Items = [requestItem, commandItem, unknownItem];
        release.Targets =
        [
            new ContractReleaseTarget
            {
                Id = Guid.NewGuid(),
                Release = release,
                ReleaseId = release.Id,
                ReleaseItem = commandItem,
                ReleaseItemId = commandItem.Id,
                Artifact = command,
                ArtifactId = command.Id,
                RuntimeNode = CreateRuntimeNode("Runtime Z", "runtime-z", "Development"),
                Status = ContractReleaseTargetStatus.PushScheduled
            },
            new ContractReleaseTarget
            {
                Id = Guid.NewGuid(),
                Release = release,
                ReleaseId = release.Id,
                ArtifactId = unknownItem.ArtifactId,
                Status = ContractReleaseTargetStatus.Failed,
                ActivationStatus = ContractReleaseActivationStatus.ActivationFailed,
                FailureReason = "missing artifact"
            }
        ];
        var model = CreateViewModel(
            new ReleaseRepository([release]),
            new RecordingArtifactDeliveryInteractionService(),
            new RecordingReleaseExecutionService());
        model.Search = " failed ";

        Assert.IsType<PageResult>(await model.OnGetAsync(release.Id, CancellationToken.None));

        var group = Assert.Single(model.ArtifactGroups);
        Assert.Equal("Event", group.ContractKind);
        Assert.Equal("Unknown contract", group.ContractName);
        Assert.Single(group.Targets);

        model.Search = null;
        Assert.IsType<PageResult>(await model.OnGetAsync(release.Id, CancellationToken.None));

        Assert.Equal(2, model.ArtifactGroups.Count);
        var commandGroup = model.ArtifactGroups.Single(x => x.ContractKind == "Command");
        Assert.Equal("Register Customer", commandGroup.ContractName);
        Assert.Contains(commandGroup.Items, x => x.Artifact?.ArtifactType == ContractArtifactType.Command);
        Assert.Contains(commandGroup.Items, x => x.Artifact?.ArtifactType == ContractArtifactType.CommandRequest);
    }

    private static ContractReleasesIndexPage CreateIndexModel(
        IContractReleaseRepository releases,
        IContractArtifactRepository artifacts,
        IRuntimeNodeRepository runtimeNodes,
        IContractReleaseExecutionService execution)
    {
        var model = new ContractReleasesIndexPage(releases, artifacts, runtimeNodes, execution);
        AttachPageContext(model);
        return model;
    }

    private static ContractReleasesViewPage CreateViewModel(
        IContractReleaseRepository releases,
        IArtifactDeliveryInteractionService delivery,
        IContractReleaseExecutionService execution)
    {
        var model = new ContractReleasesViewPage(releases, delivery, execution);
        AttachPageContext(model);
        return model;
    }

    private static void AttachPageContext(PageModel model)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddOptions()
            .AddMvcCore()
            .AddDataAnnotations()
            .Services
            .BuildServiceProvider();

        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services },
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        };
    }

    private static ContractArtifact CreateArtifact(
        string name,
        string topic,
        ContractArtifactType artifactType,
        Guid? definitionId = null,
        Guid? versionId = null,
        string payloadSchemaJson = "{}")
        => new()
        {
            Id = Guid.NewGuid(),
            ArtifactType = artifactType,
            DefinitionId = definitionId ?? Guid.NewGuid(),
            VersionId = versionId ?? Guid.NewGuid(),
            Name = name,
            Topic = topic,
            VersionNumber = "1.0.0",
            PayloadSchemaJson = payloadSchemaJson,
            ContentHash = Guid.NewGuid().ToString("N"),
            SourceStatus = ContractVersionStatus.Deployed.ToString(),
            CreatedAtUtc = DateTime.UtcNow
        };

    private static ContractRelease CreateRelease(
        ContractArtifact first,
        ContractArtifact second,
        ContractReleaseTargetStatus secondTargetStatus)
    {
        var release = new ContractRelease
        {
            Id = Guid.NewGuid(),
            Name = "Release",
            Status = ContractReleaseStatus.InProgress,
            CreatedAtUtc = DateTime.UtcNow
        };
        var firstItem = new ContractReleaseItem
        {
            Id = Guid.NewGuid(),
            Release = release,
            ReleaseId = release.Id,
            Artifact = first,
            ArtifactId = first.Id
        };
        var secondItem = new ContractReleaseItem
        {
            Id = Guid.NewGuid(),
            Release = release,
            ReleaseId = release.Id,
            Artifact = second,
            ArtifactId = second.Id
        };
        var activatedNode = CreateRuntimeNode("Runtime A", "runtime-a", "Development", DistributionMode.Pull);
        var secondNode = CreateRuntimeNode("Runtime B", "runtime-b", "Development", DistributionMode.Push);
        release.Items = [firstItem, secondItem];
        release.Targets =
        [
            new ContractReleaseTarget
            {
                Id = Guid.NewGuid(),
                Release = release,
                ReleaseId = release.Id,
                ReleaseItem = firstItem,
                ReleaseItemId = firstItem.Id,
                Artifact = first,
                ArtifactId = first.Id,
                RuntimeNode = activatedNode,
                RuntimeNodeId = activatedNode.Id,
                Status = ContractReleaseTargetStatus.Activated,
                ActivationStatus = ContractReleaseActivationStatus.Activated
            },
            new ContractReleaseTarget
            {
                Id = Guid.NewGuid(),
                Release = release,
                ReleaseId = release.Id,
                ReleaseItem = secondItem,
                ReleaseItemId = secondItem.Id,
                Artifact = second,
                ArtifactId = second.Id,
                RuntimeNode = secondNode,
                RuntimeNodeId = secondNode.Id,
                Status = secondTargetStatus,
                ActivationStatus = secondTargetStatus == ContractReleaseTargetStatus.Failed
                    ? ContractReleaseActivationStatus.ActivationFailed
                    : ContractReleaseActivationStatus.NotActivated
            }
        ];

        return release;
    }

    private static RuntimeNode CreateRuntimeNode(
        string name,
        string code,
        string environmentName,
        DistributionMode mode = DistributionMode.Pull)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            EnvironmentId = string.IsNullOrWhiteSpace(environmentName) ? null : Guid.NewGuid(),
            EnvironmentName = environmentName,
            DistributionMode = mode,
            IsEnabled = true,
            Status = RuntimeNodeStatus.Active
        };

    private sealed class ReleaseRepository(List<ContractRelease> releases) : IContractReleaseRepository
    {
        public Task<IReadOnlyList<ContractRelease>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ContractRelease>>(releases);

        public Task<ContractRelease?> GetById(Guid id, bool includeItems = false, bool includeTargets = false, CancellationToken cancellationToken = default)
            => Task.FromResult(releases.FirstOrDefault(x => x.Id == id));

        public Task Create(ContractRelease release, CancellationToken cancellationToken = default)
        {
            releases.Add(release);
            return Task.CompletedTask;
        }

        public Task UpdateStatus(Guid id, ContractReleaseStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default)
        {
            releases.First(x => x.Id == id).Status = status;
            return Task.CompletedTask;
        }
    }

    private sealed class ArtifactRepository(IReadOnlyList<ContractArtifact> artifacts) : IContractArtifactRepository
    {
        public Task<IReadOnlyList<ContractArtifact>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult(artifacts);
        public Task<IReadOnlyList<ContractArtifact>> GetDeployed(CancellationToken cancellationToken = default) => Task.FromResult(artifacts);
        public Task<ContractArtifact?> GetById(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(artifacts.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<ContractArtifact>> GetByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContractArtifact>>(artifacts.Where(x => ids.Contains(x.Id)).ToArray());
        public Task<ContractArtifact?> GetBySourceVersion(ContractArtifactType artifactType, Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.VersionId == versionId));
        public Task<ContractArtifact?> GetByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));
        public Task<ContractArtifact?> GetDeployedByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default) => Task.FromResult(artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));
        public Task<ContractArtifact?> GetLatestDeployed(ContractArtifactType artifactType, string topic, CancellationToken cancellationToken = default) => Task.FromResult(artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic));
        public Task Create(ContractArtifact artifact, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RuntimeNodeRepository(IReadOnlyList<RuntimeNode> runtimeNodes) : IRuntimeNodeRepository
    {
        public Task<IReadOnlyList<RuntimeNode>> GetAll(CancellationToken cancellationToken = default) => Task.FromResult(runtimeNodes);
        public Task<IReadOnlyList<RuntimeNode>> GetActiveEnabled(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RuntimeNode>>(runtimeNodes.Where(x => x.IsEnabled && x.Status == RuntimeNodeStatus.Active).ToArray());
        public Task<RuntimeNode?> GetById(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(runtimeNodes.FirstOrDefault(x => x.Id == id));
        public Task<RuntimeNode?> GetByCode(string code, CancellationToken cancellationToken = default) => Task.FromResult(runtimeNodes.FirstOrDefault(x => x.Code == code));
        public Task<RuntimeNode?> GetByInboundClientId(string clientId, CancellationToken cancellationToken = default) => Task.FromResult(runtimeNodes.FirstOrDefault(x => x.InboundClientId == clientId));
        public Task Create(RuntimeNode runtimeNode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task Update(RuntimeNode runtimeNode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SetIsEnabled(Guid id, bool isEnabled, DateTime updatedAtUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task Delete(Guid id, DateTime deletedAtUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecordingReleaseExecutionService : IContractReleaseExecutionService
    {
        public string? Name { get; private set; }
        public string? Description { get; private set; }
        public IReadOnlyCollection<Guid> ArtifactIds { get; private set; } = [];
        public IReadOnlyCollection<Guid> RuntimeNodeIds { get; private set; } = [];
        public Guid? ExecutedReleaseId { get; private set; }

        public Task<ContractReleaseExecutionResult> CreateAndExecute(
            string name,
            string? description,
            IReadOnlyCollection<Guid> artifactIds,
            IReadOnlyCollection<Guid> runtimeNodeIds,
            string rolloutGroup = "ManualRelease",
            string initiatedBy = "distribution",
            CancellationToken cancellationToken = default)
        {
            Name = name;
            Description = description;
            ArtifactIds = artifactIds;
            RuntimeNodeIds = runtimeNodeIds;
            return Task.FromResult(new ContractReleaseExecutionResult
            {
                ReleaseId = Guid.NewGuid(),
                TotalTargets = 3,
                Succeeded = 2,
                AvailableForPull = 1,
                Failed = 0
            });
        }

        public Task<ContractReleaseExecutionResult> Execute(Guid releaseId, string initiatedBy = "distribution", CancellationToken cancellationToken = default)
        {
            ExecutedReleaseId = releaseId;
            return Task.FromResult(new ContractReleaseExecutionResult
            {
                ReleaseId = releaseId,
                TotalTargets = 2,
                Succeeded = 1,
                AvailableForPull = 1,
                Failed = 0
            });
        }
    }

    private sealed class RecordingArtifactDeliveryInteractionService : IArtifactDeliveryInteractionService
    {
        public RuntimeArtifactDeliveryResult PushResult { get; set; } = new() { Succeeded = true, Message = "OK" };

        public Task<RuntimeArtifactDeliveryResult> Push(Guid releaseTargetId, string initiatedBy = "distribution", CancellationToken cancellationToken = default)
            => Task.FromResult(PushResult);

        public Task<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>> GetPendingForPull(Guid runtimeNodeId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<RuntimeArtifactDeliveryPackage> GetForPull(Guid runtimeNodeId, Guid releaseTargetId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<RuntimeArtifactDeliveryResult> AcknowledgePull(Guid runtimeNodeId, Guid releaseTargetId, string runtimeArtifactId, string runtimeArtifactStatus = "Ready", CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
