using System.Net;
using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Application.Distribution.Artifacts;
using KnOwl.ControlPlane.Application.Distribution.ArtifactDelivery;
using KnOwl.ControlPlane.Application.Distribution.ReleaseBundles;
using KnOwl.ControlPlane.Application.Distribution.Security;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.ControlPlane.WebUI;
using KnOwl.Documentation;
using KnOwl.Documentation.Application;
using KnOwl.Documentation.WebUI;
using KnOwl.Runtime.Application.Catalog;
using KnOwl.Runtime.Application.Security;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Storage;
using KnOwl.Runtime.WebUI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ControlPlaneHomeModel = KnOwl.ControlPlane.WebUI.Pages.IndexModel;
using DocumentationIndexModel = KnOwl.Documentation.WebUI.Pages.Documentation.IndexModel;
using RuntimeHomeModel = KnOwl.Runtime.WebUI.Pages.IndexModel;
using RuntimeDesignNode = KnOwl.Runtime.Distribution.RuntimeDesignNode;
using RuntimeDesignNodeStatus = KnOwl.Runtime.Distribution.RuntimeDesignNodeStatus;

namespace KnOwl.Tests;

public sealed class WebUiRouteRenderingCoverageTests
{
    [Fact]
    public async Task ControlPlaneWebUiRoutesRenderWithRepresentativeData()
    {
        await using var fixture = await ControlPlaneWebUiFixture.Start();
        var state = fixture.State;
        var type = state.SchemaTypes.Single(x => x.Key == "customer-reference");
        var typeVersion = type.Versions.First();
        var field = state.MetadataFields.Single();
        var fieldVersion = field.Versions.First();
        var eventDefinition = state.Events.Single();
        var eventVersion = eventDefinition.Versions.First();
        var command = state.Commands.Single();
        var commandVersion = command.Versions.First();
        var release = state.Releases.Single();
        var artifactGroupKey = $"Event:{eventDefinition.Id:N}";

        var urls = new[]
        {
            "/",
            "/Privacy",
            "/Contracts/Schemas",
            "/Contracts/Types",
            $"/Contracts/Types/View/{type.Id}",
            $"/Contracts/Types/Version/{type.Id}/versions/{typeVersion.Id}",
            "/Contracts/Types/New",
            $"/Contracts/Types/Edit/{type.Id}",
            $"/Contracts/Types/NewVersion/{type.Id}",
            "/Contracts/MetadataFields",
            $"/Contracts/MetadataFields/View/{field.Id}",
            $"/Contracts/MetadataFields/Version/{field.Id}/versions/{fieldVersion.Id}",
            "/Contracts/MetadataFields/New",
            $"/Contracts/MetadataFields/Edit/{field.Id}",
            "/Contracts/Events",
            $"/Contracts/Events/View?id={eventDefinition.Id}",
            $"/Contracts/Events/Version/{eventDefinition.Id}/versions/{eventVersion.Id}",
            "/Contracts/Events/New",
            $"/Contracts/Events/Edit/{eventDefinition.Id}",
            $"/Contracts/Events/NewVersion/{eventDefinition.Id}",
            "/Contracts/Commands",
            $"/Contracts/Commands/View?id={command.Id}",
            $"/Contracts/Commands/Version/{command.Id}/versions/{commandVersion.Id}",
            "/Contracts/Commands/New",
            $"/Contracts/Commands/Edit/{command.Id}",
            $"/Contracts/Commands/NewVersion/{command.Id}",
            "/Contracts/RuntimeEnvironments",
            "/Contracts/RuntimeNodes",
            "/Contracts/ContractArtifacts",
            $"/Contracts/ContractArtifacts?groupKey={artifactGroupKey}",
            "/Contracts/ContractReleases",
            $"/Contracts/ContractReleases/View?id={release.Id}"
        };

        foreach (var url in urls)
        {
            using var response = await fixture.Client.GetAsync(url);
            var html = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Found, $"{url} returned {response.StatusCode}. {html}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Assert.Contains("<", html);
            }
        }
    }

    [Fact]
    public async Task RuntimeWebUiRoutesRenderWithRepresentativeData()
    {
        await using var fixture = await RuntimeWebUiFixture.Start();
        var artifactId = fixture.State.Artifacts.First().Id;

        foreach (var url in new[] { "/", "/Artifacts", $"/Artifacts/View/{artifactId}", "/ControlPlanes" })
        {
            using var response = await fixture.Client.GetAsync(url);
            var html = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Found, $"{url} returned {response.StatusCode}. {html}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Assert.Contains("<", html);
            }
        }
    }

    [Fact]
    public async Task DocumentationWebUiRoutesRenderWithRepresentativeData()
    {
        await using var fixture = await DocumentationWebUiFixture.Start();
        var state = fixture.State;
        var space = state.Space;
        var topic = state.Topic;
        var page = state.Page;

        var urls = new[]
        {
            "/documentation",
            $"/documentation/{space.Id}",
            $"/documentation/topics/{topic.Id}",
            $"/documentation/pages/{page.Id}",
            $"/documentation/view/{space.Key}/{topic.Key}/{page.Key}",
            $"/documentation/view/{space.Key}/{topic.Key}/{page.Key}/1.0.0"
        };

        foreach (var url in urls)
        {
            using var response = await fixture.Client.GetAsync(url);
            var html = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Found, $"{url} returned {response.StatusCode}. {html}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Assert.Contains("<", html);
            }
        }
    }

    private sealed class ControlPlaneWebUiFixture : IAsyncDisposable
    {
        private readonly WebApplication app;

        private ControlPlaneWebUiFixture(WebApplication app, HttpClient client, ControlPlaneWebUiState state)
        {
            this.app = app;
            Client = client;
            State = state;
        }

        public HttpClient Client { get; }
        public ControlPlaneWebUiState State { get; }

        public static async Task<ControlPlaneWebUiFixture> Start()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.WebHost.UseTestServer();
            builder.Services.AddRazorPages().AddApplicationPart(typeof(ControlPlaneHomeModel).Assembly);
            builder.Services.AddKnOwlControlPlaneWebUI();

            var state = new ControlPlaneWebUiState();
            builder.Services.AddSingleton(state);
            builder.Services.AddSingleton<ISchemaTypeInteractionService>(state);
            builder.Services.AddSingleton<IContractFieldMetadataInteractionService>(state);
            builder.Services.AddSingleton<IEventInteractionService>(state);
            builder.Services.AddSingleton<ICommandInteractionService>(state);
            builder.Services.AddSingleton<IContractVersionPromotionService>(state);
            builder.Services.AddSingleton<IContractArtifactRepository>(state);
            builder.Services.AddSingleton<IContractArtifactBuilder>(state);
            builder.Services.AddSingleton<IRuntimeEnvironmentRepository>(state);
            builder.Services.AddSingleton<IRuntimeNodeRepository>(state);
            builder.Services.AddSingleton<IRuntimeNodeConnectionInteractionService>(state);
            builder.Services.AddSingleton<IContractReleaseRepository>(state);
            builder.Services.AddSingleton<IContractReleaseExecutionService>(state);
            builder.Services.AddSingleton<IArtifactDeliveryInteractionService>(state);

            var app = builder.Build();
            app.MapRazorPages();
            await app.StartAsync();
            return new ControlPlaneWebUiFixture(app, app.GetTestClient(), state);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }

    private sealed class RuntimeWebUiFixture : IAsyncDisposable
    {
        private readonly WebApplication app;

        private RuntimeWebUiFixture(WebApplication app, HttpClient client, RuntimeWebUiState state)
        {
            this.app = app;
            Client = client;
            State = state;
        }

        public HttpClient Client { get; }
        public RuntimeWebUiState State { get; }

        public static async Task<RuntimeWebUiFixture> Start()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.WebHost.UseTestServer();
            builder.Services.AddRazorPages().AddApplicationPart(typeof(RuntimeHomeModel).Assembly);
            builder.Services.AddKnOwlRuntimeWebUI();
            var state = new RuntimeWebUiState();
            builder.Services.AddSingleton(state);
            builder.Services.AddSingleton<IRuntimeContractCatalogService>(state);
            builder.Services.AddSingleton<IRuntimeDesignNodeRepository>(state);
            builder.Services.AddSingleton<IRuntimeDesignNodeConnectionService>(state);

            var app = builder.Build();
            app.MapRazorPages();
            await app.StartAsync();
            return new RuntimeWebUiFixture(app, app.GetTestClient(), state);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }

    private sealed class DocumentationWebUiFixture : IAsyncDisposable
    {
        private readonly WebApplication app;

        private DocumentationWebUiFixture(WebApplication app, HttpClient client, DocumentationWebUiState state)
        {
            this.app = app;
            Client = client;
            State = state;
        }

        public HttpClient Client { get; }
        public DocumentationWebUiState State { get; }

        public static async Task<DocumentationWebUiFixture> Start()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.WebHost.UseTestServer();
            builder.Services.AddRazorPages().AddApplicationPart(typeof(DocumentationIndexModel).Assembly);
            builder.Services.AddKnOwlDocumentationWebUI();
            var state = new DocumentationWebUiState();
            builder.Services.AddSingleton<IDocumentationInteractionService>(state);

            var app = builder.Build();
            app.MapRazorPages();
            await app.StartAsync();
            return new DocumentationWebUiFixture(app, app.GetTestClient(), state);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }

    private sealed class ControlPlaneWebUiState :
        ISchemaTypeInteractionService,
        IContractFieldMetadataInteractionService,
        IEventInteractionService,
        ICommandInteractionService,
        IContractVersionPromotionService,
        IContractArtifactRepository,
        IContractArtifactBuilder,
        IRuntimeEnvironmentRepository,
        IRuntimeNodeRepository,
        IRuntimeNodeConnectionInteractionService,
        IContractReleaseRepository,
        IContractReleaseExecutionService,
        IArtifactDeliveryInteractionService
    {
        public ControlPlaneWebUiState()
        {
            var now = DateTime.UtcNow;
            var type = new SchemaTypeDefinition { Name = "Customer Reference", Key = "customer-reference", Description = "Reusable customer key.", CreatedAtUtc = now, UpdatedAtUtc = now };
            type.Versions.Add(new SchemaTypeVersion { SchemaTypeDefinitionId = type.Id, VersionNumber = "1.0.0", DefinitionJson = """{"type":"string"}""", Comment = "Initial", IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now });
            SchemaTypes.Add(type);

            var metadata = new ContractFieldMetadataDefinition { Name = "PII Classification", Key = "pii-classification", Description = "Data sensitivity.", CreatedAtUtc = now, UpdatedAtUtc = now };
            metadata.Versions.Add(new ContractFieldMetadataVersion { ContractFieldMetadataDefinitionId = metadata.Id, VersionNumber = "1.0.0", DefinitionJson = """{"scope":["events","commands"],"dataType":"string","required":true,"allowedValues":["public","confidential"]}""", IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now });
            MetadataFields.Add(metadata);

            var eventDefinition = new EventDefinition { Name = "Customer Created", Topic = "customer.created", Description = "Customer lifecycle event.", CreatedAtUtc = now, UpdatedAtUtc = now };
            var eventVersion = new EventVersion { EventDefinition = eventDefinition, EventDefinitionId = eventDefinition.Id, VersionNumber = "1.0.0", PayloadSchemaJson = """{"type":"object","properties":{"customerId":{"type":"string"}}}""", Comment = "Initial", Status = ContractVersionStatus.Deployed, CreatedAtUtc = now, UpdatedAtUtc = now, DeployedAtUtc = now };
            eventDefinition.Versions.Add(eventVersion);
            Events.Add(eventDefinition);

            var command = new CommandDefinition { Name = "Register Customer", Topic = "customer.register", Description = "Registers a customer.", CreatedAtUtc = now, UpdatedAtUtc = now };
            var commandVersion = new CommandVersion { CommandDefinition = command, CommandDefinitionId = command.Id, VersionNumber = "1.0.0", PayloadSchemaJson = """{"type":"object"}""", ReplyPayloadSchemaJson = """{"type":"object","properties":{"accepted":{"type":"boolean"}}}""", Comment = "Initial", Status = ContractVersionStatus.Approved, CreatedAtUtc = now, UpdatedAtUtc = now, ApprovedAtUtc = now };
            command.Versions.Add(commandVersion);
            Commands.Add(command);

            var environment = new RuntimeEnvironment { Name = "Development", Code = "dev", Description = "Development environment.", IsEnabled = true, CreatedAtUtc = now, UpdatedAtUtc = now };
            Environments.Add(environment);

            var runtime = new RuntimeNode
            {
                Name = "Local Runtime",
                Code = "local-runtime",
                EnvironmentId = environment.Id,
                Environment = environment,
                EnvironmentName = environment.Name,
                DistributionMode = DistributionMode.Hybrid,
                EndpointBaseUri = "https://runtime.example.test",
                EndpointApiPath = "/api/v1/runtime",
                Status = RuntimeNodeStatus.Active,
                IsEnabled = true,
                Description = "Local validation runtime.",
                InboundCredentialStatus = ConnectionCredentialStatus.Active,
                InboundClientId = "runtime-inbound",
                InboundKeyId = "runtime-kid",
                InboundAllowedScopes = "knowl.control-plane.pull",
                OutboundCredentialStatus = ConnectionCredentialStatus.Active,
                OutboundClientId = "runtime-outbound",
                OutboundKeyId = "runtime-outbound-kid",
                OutboundRequestedScopes = "knowl.runtime.push",
                RegisteredAtUtc = now,
                LastUpdatedAtUtc = now
            };
            environment.RuntimeNodes.Add(runtime);
            RuntimeNodes.Add(runtime);

            var eventArtifact = CreateArtifact(ContractArtifactType.Event, eventDefinition.Id, eventVersion.Id, eventDefinition.Name, eventDefinition.Topic, eventVersion.VersionNumber, eventDefinition.Description, eventVersion.PayloadSchemaJson);
            var commandArtifact = CreateArtifact(ContractArtifactType.Command, command.Id, commandVersion.Id, command.Name, command.Topic, commandVersion.VersionNumber, command.Description, CommandArtifactPayloadDocument.Compose(commandVersion.PayloadSchemaJson, commandVersion.ReplyPayloadSchemaJson));
            Artifacts.Add(eventArtifact);
            Artifacts.Add(commandArtifact);

            var release = new ContractRelease { Name = "Customer Release", Description = "Customer release bundle.", Status = ContractReleaseStatus.InProgress, CreatedAtUtc = now };
            var eventItem = new ContractReleaseItem { Release = release, ReleaseId = release.Id, Artifact = eventArtifact, ArtifactId = eventArtifact.Id, CreatedAtUtc = now };
            var commandItem = new ContractReleaseItem { Release = release, ReleaseId = release.Id, Artifact = commandArtifact, ArtifactId = commandArtifact.Id, CreatedAtUtc = now };
            release.Items.Add(eventItem);
            release.Items.Add(commandItem);
            release.Targets.Add(new ContractReleaseTarget
            {
                Release = release,
                ReleaseId = release.Id,
                ReleaseItem = eventItem,
                ReleaseItemId = eventItem.Id,
                Artifact = eventArtifact,
                ArtifactId = eventArtifact.Id,
                RuntimeNode = runtime,
                RuntimeNodeId = runtime.Id,
                RolloutGroup = "Manual",
                Status = ContractReleaseTargetStatus.AvailableForPull,
                AssignedAtUtc = now,
                AvailableAtUtc = now,
                Attempts =
                {
                    new ContractReleaseAttempt
                    {
                        Action = "Pull",
                        InitiatedBy = "test",
                        StartedAtUtc = now,
                        FinishedAtUtc = now,
                        Succeeded = true,
                        ExternalReference = "runtime-artifact"
                    }
                }
            });
            release.Targets.Add(new ContractReleaseTarget
            {
                Release = release,
                ReleaseId = release.Id,
                ReleaseItem = commandItem,
                ReleaseItemId = commandItem.Id,
                Artifact = commandArtifact,
                ArtifactId = commandArtifact.Id,
                RuntimeNode = runtime,
                RuntimeNodeId = runtime.Id,
                RolloutGroup = "Manual",
                Status = ContractReleaseTargetStatus.Activated,
                ActivationStatus = ContractReleaseActivationStatus.Activated,
                AssignedAtUtc = now,
                ActivatedAtUtc = now
            });
            Releases.Add(release);
        }

        public List<SchemaTypeDefinition> SchemaTypes { get; } = [];
        public List<ContractFieldMetadataDefinition> MetadataFields { get; } = [];
        public List<EventDefinition> Events { get; } = [];
        public List<CommandDefinition> Commands { get; } = [];
        public List<ContractArtifact> Artifacts { get; } = [];
        public List<RuntimeEnvironment> Environments { get; } = [];
        public List<RuntimeNode> RuntimeNodes { get; } = [];
        public List<ContractRelease> Releases { get; } = [];

        Task<IReadOnlyList<SchemaTypeDefinition>> ISchemaTypeInteractionService.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SchemaTypeDefinition>>(SchemaTypes);
        Task<IReadOnlyList<SchemaTypeVersion>> ISchemaTypeInteractionService.GetActiveVersions(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SchemaTypeVersion>>(SchemaTypes.SelectMany(x => x.Versions).Where(x => x.IsActive).ToArray());
        Task<SchemaTypeDefinition?> ISchemaTypeInteractionService.GetById(Guid id, bool includeVersions, CancellationToken cancellationToken) => Task.FromResult(SchemaTypes.FirstOrDefault(x => x.Id == id));
        Task<SchemaTypeVersion?> ISchemaTypeInteractionService.GetVersionById(Guid versionId, CancellationToken cancellationToken) => Task.FromResult(SchemaTypes.SelectMany(x => x.Versions).FirstOrDefault(x => x.Id == versionId));
        Task<bool> ISchemaTypeInteractionService.KeyExists(string key, Guid? excludingId, CancellationToken cancellationToken) => Task.FromResult(false);
        Task<bool> ISchemaTypeInteractionService.VersionExists(Guid typeId, string versionNumber, CancellationToken cancellationToken) => Task.FromResult(false);
        Task ISchemaTypeInteractionService.Create(SchemaTypeDefinition schemaType, CancellationToken cancellationToken) => Task.CompletedTask;
        Task ISchemaTypeInteractionService.UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;
        Task ISchemaTypeInteractionService.AddVersion(Guid typeId, SchemaTypeVersion version, CancellationToken cancellationToken) => Task.CompletedTask;
        Task ISchemaTypeInteractionService.SetVersionActive(Guid typeId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;

        Task<IReadOnlyList<ContractFieldMetadataDefinition>> IContractFieldMetadataInteractionService.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ContractFieldMetadataDefinition>>(MetadataFields);
        Task<IReadOnlyList<ContractFieldMetadataDefinition>> IContractFieldMetadataInteractionService.GetActive(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ContractFieldMetadataDefinition>>(MetadataFields.Where(x => x.IsActive).ToArray());
        Task<ContractFieldMetadataDefinition?> IContractFieldMetadataInteractionService.GetById(Guid id, bool includeVersions, CancellationToken cancellationToken) => Task.FromResult(MetadataFields.FirstOrDefault(x => x.Id == id));
        Task<bool> IContractFieldMetadataInteractionService.KeyExists(string key, Guid? excludingId, CancellationToken cancellationToken) => Task.FromResult(false);
        Task<bool> IContractFieldMetadataInteractionService.VersionExists(Guid metadataFieldId, string versionNumber, CancellationToken cancellationToken) => Task.FromResult(false);
        Task IContractFieldMetadataInteractionService.Create(ContractFieldMetadataDefinition metadataField, CancellationToken cancellationToken) => Task.CompletedTask;
        Task IContractFieldMetadataInteractionService.UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;
        Task IContractFieldMetadataInteractionService.UpsertVersion(Guid metadataFieldId, ContractFieldMetadataVersion version, CancellationToken cancellationToken) => Task.CompletedTask;
        Task IContractFieldMetadataInteractionService.SetVersionActive(Guid metadataFieldId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;

        Task<IReadOnlyList<EventDefinition>> IEventInteractionService.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<EventDefinition>>(Events);
        Task<EventDefinition?> IEventInteractionService.GetById(Guid id, bool includeVersions, CancellationToken cancellationToken) => Task.FromResult(Events.FirstOrDefault(x => x.Id == id));
        Task<EventVersion?> IEventInteractionService.GetVersionById(Guid versionId, CancellationToken cancellationToken) => Task.FromResult(Events.SelectMany(x => x.Versions).FirstOrDefault(x => x.Id == versionId));
        Task<bool> IEventInteractionService.VersionExists(Guid eventId, string versionNumber, CancellationToken cancellationToken) => Task.FromResult(false);
        Task IEventInteractionService.Create(EventDefinition eventDefinition, CancellationToken cancellationToken) => Task.CompletedTask;
        Task IEventInteractionService.UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;
        Task IEventInteractionService.AddVersion(Guid eventId, EventVersion version, CancellationToken cancellationToken) => Task.CompletedTask;
        Task IEventInteractionService.Delete(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

        Task<IReadOnlyList<CommandDefinition>> ICommandInteractionService.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CommandDefinition>>(Commands);
        Task<CommandDefinition?> ICommandInteractionService.GetById(Guid id, bool includeVersions, CancellationToken cancellationToken) => Task.FromResult(Commands.FirstOrDefault(x => x.Id == id));
        Task<CommandVersion?> ICommandInteractionService.GetVersionById(Guid versionId, CancellationToken cancellationToken) => Task.FromResult(Commands.SelectMany(x => x.Versions).FirstOrDefault(x => x.Id == versionId));
        Task<bool> ICommandInteractionService.VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken) => Task.FromResult(false);
        Task ICommandInteractionService.Create(CommandDefinition commandDefinition, CancellationToken cancellationToken) => Task.CompletedTask;
        Task ICommandInteractionService.UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;
        Task ICommandInteractionService.AddVersion(Guid commandId, CommandVersion version, CancellationToken cancellationToken) => Task.CompletedTask;
        Task ICommandInteractionService.Delete(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

        Task IContractVersionPromotionService.TransitionEventVersion(Guid versionId, ContractVersionStatus targetStatus, CancellationToken cancellationToken) => Task.CompletedTask;
        Task IContractVersionPromotionService.TransitionCommandVersion(Guid versionId, ContractVersionStatus targetStatus, CancellationToken cancellationToken) => Task.CompletedTask;
        IReadOnlyCollection<ContractVersionStatus> IContractVersionPromotionService.GetAllowedTargets(ContractVersionStatus currentStatus) => [ContractVersionStatus.InReview, ContractVersionStatus.Approved, ContractVersionStatus.Abandoned];

        Task<IReadOnlyList<ContractArtifact>> IContractArtifactRepository.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ContractArtifact>>(Artifacts);
        Task<IReadOnlyList<ContractArtifact>> IContractArtifactRepository.GetDeployed(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ContractArtifact>>(Artifacts);
        Task<ContractArtifact?> IContractArtifactRepository.GetById(Guid id, CancellationToken cancellationToken) => Task.FromResult(Artifacts.FirstOrDefault(x => x.Id == id));
        Task<IReadOnlyList<ContractArtifact>> IContractArtifactRepository.GetByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ContractArtifact>>(Artifacts.Where(x => ids.Contains(x.Id)).ToArray());
        Task<ContractArtifact?> IContractArtifactRepository.GetBySourceVersion(ContractArtifactType artifactType, Guid versionId, CancellationToken cancellationToken) => Task.FromResult(Artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.VersionId == versionId));
        Task<ContractArtifact?> IContractArtifactRepository.GetByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken) => Task.FromResult(Artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));
        Task<ContractArtifact?> IContractArtifactRepository.GetDeployedByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken) => Task.FromResult(Artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));
        Task<ContractArtifact?> IContractArtifactRepository.GetLatestDeployed(ContractArtifactType artifactType, string topic, CancellationToken cancellationToken) => Task.FromResult(Artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic));
        Task IContractArtifactRepository.Create(ContractArtifact artifact, CancellationToken cancellationToken) => Task.CompletedTask;
        Task<ContractArtifact> IContractArtifactBuilder.BuildEventArtifact(Guid versionId, CancellationToken cancellationToken) => Task.FromResult(Artifacts.First(x => x.ArtifactType == ContractArtifactType.Event));
        Task<IReadOnlyList<ContractArtifact>> IContractArtifactBuilder.BuildCommandArtifacts(Guid versionId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ContractArtifact>>([Artifacts.First(x => x.ArtifactType == ContractArtifactType.Command)]);
        Task<ContractArtifact> IContractArtifactBuilder.BuildCommandArtifact(Guid versionId, CancellationToken cancellationToken) => Task.FromResult(Artifacts.First(x => x.ArtifactType == ContractArtifactType.Command));

        Task<IReadOnlyList<RuntimeEnvironment>> IRuntimeEnvironmentRepository.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RuntimeEnvironment>>(Environments);
        Task<IReadOnlyList<RuntimeEnvironment>> IRuntimeEnvironmentRepository.GetEnabled(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RuntimeEnvironment>>(Environments.Where(x => x.IsEnabled).ToArray());
        Task<RuntimeEnvironment?> IRuntimeEnvironmentRepository.GetById(Guid id, CancellationToken cancellationToken) => Task.FromResult(Environments.FirstOrDefault(x => x.Id == id));
        Task IRuntimeEnvironmentRepository.Create(RuntimeEnvironment environment, CancellationToken cancellationToken) => Task.CompletedTask;
        Task IRuntimeEnvironmentRepository.Update(RuntimeEnvironment environment, CancellationToken cancellationToken) => Task.CompletedTask;

        Task<IReadOnlyList<RuntimeNode>> IRuntimeNodeRepository.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RuntimeNode>>(RuntimeNodes);
        Task<IReadOnlyList<RuntimeNode>> IRuntimeNodeRepository.GetActiveEnabled(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RuntimeNode>>(RuntimeNodes.Where(x => x.IsEnabled && !x.IsDeleted).ToArray());
        Task<RuntimeNode?> IRuntimeNodeRepository.GetById(Guid id, CancellationToken cancellationToken) => Task.FromResult(RuntimeNodes.FirstOrDefault(x => x.Id == id));
        Task<RuntimeNode?> IRuntimeNodeRepository.GetByCode(string code, CancellationToken cancellationToken) => Task.FromResult(RuntimeNodes.FirstOrDefault(x => x.Code == code));
        Task<RuntimeNode?> IRuntimeNodeRepository.GetByInboundClientId(string clientId, CancellationToken cancellationToken) => Task.FromResult(RuntimeNodes.FirstOrDefault(x => x.InboundClientId == clientId));
        Task IRuntimeNodeRepository.Create(RuntimeNode runtimeNode, CancellationToken cancellationToken) => Task.CompletedTask;
        Task IRuntimeNodeRepository.Update(RuntimeNode runtimeNode, CancellationToken cancellationToken) => Task.CompletedTask;
        Task IRuntimeNodeRepository.SetIsEnabled(Guid id, bool isEnabled, DateTime updatedAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;
        Task IRuntimeNodeRepository.Delete(Guid id, DateTime deletedAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;

        Task<RuntimeNodeCredentialPackageModel> IRuntimeNodeConnectionInteractionService.GenerateCredentialPackage(Guid runtimeNodeId, string issuerBaseUrl, CancellationToken cancellationToken) => Task.FromResult(new RuntimeNodeCredentialPackageModel { Json = "{}", Base64 = "e30=" });
        Task IRuntimeNodeConnectionInteractionService.ImportCredentialPackage(ImportRuntimeNodeCredentialPackageInput input, CancellationToken cancellationToken) => Task.CompletedTask;
        Task<RuntimeNodeConnectionValidationModel> IRuntimeNodeConnectionInteractionService.ValidateConnection(Guid runtimeNodeId, CancellationToken cancellationToken) => Task.FromResult(new RuntimeNodeConnectionValidationModel { Succeeded = true, Message = "OK" });

        Task<IReadOnlyList<ContractRelease>> IContractReleaseRepository.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ContractRelease>>(Releases);
        Task<ContractRelease?> IContractReleaseRepository.GetById(Guid id, bool includeItems, bool includeTargets, CancellationToken cancellationToken) => Task.FromResult(Releases.FirstOrDefault(x => x.Id == id));
        Task IContractReleaseRepository.Create(ContractRelease release, CancellationToken cancellationToken) => Task.CompletedTask;
        Task IContractReleaseRepository.UpdateStatus(Guid id, ContractReleaseStatus status, DateTime changedAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;

        Task<ContractReleaseExecutionResult> IContractReleaseExecutionService.CreateAndExecute(string name, string? description, IReadOnlyCollection<Guid> artifactIds, IReadOnlyCollection<Guid> runtimeNodeIds, string rolloutGroup, string initiatedBy, CancellationToken cancellationToken) => Task.FromResult(new ContractReleaseExecutionResult { ReleaseId = Releases.Single().Id, TotalTargets = 1, Succeeded = 1 });
        Task<ContractReleaseExecutionResult> IContractReleaseExecutionService.Execute(Guid releaseId, string initiatedBy, CancellationToken cancellationToken) => Task.FromResult(new ContractReleaseExecutionResult { ReleaseId = releaseId, TotalTargets = 1, Succeeded = 1 });

        Task<RuntimeArtifactDeliveryResult> IArtifactDeliveryInteractionService.Push(Guid releaseTargetId, string initiatedBy, CancellationToken cancellationToken) => Task.FromResult(new RuntimeArtifactDeliveryResult { Succeeded = true, Status = "Delivered", Message = "Delivered" });
        Task<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>> IArtifactDeliveryInteractionService.GetPendingForPull(Guid runtimeNodeId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>>([]);
        Task<RuntimeArtifactDeliveryPackage> IArtifactDeliveryInteractionService.GetForPull(Guid runtimeNodeId, Guid releaseTargetId, CancellationToken cancellationToken) => Task.FromResult(new RuntimeArtifactDeliveryPackage());
        Task<RuntimeArtifactDeliveryResult> IArtifactDeliveryInteractionService.AcknowledgePull(Guid runtimeNodeId, Guid releaseTargetId, string runtimeArtifactId, string runtimeArtifactStatus, CancellationToken cancellationToken) => Task.FromResult(new RuntimeArtifactDeliveryResult { Succeeded = true, Status = runtimeArtifactStatus, Message = "Acknowledged" });

        private static ContractArtifact CreateArtifact(ContractArtifactType type, Guid definitionId, Guid versionId, string name, string topic, string version, string? description, string schema)
            => new()
            {
                ArtifactType = type,
                DefinitionId = definitionId,
                VersionId = versionId,
                Name = name,
                Topic = topic,
                VersionNumber = version,
                Description = description,
                PayloadSchemaJson = schema,
                ContentHash = Guid.NewGuid().ToString("N"),
                SourceStatus = "Deployed",
                CreatedAtUtc = DateTime.UtcNow
            };
    }

    private sealed class RuntimeWebUiState :
        IRuntimeContractCatalogService,
        IRuntimeDesignNodeRepository,
        IRuntimeDesignNodeConnectionService
    {
        public RuntimeWebUiState()
        {
            var now = DateTime.UtcNow;
            Artifacts.Add(CreateArtifact(ContractArtifactType.Event, "customer.created", "Customer Created"));
            Artifacts.Add(CreateArtifact(ContractArtifactType.Command, "customer.register", "Register Customer"));
            Nodes.Add(new RuntimeDesignNode
            {
                Key = "control-plane",
                Name = "Control Plane",
                EndpointBaseUri = "https://control.example.test",
                RemoteRuntimeNodeId = "runtime-1",
                DistributionMode = DistributionMode.Hybrid,
                InboundCredentialStatus = ConnectionCredentialStatus.Active,
                OutboundCredentialStatus = ConnectionCredentialStatus.Active,
                InboundClientId = "runtime-in",
                OutboundClientId = "runtime-out",
                IsEnabled = true,
                Status = RuntimeDesignNodeStatus.Enabled,
                Description = "Control Plane connection.",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        public List<RuntimeContractArtifact> Artifacts { get; } = [];
        public List<RuntimeDesignNode> Nodes { get; } = [];

        Task<IReadOnlyList<RuntimeContractArtifact>> IRuntimeContractCatalogService.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RuntimeContractArtifact>>(Artifacts);
        Task<RuntimeContractArtifact?> IRuntimeContractCatalogService.GetExact(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken) => Task.FromResult(Artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));
        Task<RuntimeContractArtifact?> IRuntimeContractCatalogService.GetLatest(ContractArtifactType artifactType, string topic, CancellationToken cancellationToken) => Task.FromResult(Artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic));
        Task<RuntimeContractArtifact?> IRuntimeContractCatalogService.GetEvent(string eventKey, string versionNumber, CancellationToken cancellationToken) => Task.FromResult(Artifacts.FirstOrDefault(x => x.ArtifactType == ContractArtifactType.Event && x.Topic == eventKey && x.VersionNumber == versionNumber));
        Task<CommandContractArtifacts<RuntimeContractArtifact>?> IRuntimeContractCatalogService.GetCommand(string commandKey, string versionNumber, CancellationToken cancellationToken)
        {
            var command = Artifacts.FirstOrDefault(x => x.ArtifactType == ContractArtifactType.Command && x.Topic == commandKey && x.VersionNumber == versionNumber);
            return Task.FromResult(command is null ? null : new CommandContractArtifacts<RuntimeContractArtifact>(commandKey, versionNumber, command, null));
        }

        Task<IReadOnlyList<RuntimeDesignNode>> IRuntimeDesignNodeRepository.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RuntimeDesignNode>>(Nodes);
        Task<RuntimeDesignNode?> IRuntimeDesignNodeRepository.GetById(Guid id, CancellationToken cancellationToken) => Task.FromResult(Nodes.FirstOrDefault(x => x.Id == id));
        Task<RuntimeDesignNode?> IRuntimeDesignNodeRepository.GetByKey(string key, CancellationToken cancellationToken) => Task.FromResult(Nodes.FirstOrDefault(x => x.Key == key));
        Task<RuntimeDesignNode?> IRuntimeDesignNodeRepository.GetByInboundClientId(string clientId, CancellationToken cancellationToken) => Task.FromResult(Nodes.FirstOrDefault(x => x.InboundClientId == clientId));
        Task IRuntimeDesignNodeRepository.Upsert(RuntimeDesignNode designNode, CancellationToken cancellationToken) => Task.CompletedTask;

        Task<RuntimeDesignNode> IRuntimeDesignNodeConnectionService.UpsertDesignNode(Guid? id, string key, string name, DistributionMode distributionMode, string endpointBaseUri, string remoteRuntimeNodeId, bool isEnabled, CancellationToken cancellationToken) => Task.FromResult(Nodes.First());
        Task<RuntimeDesignNodeCredentialPackageModel> IRuntimeDesignNodeConnectionService.GenerateCredentialPackage(Guid designNodeId, string issuerBaseUrl, CancellationToken cancellationToken) => Task.FromResult(new RuntimeDesignNodeCredentialPackageModel { Json = "{}", Base64 = "e30=" });
        Task IRuntimeDesignNodeConnectionService.ImportCredentialPackage(ImportRuntimeDesignNodeCredentialPackageInput input, CancellationToken cancellationToken) => Task.CompletedTask;
        Task<RuntimeDesignNodeConnectionValidationModel> IRuntimeDesignNodeConnectionService.ValidateConnection(Guid designNodeId, CancellationToken cancellationToken) => Task.FromResult(new RuntimeDesignNodeConnectionValidationModel { Succeeded = true, Message = "OK" });

        private static RuntimeContractArtifact CreateArtifact(ContractArtifactType type, string topic, string name)
            => new()
            {
                SourceArtifactId = Guid.NewGuid(),
                SourceReleaseId = Guid.NewGuid(),
                ArtifactType = type,
                DefinitionId = Guid.NewGuid(),
                VersionId = Guid.NewGuid(),
                Name = name,
                Topic = topic,
                VersionNumber = "1.0.0",
                Description = $"{name} artifact.",
                PayloadSchemaJson = """{"type":"object","properties":{"id":{"type":"string"}}}""",
                ContentHash = Guid.NewGuid().ToString("N"),
                DeployedAtUtc = DateTime.UtcNow
            };
    }

    private sealed class DocumentationWebUiState : IDocumentationInteractionService
    {
        public DocumentationWebUiState()
        {
            Topic.SpaceId = Space.Id;
            Page.TopicId = Topic.Id;
            Version.PageId = Page.Id;
            Source.PageVersionId = Version.Id;
            Space.Topics.Add(Topic);
            Topic.Space = Space;
            Topic.Pages.Add(Page);
            Page.Topic = Topic;
            Page.Versions.Add(Version);
            Version.Page = Page;
            Version.Assets.Add(Source);
            Source.PageVersion = Version;
        }

        public DocumentationSpace Space { get; } = new() { Key = "knowl", Name = "KnOwl", Description = "KnOwl docs." };
        public DocumentationTopic Topic { get; } = new() { Key = "contracts", Name = "Contracts", Description = "Contract documentation." };
        public DocumentationPage Page { get; } = new() { Key = "overview", Title = "Overview", Description = "Contract overview." };
        public DocumentationPageVersion Version { get; } = new() { VersionNumber = "1.0.0", EntryPath = "index.md", ContentHash = "hash", Status = DocPageVersionStatus.Published, PublishedAtUtc = DateTime.UtcNow };
        public DocumentationAsset Source { get; } = new() { LogicalPath = "index.md", FileName = "index.md", ContentType = "text/markdown", ContentHash = "hash", Kind = DocAssetKind.SourceMarkdown, StorageKey = "source" };

        Task<IReadOnlyList<DocumentationSpace>> IDocumentationInteractionService.GetSpaces(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DocumentationSpace>>([Space]);
        Task<DocumentationSpace?> IDocumentationInteractionService.GetSpace(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == Space.Id ? Space : null);
        Task<DocumentationSpace> IDocumentationInteractionService.UpsertSpace(string key, string name, string? description, bool isActive, CancellationToken cancellationToken) => Task.FromResult(Space);
        Task<IReadOnlyList<DocumentationTopic>> IDocumentationInteractionService.GetTopics(string spaceKey, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DocumentationTopic>>(spaceKey == Space.Key ? [Topic] : []);
        Task<DocumentationTopic?> IDocumentationInteractionService.GetTopic(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == Topic.Id ? Topic : null);
        Task<DocumentationTopic> IDocumentationInteractionService.UpsertTopic(string spaceKey, string key, string name, string? description, bool isActive, CancellationToken cancellationToken) => Task.FromResult(Topic);
        Task<IReadOnlyList<DocumentationPage>> IDocumentationInteractionService.GetPages(string spaceKey, string topicKey, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DocumentationPage>>(spaceKey == Space.Key && topicKey == Topic.Key ? [Page] : []);
        Task<DocumentationPage?> IDocumentationInteractionService.GetPage(Guid id, bool includeVersions, CancellationToken cancellationToken) => Task.FromResult(id == Page.Id ? Page : null);
        Task<DocumentationPage> IDocumentationInteractionService.UpsertPage(string spaceKey, string topicKey, string key, string title, string? description, bool isActive, CancellationToken cancellationToken) => Task.FromResult(Page);
        Task<DocumentationPageVersion> IDocumentationInteractionService.ImportVersion(DocumentationVersionInput input, CancellationToken cancellationToken) => Task.FromResult(Version);
        Task IDocumentationInteractionService.PublishVersion(Guid versionId, CancellationToken cancellationToken) => Task.CompletedTask;
        Task IDocumentationInteractionService.ArchiveVersion(Guid versionId, CancellationToken cancellationToken) => Task.CompletedTask;
        Task<RenderedDocumentation?> IDocumentationInteractionService.Render(string spaceKey, string topicKey, string pageKey, string? versionNumber, CancellationToken cancellationToken)
        {
            if (spaceKey != Space.Key || topicKey != Topic.Key || pageKey != Page.Key)
            {
                return Task.FromResult<RenderedDocumentation?>(null);
            }

            var rendered = new RenderedDocumentation(
                Version,
                Source,
                "# Overview\n\n## Details\n\nBody",
                "<h1 id=\"overview\">Overview</h1><h2 id=\"details\">Details</h2><p>Body</p>",
                [new("overview", "Overview", 1), new("details", "Details", 2)]);
            return Task.FromResult<RenderedDocumentation?>(rendered);
        }

        Task<DocumentationAsset?> IDocumentationInteractionService.GetAsset(Guid assetId, CancellationToken cancellationToken) => Task.FromResult(assetId == Source.Id ? Source : null);
        Task<Stream> IDocumentationInteractionService.OpenAsset(DocumentationAsset asset, CancellationToken cancellationToken) => Task.FromResult<Stream>(new MemoryStream("# Overview"u8.ToArray()));
        Task<(string FileName, Stream Content)> IDocumentationInteractionService.BuildSourcePackage(Guid versionId, CancellationToken cancellationToken) => Task.FromResult<(string, Stream)>(("docs.zip", new MemoryStream("zip"u8.ToArray())));
        Task<(string FileName, Stream Content)> IDocumentationInteractionService.BuildPdf(Guid versionId, CancellationToken cancellationToken) => Task.FromResult<(string, Stream)>(("docs.pdf", new MemoryStream("pdf"u8.ToArray())));
    }
}
