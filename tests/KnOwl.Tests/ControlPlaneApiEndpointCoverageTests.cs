using System.Net;
using System.Net.Http.Json;
using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.Contracts.Artifacts;
using KnOwl.Contracts.Distribution;
using KnOwl.ControlPlane.Api;
using KnOwl.ControlPlane.Api.Contracts;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Application.Distribution.Artifacts;
using KnOwl.ControlPlane.Application.Distribution.Catalog;
using KnOwl.ControlPlane.Application.Distribution.ReleaseBundles;
using KnOwl.ControlPlane.Application.Distribution.Security;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.Security.Authorization;
using KnOwl.Security.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Tests;

public sealed class ControlPlaneApiEndpointCoverageTests
{
    [Fact]
    public async Task ControlPlaneApiRoutesExecuteHappyPathsAndKeyErrorBranches()
    {
        await using var fixture = await ControlPlaneApiFixture.Start();
        var client = fixture.Client;
        var state = fixture.State;

        await AssertStatus(client.GetAsync("/api/v1/control-plane/schema-types/"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync($"/api/v1/control-plane/schema-types/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
        var schemaPost = await client.PostAsJsonAsync("/api/v1/control-plane/schema-types/", new CreateSchemaTypeRequest(" customer ", " Customer ", "Schema", " 1.0.0 ", """{"type":"object"}""", "Initial"));
        Assert.Equal(HttpStatusCode.Created, schemaPost.StatusCode);
        var schemaId = state.SchemaTypes.Single(x => x.Key == "customer").Id;
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/schema-types/", new CreateSchemaTypeRequest("customer", "Duplicate", null, "1.0.0", "{}", null)), HttpStatusCode.Conflict);
        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/schema-types/{schemaId}", new UpdateSchemaTypeRequest("customer-v2", "Customer v2", "Updated", true)), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/schema-types/{schemaId}/versions", new CreateSchemaTypeVersionRequest("1.1.0", """{"type":"object"}""", "Minor")), HttpStatusCode.Created);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/schema-types/{schemaId}/versions", new CreateSchemaTypeVersionRequest("1.1.0", """{"type":"object"}""", "Duplicate")), HttpStatusCode.Conflict);
        await AssertStatus(client.PatchAsJsonAsync($"/api/v1/control-plane/schema-types/{schemaId}/versions/{state.SchemaTypes.Single(x => x.Id == schemaId).Versions.Last().Id}/active", new SetVersionActiveRequest(false)), HttpStatusCode.NoContent);

        await AssertStatus(client.GetAsync("/api/v1/control-plane/metadata-fields/"), HttpStatusCode.OK);
        var metadataPost = await client.PostAsJsonAsync("/api/v1/control-plane/metadata-fields/", new CreateMetadataFieldRequest(" trace-id ", " Trace Id ", "Correlation", " 1.0.0 ", "{}", "Initial"));
        Assert.Equal(HttpStatusCode.Created, metadataPost.StatusCode);
        var metadataId = state.MetadataFields.Single(x => x.Key == "trace-id").Id;
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/metadata-fields/", new CreateMetadataFieldRequest("trace-id", "Duplicate", null, "1.0.0", "{}", null)), HttpStatusCode.Conflict);
        await AssertStatus(client.GetAsync($"/api/v1/control-plane/metadata-fields/{metadataId}"), HttpStatusCode.OK);
        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/metadata-fields/{metadataId}", new UpdateMetadataFieldRequest("trace-key", "Trace Key", "Updated", true)), HttpStatusCode.OK);
        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/metadata-fields/{metadataId}/versions", new UpsertMetadataFieldVersionRequest("1.1.0", "{}", "Minor")), HttpStatusCode.OK);
        await AssertStatus(client.PatchAsJsonAsync($"/api/v1/control-plane/metadata-fields/{metadataId}/versions/{state.MetadataFields.Single(x => x.Id == metadataId).Versions.Last().Id}/active", new SetVersionActiveRequest(false)), HttpStatusCode.NoContent);

        await AssertStatus(client.GetAsync("/api/v1/control-plane/events/"), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/events/", new CreateEventRequest(" Customer Created ", " customer.created ", "Created", " 1.0.0 ", "{}", "Initial")), HttpStatusCode.Created);
        var eventId = state.Events.Single(x => x.Topic == "customer.created").Id;
        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/events/{eventId}", new UpdateEventRequest("Customer Changed", "customer.changed", "Updated")), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/events/{eventId}/versions", new CreateEventVersionRequest("1.1.0", "{}", "Minor")), HttpStatusCode.Created);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/events/{eventId}/versions", new CreateEventVersionRequest("1.1.0", "{}", "Duplicate")), HttpStatusCode.Conflict);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/events/{eventId}/versions/{state.Events.Single(x => x.Id == eventId).Versions.Last().Id}/transition", new TransitionVersionRequest(ContractVersionStatus.InReview)), HttpStatusCode.NoContent);
        await AssertStatus(client.DeleteAsync($"/api/v1/control-plane/events/{eventId}"), HttpStatusCode.NoContent);

        await AssertStatus(client.GetAsync("/api/v1/control-plane/commands/"), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/commands/", new CreateCommandRequest(" Create Customer ", " customer.create ", "Create", " 1.0.0 ", "{}", "   ", "Initial")), HttpStatusCode.Created);
        var commandId = state.Commands.Single(x => x.Topic == "customer.create").Id;
        Assert.Null(state.Commands.Single(x => x.Id == commandId).Versions.Single().ReplyPayloadSchemaJson);
        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/commands/{commandId}", new UpdateCommandRequest("Create Customer", "customer.create.v2", "Updated")), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/commands/{commandId}/versions", new CreateCommandVersionRequest("1.1.0", "{}", """{"type":"object"}""", "Minor")), HttpStatusCode.Created);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/commands/{commandId}/versions", new CreateCommandVersionRequest("1.1.0", "{}", null, "Duplicate")), HttpStatusCode.Conflict);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/commands/{commandId}/versions/{state.Commands.Single(x => x.Id == commandId).Versions.Last().Id}/transition", new TransitionVersionRequest(ContractVersionStatus.InReview)), HttpStatusCode.NoContent);
        await AssertStatus(client.DeleteAsync($"/api/v1/control-plane/commands/{commandId}"), HttpStatusCode.NoContent);

        await AssertStatus(client.GetAsync("/api/v1/control-plane/artifacts/"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/api/v1/control-plane/artifacts/deployed"), HttpStatusCode.OK);
        await AssertStatus(client.PostAsync($"/api/v1/control-plane/artifacts/events/{Guid.NewGuid()}/build", null), HttpStatusCode.OK);
        await AssertStatus(client.PostAsync($"/api/v1/control-plane/artifacts/commands/{Guid.NewGuid()}/build", null), HttpStatusCode.OK);

        await AssertStatus(client.GetAsync("/api/v1/control-plane/runtime-environments/"), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/runtime-environments/", new CreateRuntimeEnvironmentRequest(" Development ", " dev ", "Dev")), HttpStatusCode.Created);
        var environmentId = state.Environments.Single(x => x.Code == "dev").Id;
        await AssertStatus(client.GetAsync($"/api/v1/control-plane/runtime-environments/{environmentId}"), HttpStatusCode.OK);
        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/runtime-environments/{environmentId}", new UpdateRuntimeEnvironmentRequest("QA", "qa", "QA", true)), HttpStatusCode.OK);

        await AssertStatus(client.GetAsync("/api/v1/control-plane/runtime-nodes/"), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/runtime-nodes/", new CreateRuntimeNodeRequest(" Runtime ", " runtime ", environmentId, " QA ", DistributionMode.Hybrid, "https://runtime.example.test/", "/runtime/", true, "Node")), HttpStatusCode.Created);
        var runtimeId = state.RuntimeNodes.Single(x => x.Code == "runtime").Id;
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/runtime-nodes/", new CreateRuntimeNodeRequest("Duplicate", "runtime", environmentId, "QA", DistributionMode.Hybrid, "https://runtime.example.test", "/runtime")), HttpStatusCode.Conflict);
        await AssertStatus(client.GetAsync($"/api/v1/control-plane/runtime-nodes/{runtimeId}"), HttpStatusCode.OK);
        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/runtime-nodes/{runtimeId}", new UpdateRuntimeNodeRequest("Runtime 2", "runtime-2", environmentId, "QA", DistributionMode.Pull, "https://runtime2.example.test/", "/api/", true, "Updated")), HttpStatusCode.OK);
        await AssertStatus(client.PostAsync($"/api/v1/control-plane/runtime-nodes/{runtimeId}/credentials/generate?issuerBaseUrl=https%3A%2F%2Fcontrol.example.test", null), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/runtime-nodes/{runtimeId}/credentials/import", new ImportRuntimeNodeCredentialPackageRequest("{}")), HttpStatusCode.NoContent);
        await AssertStatus(client.PostAsync($"/api/v1/control-plane/runtime-nodes/{runtimeId}/connect/validate", null), HttpStatusCode.OK);

        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/releases/", new CreateReleaseRequest(" Release ", "Release", [state.Artifacts.Single().Id])), HttpStatusCode.Created);
        var releaseId = state.Releases.Single().Id;
        await AssertStatus(client.GetAsync("/api/v1/control-plane/releases/"), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync($"/api/v1/control-plane/releases/{releaseId}"), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/releases/{releaseId}/plan", new PlanReleaseRequest([runtimeId], "Manual")), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/releases/{releaseId}/execute", new ExecuteReleaseRequest("api")), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/releases/execute", new CreateAndExecuteReleaseRequest(" One Shot ", "Now", [state.Artifacts.Single().Id], [runtimeId], "Manual", "api")), HttpStatusCode.OK);

        await AssertStatus(client.GetAsync("/api/v1/control-plane/security/subjects"), HttpStatusCode.OK);
        await AssertStatus(client.PutAsJsonAsync("/api/v1/control-plane/security/subjects", new UpsertSecuritySubjectRequest(" oidc ", " user-1 ", " User ", " user@example.test ")), HttpStatusCode.OK);
        await AssertStatus(client.GetAsync("/api/v1/control-plane/security/role-assignments"), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/security/role-assignments", new AssignSecurityRoleRequest(" oidc ", " user-1 ", " Admin ", " Global ", " * ")), HttpStatusCode.Created);
        await AssertStatus(client.GetAsync("/api/v1/control-plane/security/permission-assignments"), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/security/permission-assignments", new AssignSecurityPermissionRequest(" oidc ", " user-1 ", " contracts.read ", " Global ", " * ")), HttpStatusCode.Created);
        await AssertStatus(client.GetAsync("/api/v1/control-plane/security/external-group-role-assignments"), HttpStatusCode.OK);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/security/external-group-role-assignments", new AssignSecurityExternalGroupRoleRequest(" oidc ", " group-1 ", " Reader ", " Global ", " * ")), HttpStatusCode.Created);
    }

    [Fact]
    public async Task ControlPlaneApiRoutesReturnExpectedNotFoundAndConflictResponses()
    {
        await using var fixture = await ControlPlaneApiFixture.Start();
        var client = fixture.Client;
        var missing = Guid.NewGuid();

        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/schema-types/{missing}", new UpdateSchemaTypeRequest("missing", "Missing", null, true)), HttpStatusCode.NotFound);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/schema-types/{missing}/versions", new CreateSchemaTypeVersionRequest("1.0.0", "{}", null)), HttpStatusCode.NotFound);
        await AssertStatus(client.PatchAsJsonAsync($"/api/v1/control-plane/schema-types/{missing}/versions/{Guid.NewGuid()}/active", new SetVersionActiveRequest(true)), HttpStatusCode.NotFound);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/schema-types/", new CreateSchemaTypeRequest("first", "First", null, "1.0.0", "{}", null)), HttpStatusCode.Created);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/schema-types/", new CreateSchemaTypeRequest("second", "Second", null, "1.0.0", "{}", null)), HttpStatusCode.Created);
        var firstSchemaId = fixture.State.SchemaTypes.Single(x => x.Key == "first").Id;
        var secondSchemaId = fixture.State.SchemaTypes.Single(x => x.Key == "second").Id;
        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/schema-types/{secondSchemaId}", new UpdateSchemaTypeRequest("first", "Duplicate", null, true)), HttpStatusCode.Conflict);

        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/metadata-fields/{missing}", new UpdateMetadataFieldRequest("missing", "Missing", null, true)), HttpStatusCode.NotFound);
        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/metadata-fields/{missing}/versions", new UpsertMetadataFieldVersionRequest("1.0.0", "{}", null)), HttpStatusCode.NotFound);
        await AssertStatus(client.PatchAsJsonAsync($"/api/v1/control-plane/metadata-fields/{missing}/versions/{Guid.NewGuid()}/active", new SetVersionActiveRequest(true)), HttpStatusCode.NotFound);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/metadata-fields/", new CreateMetadataFieldRequest("meta-one", "Meta One", null, "1.0.0", "{}", null)), HttpStatusCode.Created);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/metadata-fields/", new CreateMetadataFieldRequest("meta-two", "Meta Two", null, "1.0.0", "{}", null)), HttpStatusCode.Created);
        var secondMetadataId = fixture.State.MetadataFields.Single(x => x.Key == "meta-two").Id;
        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/metadata-fields/{secondMetadataId}", new UpdateMetadataFieldRequest("meta-one", "Duplicate", null, true)), HttpStatusCode.Conflict);

        await AssertStatus(client.GetAsync($"/api/v1/control-plane/events/{missing}"), HttpStatusCode.NotFound);
        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/events/{missing}", new UpdateEventRequest("Missing", "missing", null)), HttpStatusCode.NotFound);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/events/{missing}/versions", new CreateEventVersionRequest("1.0.0", "{}", null)), HttpStatusCode.NotFound);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/events/{missing}/versions/{Guid.NewGuid()}/transition", new TransitionVersionRequest(ContractVersionStatus.InReview)), HttpStatusCode.NotFound);
        await AssertStatus(client.DeleteAsync($"/api/v1/control-plane/events/{missing}"), HttpStatusCode.NotFound);

        await AssertStatus(client.GetAsync($"/api/v1/control-plane/commands/{missing}"), HttpStatusCode.NotFound);
        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/commands/{missing}", new UpdateCommandRequest("Missing", "missing", null)), HttpStatusCode.NotFound);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/commands/{missing}/versions", new CreateCommandVersionRequest("1.0.0", "{}", null, null)), HttpStatusCode.NotFound);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/commands/{missing}/versions/{Guid.NewGuid()}/transition", new TransitionVersionRequest(ContractVersionStatus.InReview)), HttpStatusCode.NotFound);
        await AssertStatus(client.DeleteAsync($"/api/v1/control-plane/commands/{missing}"), HttpStatusCode.NotFound);

        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/runtime-environments/{missing}", new UpdateRuntimeEnvironmentRequest("Missing", "missing", null, true)), HttpStatusCode.NotFound);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/runtime-environments/", new CreateRuntimeEnvironmentRequest("Development", "dev", null)), HttpStatusCode.Created);
        var environmentId = fixture.State.Environments.Single().Id;

        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/runtime-nodes/{missing}", new UpdateRuntimeNodeRequest("Missing", "missing", environmentId, "Development", DistributionMode.Pull, "https://runtime.example.test", "/", true, null)), HttpStatusCode.NotFound);
        await AssertStatus(client.DeleteAsync($"/api/v1/control-plane/runtime-nodes/{missing}"), HttpStatusCode.NotFound);
        await AssertStatus(client.PostAsync($"/api/v1/control-plane/runtime-nodes/{missing}/credentials/generate?issuerBaseUrl=https%3A%2F%2Fcontrol.example.test", null), HttpStatusCode.NotFound);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/runtime-nodes/{missing}/credentials/import", new ImportRuntimeNodeCredentialPackageRequest("{}")), HttpStatusCode.NotFound);
        await AssertStatus(client.PostAsync($"/api/v1/control-plane/runtime-nodes/{missing}/connect/validate", null), HttpStatusCode.NotFound);

        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/runtime-nodes/", new CreateRuntimeNodeRequest("Runtime One", "runtime-one", environmentId, "Development", DistributionMode.Pull, "https://runtime1.example.test", "/")), HttpStatusCode.Created);
        await AssertStatus(client.PostAsJsonAsync("/api/v1/control-plane/runtime-nodes/", new CreateRuntimeNodeRequest("Runtime Two", "runtime-two", environmentId, "Development", DistributionMode.Pull, "https://runtime2.example.test", "/")), HttpStatusCode.Created);
        var runtimeTwoId = fixture.State.RuntimeNodes.Single(x => x.Code == "runtime-two").Id;
        await AssertStatus(client.PutAsJsonAsync($"/api/v1/control-plane/runtime-nodes/{runtimeTwoId}", new UpdateRuntimeNodeRequest("Runtime Duplicate", "runtime-one", environmentId, "Development", DistributionMode.Pull, "https://runtime3.example.test", "/", true, null)), HttpStatusCode.Conflict);
        var runtimeOneId = fixture.State.RuntimeNodes.Single(x => x.Code == "runtime-one").Id;
        await AssertStatus(client.DeleteAsync($"/api/v1/control-plane/runtime-nodes/{runtimeOneId}"), HttpStatusCode.NoContent);

        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/releases/{missing}/plan", new PlanReleaseRequest([runtimeTwoId], "Manual")), HttpStatusCode.NotFound);
        await AssertStatus(client.PostAsJsonAsync($"/api/v1/control-plane/releases/{missing}/execute", new ExecuteReleaseRequest("api")), HttpStatusCode.NotFound);

        Assert.NotEqual(firstSchemaId, secondSchemaId);
    }

    [Fact]
    public async Task ControlPlaneApiCanBeMappedWithFallbackPolicy()
    {
        await using var fixture = await ControlPlaneApiFixture.Start("api-policy");

        Assert.NotNull(fixture.Client);
    }

    private static async Task AssertStatus(Task<HttpResponseMessage> task, HttpStatusCode expected)
    {
        using var response = await task;
        Assert.Equal(expected, response.StatusCode);
    }

    private sealed class ControlPlaneApiFixture : IAsyncDisposable
    {
        private readonly WebApplication app;

        private ControlPlaneApiFixture(WebApplication app, HttpClient client, ApiState state)
        {
            this.app = app;
            Client = client;
            State = state;
        }

        public HttpClient Client { get; }
        public ApiState State { get; }

        public static async Task<ControlPlaneApiFixture> Start(string? fallbackPolicy = null)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            if (!string.IsNullOrWhiteSpace(fallbackPolicy))
            {
                builder.Services.AddAuthorization(options =>
                    options.AddPolicy(fallbackPolicy, policy => policy.RequireAssertion(_ => true)));
            }
            var state = new ApiState();
            builder.Services.AddSingleton(state);
            builder.Services.AddSingleton<ISchemaTypeInteractionService>(state);
            builder.Services.AddSingleton<IContractFieldMetadataInteractionService>(state);
            builder.Services.AddSingleton<IEventInteractionService>(state);
            builder.Services.AddSingleton<ICommandInteractionService>(state);
            builder.Services.AddSingleton<IContractVersionPromotionService>(state);
            builder.Services.AddSingleton<IContractArtifactRepository>(state);
            builder.Services.AddSingleton<IControlPlaneContractCatalogService>(state);
            builder.Services.AddSingleton<IContractArtifactBuilder>(state);
            builder.Services.AddSingleton<IRuntimeEnvironmentRepository>(state);
            builder.Services.AddSingleton<IRuntimeNodeRepository>(state);
            builder.Services.AddSingleton<IRuntimeNodeConnectionInteractionService>(state);
            builder.Services.AddSingleton<IContractReleaseRepository>(state);
            builder.Services.AddSingleton<IContractReleaseInteractionService>(state);
            builder.Services.AddSingleton<IContractReleaseExecutionService>(state);
            builder.Services.AddSingleton<IKnOwlSecurityStore>(state);

            var app = builder.Build();
            app.MapKnOwlControlPlaneApi(string.IsNullOrWhiteSpace(fallbackPolicy)
                ? new KnOwlApiAuthorizationOptions()
                : new KnOwlApiAuthorizationOptions { FallbackPolicy = fallbackPolicy });
            await app.StartAsync();
            return new ControlPlaneApiFixture(app, app.GetTestClient(), state);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }

    private sealed class ApiState :
        ISchemaTypeInteractionService,
        IContractFieldMetadataInteractionService,
        IEventInteractionService,
        ICommandInteractionService,
        IContractVersionPromotionService,
        IContractArtifactRepository,
        IControlPlaneContractCatalogService,
        IContractArtifactBuilder,
        IRuntimeEnvironmentRepository,
        IRuntimeNodeRepository,
        IRuntimeNodeConnectionInteractionService,
        IContractReleaseRepository,
        IContractReleaseInteractionService,
        IContractReleaseExecutionService,
        IKnOwlSecurityStore
    {
        public List<SchemaTypeDefinition> SchemaTypes { get; } = [];
        public List<ContractFieldMetadataDefinition> MetadataFields { get; } = [];
        public List<EventDefinition> Events { get; } = [];
        public List<CommandDefinition> Commands { get; } = [];
        public List<ContractArtifact> Artifacts { get; } = [CreateArtifact()];
        public List<RuntimeEnvironment> Environments { get; } = [];
        public List<RuntimeNode> RuntimeNodes { get; } = [];
        public List<ContractRelease> Releases { get; } = [];
        private readonly List<KnOwlSubject> subjects = [];
        private readonly List<KnOwlRoleAssignment> roles = [];
        private readonly List<KnOwlPermissionAssignment> permissions = [];
        private readonly List<KnOwlExternalGroupRoleAssignment> groupRoles = [];

        Task<IReadOnlyList<SchemaTypeDefinition>> ISchemaTypeInteractionService.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SchemaTypeDefinition>>(SchemaTypes);
        Task<IReadOnlyList<SchemaTypeVersion>> ISchemaTypeInteractionService.GetActiveVersions(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SchemaTypeVersion>>(SchemaTypes.SelectMany(x => x.Versions).Where(x => x.IsActive).ToList());
        Task<SchemaTypeDefinition?> ISchemaTypeInteractionService.GetById(Guid id, bool includeVersions, CancellationToken cancellationToken) => Task.FromResult(SchemaTypes.FirstOrDefault(x => x.Id == id));
        Task<SchemaTypeVersion?> ISchemaTypeInteractionService.GetVersionById(Guid versionId, CancellationToken cancellationToken) => Task.FromResult(SchemaTypes.SelectMany(x => x.Versions).FirstOrDefault(x => x.Id == versionId));
        Task<bool> ISchemaTypeInteractionService.KeyExists(string key, Guid? excludingId, CancellationToken cancellationToken) => Task.FromResult(SchemaTypes.Any(x => x.Id != excludingId && string.Equals(x.Key, key.Trim(), StringComparison.OrdinalIgnoreCase)));
        Task<bool> ISchemaTypeInteractionService.VersionExists(Guid typeId, string versionNumber, CancellationToken cancellationToken) => Task.FromResult(SchemaTypes.First(x => x.Id == typeId).Versions.Any(x => x.VersionNumber == versionNumber));
        Task ISchemaTypeInteractionService.Create(SchemaTypeDefinition schemaType, CancellationToken cancellationToken) { schemaType.Id = Guid.NewGuid(); foreach (var version in schemaType.Versions) version.Id = Guid.NewGuid(); SchemaTypes.Add(schemaType); return Task.CompletedTask; }
        Task ISchemaTypeInteractionService.UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken) { var item = SchemaTypes.First(x => x.Id == id); item.Key = key; item.Name = name; item.Description = description; item.IsActive = isActive; item.UpdatedAtUtc = updatedAtUtc; return Task.CompletedTask; }
        Task ISchemaTypeInteractionService.AddVersion(Guid typeId, SchemaTypeVersion version, CancellationToken cancellationToken) { version.Id = Guid.NewGuid(); SchemaTypes.First(x => x.Id == typeId).Versions.Add(version); return Task.CompletedTask; }
        Task ISchemaTypeInteractionService.SetVersionActive(Guid typeId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken) { SchemaTypes.First(x => x.Id == typeId).Versions.First(x => x.Id == versionId).IsActive = isActive; return Task.CompletedTask; }

        Task<IReadOnlyList<ContractFieldMetadataDefinition>> IContractFieldMetadataInteractionService.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ContractFieldMetadataDefinition>>(MetadataFields);
        Task<IReadOnlyList<ContractFieldMetadataDefinition>> IContractFieldMetadataInteractionService.GetActive(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ContractFieldMetadataDefinition>>(MetadataFields.Where(x => x.IsActive).ToList());
        Task<ContractFieldMetadataDefinition?> IContractFieldMetadataInteractionService.GetById(Guid id, bool includeVersions, CancellationToken cancellationToken) => Task.FromResult(MetadataFields.FirstOrDefault(x => x.Id == id));
        Task<bool> IContractFieldMetadataInteractionService.KeyExists(string key, Guid? excludingId, CancellationToken cancellationToken) => Task.FromResult(MetadataFields.Any(x => x.Id != excludingId && string.Equals(x.Key, key.Trim(), StringComparison.OrdinalIgnoreCase)));
        Task<bool> IContractFieldMetadataInteractionService.VersionExists(Guid metadataFieldId, string versionNumber, CancellationToken cancellationToken) => Task.FromResult(MetadataFields.First(x => x.Id == metadataFieldId).Versions.Any(x => x.VersionNumber == versionNumber));
        Task IContractFieldMetadataInteractionService.Create(ContractFieldMetadataDefinition metadataField, CancellationToken cancellationToken) { metadataField.Id = Guid.NewGuid(); foreach (var version in metadataField.Versions) version.Id = Guid.NewGuid(); MetadataFields.Add(metadataField); return Task.CompletedTask; }
        Task IContractFieldMetadataInteractionService.UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken) { var item = MetadataFields.First(x => x.Id == id); item.Key = key; item.Name = name; item.Description = description; item.IsActive = isActive; return Task.CompletedTask; }
        Task IContractFieldMetadataInteractionService.UpsertVersion(Guid metadataFieldId, ContractFieldMetadataVersion version, CancellationToken cancellationToken) { version.Id = Guid.NewGuid(); MetadataFields.First(x => x.Id == metadataFieldId).Versions.Add(version); return Task.CompletedTask; }
        Task IContractFieldMetadataInteractionService.SetVersionActive(Guid metadataFieldId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken) { MetadataFields.First(x => x.Id == metadataFieldId).Versions.First(x => x.Id == versionId).IsActive = isActive; return Task.CompletedTask; }

        Task<IReadOnlyList<EventDefinition>> IEventInteractionService.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<EventDefinition>>(Events);
        Task<EventDefinition?> IEventInteractionService.GetById(Guid id, bool includeVersions, CancellationToken cancellationToken) => Task.FromResult(Events.FirstOrDefault(x => x.Id == id));
        Task<EventVersion?> IEventInteractionService.GetVersionById(Guid versionId, CancellationToken cancellationToken) => Task.FromResult(Events.SelectMany(x => x.Versions).FirstOrDefault(x => x.Id == versionId));
        Task<bool> IEventInteractionService.VersionExists(Guid eventId, string versionNumber, CancellationToken cancellationToken) => Task.FromResult(Events.First(x => x.Id == eventId).Versions.Any(x => x.VersionNumber == versionNumber));
        Task IEventInteractionService.Create(EventDefinition eventDefinition, CancellationToken cancellationToken) { eventDefinition.Id = Guid.NewGuid(); foreach (var version in eventDefinition.Versions) version.Id = Guid.NewGuid(); Events.Add(eventDefinition); return Task.CompletedTask; }
        Task IEventInteractionService.UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken) { var item = Events.First(x => x.Id == id); item.Name = name; item.Topic = topic; item.Description = description; item.UpdatedAtUtc = updatedAtUtc; return Task.CompletedTask; }
        Task IEventInteractionService.AddVersion(Guid eventId, EventVersion version, CancellationToken cancellationToken) { version.Id = Guid.NewGuid(); Events.First(x => x.Id == eventId).Versions.Add(version); return Task.CompletedTask; }
        Task IEventInteractionService.Delete(Guid id, CancellationToken cancellationToken) { Events.First(x => x.Id == id).IsActive = false; return Task.CompletedTask; }

        Task<IReadOnlyList<CommandDefinition>> ICommandInteractionService.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CommandDefinition>>(Commands);
        Task<CommandDefinition?> ICommandInteractionService.GetById(Guid id, bool includeVersions, CancellationToken cancellationToken) => Task.FromResult(Commands.FirstOrDefault(x => x.Id == id));
        Task<CommandVersion?> ICommandInteractionService.GetVersionById(Guid versionId, CancellationToken cancellationToken) => Task.FromResult(Commands.SelectMany(x => x.Versions).FirstOrDefault(x => x.Id == versionId));
        Task<bool> ICommandInteractionService.VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken) => Task.FromResult(Commands.First(x => x.Id == commandId).Versions.Any(x => x.VersionNumber == versionNumber));
        Task ICommandInteractionService.Create(CommandDefinition commandDefinition, CancellationToken cancellationToken) { commandDefinition.Id = Guid.NewGuid(); foreach (var version in commandDefinition.Versions) version.Id = Guid.NewGuid(); Commands.Add(commandDefinition); return Task.CompletedTask; }
        Task ICommandInteractionService.UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken) { var item = Commands.First(x => x.Id == id); item.Name = name; item.Topic = topic; item.Description = description; item.UpdatedAtUtc = updatedAtUtc; return Task.CompletedTask; }
        Task ICommandInteractionService.AddVersion(Guid commandId, CommandVersion version, CancellationToken cancellationToken) { version.Id = Guid.NewGuid(); Commands.First(x => x.Id == commandId).Versions.Add(version); return Task.CompletedTask; }
        Task ICommandInteractionService.Delete(Guid id, CancellationToken cancellationToken) { Commands.First(x => x.Id == id).IsActive = false; return Task.CompletedTask; }

        Task IContractVersionPromotionService.TransitionEventVersion(Guid versionId, ContractVersionStatus targetStatus, CancellationToken cancellationToken) { Events.SelectMany(x => x.Versions).First(x => x.Id == versionId).Status = targetStatus; return Task.CompletedTask; }
        Task IContractVersionPromotionService.TransitionCommandVersion(Guid versionId, ContractVersionStatus targetStatus, CancellationToken cancellationToken) { Commands.SelectMany(x => x.Versions).First(x => x.Id == versionId).Status = targetStatus; return Task.CompletedTask; }
        IReadOnlyCollection<ContractVersionStatus> IContractVersionPromotionService.GetAllowedTargets(ContractVersionStatus currentStatus) => [ContractVersionStatus.InReview];

        Task<IReadOnlyList<ContractArtifact>> IContractArtifactRepository.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ContractArtifact>>(Artifacts);
        Task<IReadOnlyList<ContractArtifact>> IContractArtifactRepository.GetDeployed(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ContractArtifact>>(Artifacts);
        Task<ContractArtifact?> IContractArtifactRepository.GetById(Guid id, CancellationToken cancellationToken) => Task.FromResult(Artifacts.FirstOrDefault(x => x.Id == id));
        Task<IReadOnlyList<ContractArtifact>> IContractArtifactRepository.GetByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ContractArtifact>>(Artifacts.Where(x => ids.Contains(x.Id)).ToList());
        Task<ContractArtifact?> IContractArtifactRepository.GetBySourceVersion(ContractArtifactType artifactType, Guid versionId, CancellationToken cancellationToken) => Task.FromResult(Artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.VersionId == versionId));
        Task<ContractArtifact?> IContractArtifactRepository.GetByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken) => Task.FromResult(Artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));
        Task<ContractArtifact?> IContractArtifactRepository.GetDeployedByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken) => Task.FromResult(Artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber));
        Task<ContractArtifact?> IContractArtifactRepository.GetLatestDeployed(ContractArtifactType artifactType, string topic, CancellationToken cancellationToken) => Task.FromResult(Artifacts.FirstOrDefault(x => x.ArtifactType == artifactType && x.Topic == topic));
        Task IContractArtifactRepository.Create(ContractArtifact artifact, CancellationToken cancellationToken) { Artifacts.Add(artifact); return Task.CompletedTask; }
        Task<IReadOnlyList<ContractArtifact>> IControlPlaneContractCatalogService.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ContractArtifact>>(Artifacts);
        Task<ContractArtifact?> IControlPlaneContractCatalogService.GetExact(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken) => Task.FromResult(Artifacts.FirstOrDefault());
        Task<ContractArtifact?> IControlPlaneContractCatalogService.GetLatest(ContractArtifactType artifactType, string topic, CancellationToken cancellationToken) => Task.FromResult(Artifacts.FirstOrDefault());
        Task<ContractArtifact?> IControlPlaneContractCatalogService.GetEvent(string eventKey, string versionNumber, CancellationToken cancellationToken) => Task.FromResult(Artifacts.FirstOrDefault());
        Task<CommandContractArtifacts<ContractArtifact>?> IControlPlaneContractCatalogService.GetCommand(string commandKey, string versionNumber, CancellationToken cancellationToken) => Task.FromResult<CommandContractArtifacts<ContractArtifact>?>(new(commandKey, versionNumber, Artifacts.First(), null));
        Task<ContractArtifact> IContractArtifactBuilder.BuildEventArtifact(Guid versionId, CancellationToken cancellationToken) => Task.FromResult(CreateArtifact(versionId: versionId));
        Task<IReadOnlyList<ContractArtifact>> IContractArtifactBuilder.BuildCommandArtifacts(Guid versionId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ContractArtifact>>([CreateArtifact(ContractArtifactType.Command, versionId)]);
        Task<ContractArtifact> IContractArtifactBuilder.BuildCommandArtifact(Guid versionId, CancellationToken cancellationToken) => Task.FromResult(CreateArtifact(ContractArtifactType.Command, versionId));

        Task<IReadOnlyList<RuntimeEnvironment>> IRuntimeEnvironmentRepository.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RuntimeEnvironment>>(Environments);
        Task<IReadOnlyList<RuntimeEnvironment>> IRuntimeEnvironmentRepository.GetEnabled(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RuntimeEnvironment>>(Environments.Where(x => x.IsEnabled).ToList());
        Task<RuntimeEnvironment?> IRuntimeEnvironmentRepository.GetById(Guid id, CancellationToken cancellationToken) => Task.FromResult(Environments.FirstOrDefault(x => x.Id == id));
        Task IRuntimeEnvironmentRepository.Create(RuntimeEnvironment environment, CancellationToken cancellationToken) { environment.Id = Guid.NewGuid(); Environments.Add(environment); return Task.CompletedTask; }
        Task IRuntimeEnvironmentRepository.Update(RuntimeEnvironment environment, CancellationToken cancellationToken) => Task.CompletedTask;

        Task<IReadOnlyList<RuntimeNode>> IRuntimeNodeRepository.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RuntimeNode>>(RuntimeNodes);
        Task<IReadOnlyList<RuntimeNode>> IRuntimeNodeRepository.GetActiveEnabled(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RuntimeNode>>(RuntimeNodes.Where(x => x.IsEnabled && x.Status == RuntimeNodeStatus.Active).ToList());
        Task<RuntimeNode?> IRuntimeNodeRepository.GetById(Guid id, CancellationToken cancellationToken) => Task.FromResult(RuntimeNodes.FirstOrDefault(x => x.Id == id));
        Task<RuntimeNode?> IRuntimeNodeRepository.GetByCode(string code, CancellationToken cancellationToken) => Task.FromResult(RuntimeNodes.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)));
        Task<RuntimeNode?> IRuntimeNodeRepository.GetByInboundClientId(string clientId, CancellationToken cancellationToken) => Task.FromResult(RuntimeNodes.FirstOrDefault(x => x.InboundClientId == clientId));
        Task IRuntimeNodeRepository.Create(RuntimeNode runtimeNode, CancellationToken cancellationToken) { runtimeNode.Id = Guid.NewGuid(); RuntimeNodes.Add(runtimeNode); return Task.CompletedTask; }
        Task IRuntimeNodeRepository.Update(RuntimeNode runtimeNode, CancellationToken cancellationToken) => Task.CompletedTask;
        Task IRuntimeNodeRepository.SetIsEnabled(Guid id, bool isEnabled, DateTime updatedAtUtc, CancellationToken cancellationToken) { RuntimeNodes.First(x => x.Id == id).IsEnabled = isEnabled; return Task.CompletedTask; }
        Task IRuntimeNodeRepository.Delete(Guid id, DateTime deletedAtUtc, CancellationToken cancellationToken) { var node = RuntimeNodes.First(x => x.Id == id); node.IsDeleted = true; node.DeletedAtUtc = deletedAtUtc; return Task.CompletedTask; }
        Task<RuntimeNodeCredentialPackageModel> IRuntimeNodeConnectionInteractionService.GenerateCredentialPackage(Guid runtimeNodeId, string issuerBaseUrl, CancellationToken cancellationToken) => Task.FromResult(new RuntimeNodeCredentialPackageModel { Json = $"{{\"issuer\":\"{issuerBaseUrl}\"}}", Base64 = "e30=" });
        Task IRuntimeNodeConnectionInteractionService.ImportCredentialPackage(ImportRuntimeNodeCredentialPackageInput input, CancellationToken cancellationToken) => Task.CompletedTask;
        Task<RuntimeNodeConnectionValidationModel> IRuntimeNodeConnectionInteractionService.ValidateConnection(Guid runtimeNodeId, CancellationToken cancellationToken) => Task.FromResult(new RuntimeNodeConnectionValidationModel { Succeeded = true, Message = "OK" });

        Task<IReadOnlyList<ContractRelease>> IContractReleaseRepository.GetAll(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ContractRelease>>(Releases);
        Task<ContractRelease?> IContractReleaseRepository.GetById(Guid id, bool includeItems, bool includeTargets, CancellationToken cancellationToken) => Task.FromResult(Releases.FirstOrDefault(x => x.Id == id));
        Task IContractReleaseRepository.Create(ContractRelease release, CancellationToken cancellationToken) { Releases.Add(release); return Task.CompletedTask; }
        Task IContractReleaseRepository.UpdateStatus(Guid id, ContractReleaseStatus status, DateTime changedAtUtc, CancellationToken cancellationToken) { Releases.First(x => x.Id == id).Status = status; return Task.CompletedTask; }
        Task<ContractRelease> IContractReleaseInteractionService.Create(string name, string? description, IReadOnlyCollection<Guid> artifactIds, CancellationToken cancellationToken)
        {
            var release = new ContractRelease { Name = name, Description = description, Items = artifactIds.Select(id => new ContractReleaseItem { ArtifactId = id }).ToList() };
            Releases.Add(release);
            return Task.FromResult(release);
        }
        Task<ContractRelease> IContractReleaseInteractionService.Plan(Guid releaseId, IReadOnlyCollection<Guid> runtimeNodeIds, string rolloutGroup, CancellationToken cancellationToken)
        {
            var release = Releases.First(x => x.Id == releaseId);
            release.Targets = runtimeNodeIds.Select(id => new ContractReleaseTarget { RuntimeNodeId = id, ArtifactId = release.Items.First().ArtifactId, RolloutGroup = rolloutGroup, Status = ContractReleaseTargetStatus.AvailableForPull }).ToList();
            return Task.FromResult(release);
        }
        Task<ContractReleaseExecutionResult> IContractReleaseExecutionService.CreateAndExecute(string name, string? description, IReadOnlyCollection<Guid> artifactIds, IReadOnlyCollection<Guid> runtimeNodeIds, string rolloutGroup, string initiatedBy, CancellationToken cancellationToken)
        {
            var release = new ContractRelease { Name = name, Description = description, Items = artifactIds.Select(id => new ContractReleaseItem { ArtifactId = id }).ToList() };
            Releases.Add(release);
            return Task.FromResult(CreateExecutionResult(release));
        }
        Task<ContractReleaseExecutionResult> IContractReleaseExecutionService.Execute(Guid releaseId, string initiatedBy, CancellationToken cancellationToken) => Task.FromResult(CreateExecutionResult(Releases.First(x => x.Id == releaseId)));

        Task<IReadOnlyList<KnOwlSubject>> IKnOwlSecurityStore.GetSubjects(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<KnOwlSubject>>(subjects);
        Task<KnOwlSubject?> IKnOwlSecurityStore.GetSubject(string provider, string subjectId, CancellationToken cancellationToken) => Task.FromResult(subjects.FirstOrDefault(x => x.Provider == provider && x.SubjectId == subjectId));
        Task<IReadOnlyList<KnOwlRoleAssignment>> IKnOwlSecurityStore.GetRoleAssignments(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<KnOwlRoleAssignment>>(roles);
        Task<IReadOnlyList<KnOwlRoleAssignment>> IKnOwlSecurityStore.GetRoleAssignments(string provider, string subjectId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<KnOwlRoleAssignment>>(roles.Where(x => x.Provider == provider && x.SubjectId == subjectId).ToList());
        Task<IReadOnlyList<KnOwlPermissionAssignment>> IKnOwlSecurityStore.GetPermissionAssignments(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<KnOwlPermissionAssignment>>(permissions);
        Task<IReadOnlyList<KnOwlPermissionAssignment>> IKnOwlSecurityStore.GetPermissionAssignments(string provider, string subjectId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<KnOwlPermissionAssignment>>(permissions.Where(x => x.Provider == provider && x.SubjectId == subjectId).ToList());
        Task<IReadOnlyList<KnOwlExternalGroupRoleAssignment>> IKnOwlSecurityStore.GetExternalGroupRoleAssignments(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<KnOwlExternalGroupRoleAssignment>>(groupRoles);
        Task<IReadOnlyList<KnOwlExternalGroupRoleAssignment>> IKnOwlSecurityStore.GetExternalGroupRoleAssignments(string provider, IReadOnlyCollection<string> externalGroupIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<KnOwlExternalGroupRoleAssignment>>(groupRoles.Where(x => x.Provider == provider && externalGroupIds.Contains(x.ExternalGroupId)).ToList());
        Task<bool> IKnOwlSecurityStore.HasEnabledAdmin(CancellationToken cancellationToken) => Task.FromResult(roles.Any(x => x.IsEnabled && x.Role == "Admin"));
        Task IKnOwlSecurityStore.SynchronizeBootstrapAdmin(KnOwl.Security.Subjects.KnOwlExternalSubject subject, CancellationToken cancellationToken) => Task.CompletedTask;
        Task<KnOwlSubject> IKnOwlSecurityStore.UpsertSubject(KnOwlSubject subject, CancellationToken cancellationToken) { subjects.Add(subject); return Task.FromResult(subject); }
        Task<KnOwlRoleAssignment> IKnOwlSecurityStore.AssignRole(KnOwlRoleAssignment assignment, CancellationToken cancellationToken) { roles.Add(assignment); return Task.FromResult(assignment); }
        Task<KnOwlPermissionAssignment> IKnOwlSecurityStore.AssignPermission(KnOwlPermissionAssignment assignment, CancellationToken cancellationToken) { permissions.Add(assignment); return Task.FromResult(assignment); }
        Task<KnOwlExternalGroupRoleAssignment> IKnOwlSecurityStore.AssignExternalGroupRole(KnOwlExternalGroupRoleAssignment assignment, CancellationToken cancellationToken) { groupRoles.Add(assignment); return Task.FromResult(assignment); }

        private static ContractReleaseExecutionResult CreateExecutionResult(ContractRelease release)
            => new()
            {
                ReleaseId = release.Id,
                TotalTargets = 1,
                Succeeded = 1,
                Results =
                [
                    new RuntimeArtifactDeliveryResult
                    {
                        ReleaseTargetId = release.Targets.FirstOrDefault()?.Id ?? Guid.NewGuid(),
                        Succeeded = true,
                        Status = "Delivered",
                        Message = "OK"
                    }
                ]
            };

        private static ContractArtifact CreateArtifact(ContractArtifactType artifactType = ContractArtifactType.Event, Guid? versionId = null)
            => new()
            {
                Id = Guid.NewGuid(),
                ArtifactType = artifactType,
                DefinitionId = Guid.NewGuid(),
                VersionId = versionId ?? Guid.NewGuid(),
                Name = "Customer Created",
                Topic = "customer.created",
                VersionNumber = "1.0.0",
                PayloadSchemaJson = "{}",
                ContentHash = Guid.NewGuid().ToString("N"),
                SourceStatus = ContractVersionStatus.Deployed.ToString()
            };
    }
}
