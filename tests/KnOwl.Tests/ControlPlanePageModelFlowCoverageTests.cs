using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Application.Distribution.Artifacts;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CommandEditModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Commands.EditModel;
using CommandIndexModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Commands.IndexModel;
using CommandNewVersionModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Commands.NewVersionModel;
using CommandVersionModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Commands.VersionModel;
using CommandViewModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Commands.ViewModel;
using EventEditModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Events.EditModel;
using EventIndexModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Events.IndexModel;
using EventNewModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Events.NewModel;
using EventNewVersionModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Events.NewVersionModel;
using EventVersionModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Events.VersionModel;
using EventViewModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Events.ViewModel;
using MetadataIndexModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.MetadataFields.IndexModel;
using MetadataVersionModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.MetadataFields.VersionModel;
using MetadataViewModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.MetadataFields.ViewModel;
using RuntimeEnvironmentIndexModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.RuntimeEnvironments.IndexModel;
using RuntimeEnvironmentInput = KnOwl.ControlPlane.WebUI.Pages.Contracts.RuntimeEnvironments.RuntimeEnvironmentInput;
using TypeIndexModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Types.IndexModel;
using TypeVersionModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Types.VersionModel;
using TypeViewModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Types.ViewModel;

namespace KnOwl.Tests;

public sealed class ControlPlanePageModelFlowCoverageTests
{
    [Fact]
    public void EventNewRedirectsToEventIndex()
    {
        var model = new EventNewModel();

        Assert.Equal("/Contracts/Events/Index", Assert.IsType<RedirectToPageResult>(model.OnGet()).PageName);
        Assert.Equal("/Contracts/Events/Index", Assert.IsType<RedirectToPageResult>(model.OnPost()).PageName);
    }

    [Fact]
    public async Task NewVersionPagesCoverMissingEntitiesAndVersionFallbacks()
    {
        var schemaTypes = new SchemaTypeServiceFake();
        var metadataFields = new MetadataFieldServiceFake();
        var eventWithoutVersions = new EventDefinition { Id = Guid.NewGuid(), Name = "Empty Event", Topic = "empty.event" };
        var eventWithNonSemanticVersion = CreateEvent();
        eventWithNonSemanticVersion.Versions.Clear();
        eventWithNonSemanticVersion.Versions.Add(new EventVersion
        {
            EventDefinitionId = eventWithNonSemanticVersion.Id,
            VersionNumber = "preview",
            PayloadSchemaJson = "{}",
            CreatedAtUtc = DateTime.UtcNow
        });

        var missingEvent = new EventNewVersionModel(new EventServiceFake(), schemaTypes, metadataFields);
        var emptyEvent = new EventNewVersionModel(new EventServiceFake([eventWithoutVersions]), schemaTypes, metadataFields);
        var nonSemanticEvent = new EventNewVersionModel(new EventServiceFake([eventWithNonSemanticVersion]), schemaTypes, metadataFields);
        var missingCommand = new CommandNewVersionModel(new CommandServiceFake(), schemaTypes, metadataFields);

        Assert.IsType<NotFoundResult>(await missingEvent.OnGetAsync(Guid.NewGuid(), CancellationToken.None));
        Assert.IsType<PageResult>(await emptyEvent.OnGetAsync(eventWithoutVersions.Id, CancellationToken.None));
        Assert.Equal("1.0.0", emptyEvent.Input.Version);
        Assert.IsType<PageResult>(await nonSemanticEvent.OnGetAsync(eventWithNonSemanticVersion.Id, CancellationToken.None));
        Assert.Equal("preview.1", nonSemanticEvent.Input.Version);
        Assert.IsType<NotFoundResult>(await missingCommand.OnGetAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task EventViewCoversSelectionSearchJsonAndTransitionBranches()
    {
        var eventDefinition = CreateEvent();
        var promotion = new PromotionServiceFake();
        var artifactBuilder = new ArtifactBuilderFake();
        var model = new EventViewModel(new EventServiceFake([eventDefinition]), promotion, artifactBuilder)
        {
            Search = " draft "
        };

        Assert.IsType<NotFoundResult>(await model.OnGetAsync(null, null, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await new EventViewModel(new EventServiceFake(), promotion, artifactBuilder).OnGetAsync(Guid.NewGuid(), null, CancellationToken.None));

        var result = await model.OnGetAsync(eventDefinition.Id, "1.0.0", CancellationToken.None);
        var selected = Assert.IsType<PageResult>(result);

        Assert.NotNull(selected);
        Assert.Equal("1.0.0", model.SelectedVersion?.VersionNumber);
        Assert.Single(model.Versions);
        Assert.Contains(Environment.NewLine, model.FormattedPayloadSchemaJson);
        Assert.Contains(ContractVersionStatus.InReview, model.AllowedTargets);
        Assert.Equal("{}", EventViewModel.FormatJson(""));
        Assert.Equal("{", EventViewModel.FormatJson("{"));

        var redirect = Assert.IsType<RedirectToPageResult>(
            await model.OnPostTransitionAsync(eventDefinition.Id, eventDefinition.Versions.First().Id, ContractVersionStatus.InReview, CancellationToken.None));
        Assert.Equal(eventDefinition.Id, redirect.RouteValues?["id"]);
        Assert.Contains("InReview", model.StatusMessage);
        Assert.Equal(ContractVersionStatus.InReview, promotion.LastEventTarget);
        Assert.False(artifactBuilder.EventArtifactWasBuilt);

        await model.OnPostTransitionAsync(eventDefinition.Id, eventDefinition.Versions.First().Id, ContractVersionStatus.Deployed, CancellationToken.None);
        Assert.True(artifactBuilder.EventArtifactWasBuilt);
        Assert.Contains("events.customer.created@1.0.0", model.StatusMessage);

        promotion.Exception = new InvalidOperationException("invalid transition");
        await model.OnPostTransitionAsync(eventDefinition.Id, eventDefinition.Versions.First().Id, ContractVersionStatus.Archived, CancellationToken.None);
        Assert.Equal("invalid transition", model.StatusMessage);
    }

    [Fact]
    public async Task EventVersionCoversLoadAndTransitionBranches()
    {
        var eventDefinition = CreateEvent();
        var version = eventDefinition.Versions.First();
        var promotion = new PromotionServiceFake();
        var artifactBuilder = new ArtifactBuilderFake();
        var model = new EventVersionModel(new EventServiceFake([eventDefinition]), promotion, artifactBuilder);

        Assert.IsType<NotFoundResult>(await model.OnGetAsync(Guid.NewGuid(), version.Id, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await model.OnGetAsync(eventDefinition.Id, Guid.NewGuid(), CancellationToken.None));

        Assert.IsType<PageResult>(await model.OnGetAsync(eventDefinition.Id, version.Id, CancellationToken.None));
        Assert.Equal(version.Id, model.Version?.Id);
        Assert.Contains(Environment.NewLine, model.FormattedPayloadSchemaJson);
        Assert.Equal("{}", EventVersionModel.FormatJson(null));
        Assert.Equal("{", EventVersionModel.FormatJson("{"));

        var redirect = Assert.IsType<RedirectToPageResult>(
            await model.OnPostTransitionAsync(eventDefinition.Id, version.Id, ContractVersionStatus.Deployed, CancellationToken.None));
        Assert.Equal(eventDefinition.Id, redirect.RouteValues?["id"]);
        Assert.Equal(version.Id, redirect.RouteValues?["versionId"]);
        Assert.True(artifactBuilder.EventArtifactWasBuilt);
        Assert.Contains("Artifact generated", model.StatusMessage);

        promotion.Exception = new KeyNotFoundException("missing version");
        await model.OnPostTransitionAsync(eventDefinition.Id, version.Id, ContractVersionStatus.Archived, CancellationToken.None);
        Assert.Equal("missing version", model.StatusMessage);
    }

    [Fact]
    public async Task CommandViewFormatsFiltersAndTransitionsVersions()
    {
        var command = CreateCommand();
        var promotion = new PromotionServiceFake();
        var artifactBuilder = new ArtifactBuilderFake();
        var commands = new CommandServiceFake([command]);
        var view = new CommandViewModel(commands, promotion, artifactBuilder)
        {
            Search = " request and reply "
        };

        Assert.IsType<NotFoundResult>(await view.OnGetAsync(null, null, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await new CommandViewModel(new CommandServiceFake(), promotion, artifactBuilder).OnGetAsync(Guid.NewGuid(), null, CancellationToken.None));

        Assert.IsType<PageResult>(await view.OnGetAsync(command.Id, "1.0.0", CancellationToken.None));
        Assert.Single(view.Versions);
        Assert.Contains(Environment.NewLine, view.FormattedPayloadSchemaJson);
        Assert.Contains(Environment.NewLine, view.FormattedReplyPayloadSchemaJson);
        Assert.Equal("{}", CommandViewModel.FormatJson(null));
        Assert.Equal("{", CommandViewModel.FormatJson("{"));

        await view.OnPostTransitionAsync(command.Id, command.Versions.First().Id, ContractVersionStatus.Deployed, CancellationToken.None);
        Assert.True(artifactBuilder.CommandArtifactWasBuilt);
        Assert.Contains("commands.customer.register@1.0.0", view.StatusMessage);

        promotion.Exception = new InvalidOperationException("blocked");
        await view.OnPostTransitionAsync(command.Id, command.Versions.First().Id, ContractVersionStatus.Archived, CancellationToken.None);
        Assert.Equal("blocked", view.StatusMessage);
    }

    [Fact]
    public async Task CommandVersionLoadsFormatsAndTransitionsVersion()
    {
        var command = CreateCommand();
        var commands = new CommandServiceFake([command]);
        var version = command.Versions.First();
        var versionModel = new CommandVersionModel(commands, new PromotionServiceFake(), new ArtifactBuilderFake());
        Assert.IsType<NotFoundResult>(await versionModel.OnGetAsync(Guid.NewGuid(), version.Id, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await versionModel.OnGetAsync(command.Id, Guid.NewGuid(), CancellationToken.None));
        Assert.IsType<PageResult>(await versionModel.OnGetAsync(command.Id, version.Id, CancellationToken.None));
        Assert.Equal(version.Id, versionModel.Version?.Id);
        Assert.Equal("{}", CommandVersionModel.FormatJson(""));
        Assert.Equal("{", CommandVersionModel.FormatJson("{"));

        var versionPromotion = new PromotionServiceFake();
        var versionArtifactBuilder = new ArtifactBuilderFake();
        versionModel = new CommandVersionModel(commands, versionPromotion, versionArtifactBuilder);
        var versionRedirect = Assert.IsType<RedirectToPageResult>(
            await versionModel.OnPostTransitionAsync(command.Id, version.Id, ContractVersionStatus.InReview, CancellationToken.None));
        Assert.Equal(command.Id, versionRedirect.RouteValues?["id"]);
        Assert.Equal(version.Id, versionRedirect.RouteValues?["versionId"]);
        Assert.Contains("InReview", versionModel.StatusMessage);

        await versionModel.OnPostTransitionAsync(command.Id, version.Id, ContractVersionStatus.Deployed, CancellationToken.None);
        Assert.True(versionArtifactBuilder.CommandArtifactWasBuilt);
        Assert.Contains("Artifact generated", versionModel.StatusMessage);

        versionPromotion.Exception = new KeyNotFoundException("missing command version");
        await versionModel.OnPostTransitionAsync(command.Id, version.Id, ContractVersionStatus.Archived, CancellationToken.None);
        Assert.Equal("missing command version", versionModel.StatusMessage);
    }

    [Fact]
    public async Task EventIndexFiltersBySearchAndDeletesSelectedEvent()
    {
        var eventDefinition = CreateEvent();
        eventDefinition.Versions.First().Comment = null;
        var events = new EventServiceFake([eventDefinition]);
        var eventIndex = new EventIndexModel(events)
        {
            Search = " approved "
        };

        await eventIndex.OnGetAsync(CancellationToken.None);
        Assert.Single(eventIndex.Events);
        Assert.Equal(1, eventIndex.TotalEvents);

        eventIndex.Search = "missing";
        await eventIndex.OnGetAsync(CancellationToken.None);
        Assert.Empty(eventIndex.Events);

        var eventDelete = Assert.IsType<RedirectToPageResult>(
            await eventIndex.OnPostDeleteAsync(eventDefinition.Id, CancellationToken.None));
        Assert.Null(eventDelete.PageName);
        Assert.Equal(eventDefinition.Id, events.LastDeletedId);
    }

    [Fact]
    public async Task CommandIndexFiltersBySearchAndDeletesSelectedCommand()
    {
        var command = CreateCommand();
        command.Versions.First().Comment = null;
        var commands = new CommandServiceFake([command]);
        var commandIndex = new CommandIndexModel(commands)
        {
            Search = " approved "
        };

        await commandIndex.OnGetAsync(CancellationToken.None);
        Assert.Single(commandIndex.Commands);
        Assert.Equal(1, commandIndex.TotalCommands);

        commandIndex.Search = "missing";
        await commandIndex.OnGetAsync(CancellationToken.None);
        Assert.Empty(commandIndex.Commands);

        var commandDelete = Assert.IsType<RedirectToPageResult>(
            await commandIndex.OnPostDeleteAsync(command.Id, CancellationToken.None));
        Assert.Null(commandDelete.PageName);
        Assert.Equal(command.Id, commands.LastDeletedId);
    }

    [Fact]
    public async Task TypeIndexOrdersSystemTypesAfterCustomTypes()
    {
        var custom = new SchemaTypeDefinition
        {
            Name = "Customer",
            Key = "customer",
            IsSystem = false,
            IsActive = true,
            Versions = { new SchemaTypeVersion { VersionNumber = "1.0.0", DefinitionJson = "{}" } }
        };
        var system = new SchemaTypeDefinition
        {
            Name = "Date",
            Key = "date",
            IsSystem = true,
            IsActive = true
        };
        var model = new TypeIndexModel(new SchemaTypeServiceFake([system, custom]));

        await model.OnGetAsync(CancellationToken.None);

        Assert.Collection(
            model.Types,
            item => Assert.Equal(custom.Id, item.Id),
            item => Assert.Equal(system.Id, item.Id));
    }

    [Fact]
    public async Task TypeVersionViewsSelectFormatAndDeactivateVersions()
    {
        var type = new SchemaTypeDefinition
        {
            Name = "Customer",
            Key = "customer",
            Description = "Reusable type",
            Versions =
            {
                new SchemaTypeVersion
                {
                    VersionNumber = "1.0.0",
                    Comment = "Current",
                    DefinitionJson = """{"baseType":"string","schema":{"type":"string"}}""",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
                },
                new SchemaTypeVersion
                {
                    VersionNumber = "0.9.0",
                    Comment = "Legacy",
                    DefinitionJson = "{",
                    IsActive = false,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
                }
            }
        };
        var schemaTypes = new SchemaTypeServiceFake([type]);
        var typeView = new TypeViewModel(schemaTypes) { Search = " legacy " };

        Assert.IsType<NotFoundResult>(await typeView.OnGetAsync(Guid.NewGuid(), cancellationToken: CancellationToken.None));
        Assert.IsType<PageResult>(await typeView.OnGetAsync(type.Id, "1.0.0", CancellationToken.None));
        Assert.Equal("1.0.0", typeView.SelectedVersion?.VersionNumber);
        Assert.Single(typeView.Versions);
        Assert.Equal("string", typeView.SelectedDefinition.BaseType);
        Assert.Contains(Environment.NewLine, typeView.FormattedJsonSchema);
        Assert.Equal("{}", TypeViewModel.ReadTypeDefinition("{").SchemaJson);

        var typeDeactivate = Assert.IsType<RedirectToPageResult>(
            await typeView.OnPostDeactivateVersionAsync(type.Id, type.Versions.First().Id, CancellationToken.None));
        Assert.Equal(type.Id, typeDeactivate.RouteValues?["id"]);
        Assert.False(type.Versions.First().IsActive);

        schemaTypes.ThrowOnSetVersionActive = true;
        Assert.IsType<RedirectToPageResult>(
            await typeView.OnPostDeactivateVersionAsync(type.Id, Guid.NewGuid(), CancellationToken.None));

        var typeVersion = new TypeVersionModel(schemaTypes);
        Assert.IsType<NotFoundResult>(await typeVersion.OnGetAsync(Guid.NewGuid(), type.Versions.First().Id, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await typeVersion.OnGetAsync(type.Id, Guid.NewGuid(), CancellationToken.None));
        Assert.IsType<PageResult>(await typeVersion.OnGetAsync(type.Id, type.Versions.Last().Id, CancellationToken.None));
        Assert.Equal("{}", typeVersion.FormattedJsonSchema);
        schemaTypes.ThrowOnSetVersionActive = false;
        Assert.IsType<RedirectToPageResult>(
            await typeVersion.OnPostDeactivateVersionAsync(type.Id, type.Versions.Last().Id, CancellationToken.None));
        schemaTypes.ThrowOnSetVersionActive = true;
        Assert.IsType<RedirectToPageResult>(
            await typeVersion.OnPostDeactivateVersionAsync(type.Id, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task MetadataVersionViewsSelectFormatAndDeactivateVersions()
    {
        var field = new ContractFieldMetadataDefinition
        {
            Name = "Trace Id",
            Key = "trace-id",
            Description = "Correlation",
            IsActive = true,
            Versions =
            {
                new ContractFieldMetadataVersion
                {
                    VersionNumber = "1.0.0",
                    Comment = "Current",
                    DefinitionJson = """{"name":"Trace Id","key":"trace-id"}""",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
                },
                new ContractFieldMetadataVersion
                {
                    VersionNumber = "0.9.0",
                    Comment = "Legacy",
                    DefinitionJson = "{",
                    IsActive = false,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
                }
            }
        };
        var metadataFields = new MetadataFieldServiceFake([field]);
        var metadataView = new MetadataViewModel(metadataFields) { Search = " legacy " };

        Assert.IsType<NotFoundResult>(await metadataView.OnGetAsync(Guid.NewGuid(), cancellationToken: CancellationToken.None));
        Assert.IsType<PageResult>(await metadataView.OnGetAsync(field.Id, "1.0.0", CancellationToken.None));
        Assert.Equal("1.0.0", metadataView.SelectedVersion?.VersionNumber);
        Assert.Single(metadataView.Versions);
        Assert.Contains(Environment.NewLine, metadataView.FormattedDefinitionJson);
        Assert.NotEmpty(metadataView.ButterMorphContext);

        var metadataDeactivate = Assert.IsType<RedirectToPageResult>(
            await metadataView.OnPostDeactivateVersionAsync(field.Id, field.Versions.First().Id, CancellationToken.None));
        Assert.Equal(field.Id, metadataDeactivate.RouteValues?["id"]);
        Assert.False(field.Versions.First().IsActive);

        metadataFields.ThrowOnSetVersionActive = true;
        Assert.IsType<RedirectToPageResult>(
            await metadataView.OnPostDeactivateVersionAsync(field.Id, Guid.NewGuid(), CancellationToken.None));

        var metadataVersion = new MetadataVersionModel(metadataFields);
        Assert.IsType<NotFoundResult>(await metadataVersion.OnGetAsync(Guid.NewGuid(), field.Versions.First().Id, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await metadataVersion.OnGetAsync(field.Id, Guid.NewGuid(), CancellationToken.None));
        Assert.IsType<PageResult>(await metadataVersion.OnGetAsync(field.Id, field.Versions.Last().Id, CancellationToken.None));
        Assert.Equal("{", metadataVersion.FormattedDefinitionJson);
        metadataFields.ThrowOnSetVersionActive = false;
        Assert.IsType<RedirectToPageResult>(
            await metadataVersion.OnPostDeactivateVersionAsync(field.Id, field.Versions.Last().Id, CancellationToken.None));
        metadataFields.ThrowOnSetVersionActive = true;
        Assert.IsType<RedirectToPageResult>(
            await metadataVersion.OnPostDeactivateVersionAsync(field.Id, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task EventEditLoadsValidatesAndSavesTrimmedUpdates()
    {
        var eventDefinition = CreateEvent();
        var events = new EventServiceFake([eventDefinition]);
        var eventEdit = new EventEditModel(events);

        Assert.IsType<NotFoundResult>(await eventEdit.OnGetAsync(Guid.NewGuid(), CancellationToken.None));
        Assert.IsType<PageResult>(await eventEdit.OnGetAsync(eventDefinition.Id, CancellationToken.None));
        Assert.Equal(eventDefinition.Topic, eventEdit.Input.Topic);

        eventEdit.ModelState.AddModelError("Input.Name", "bad");
        Assert.IsType<PageResult>(await eventEdit.OnPostAsync(CancellationToken.None));

        Assert.IsType<NotFoundResult>(await new EventEditModel(events) { Id = Guid.NewGuid() }.OnPostAsync(CancellationToken.None));

        var eventSuccess = new EventEditModel(events)
        {
            Id = eventDefinition.Id,
            Input = new EventEditModel.EventInput
            {
                Name = " Updated Event ",
                Topic = " events.updated ",
                Description = " "
            }
        };
        Assert.IsType<RedirectToPageResult>(await eventSuccess.OnPostAsync(CancellationToken.None));
        Assert.Equal("Updated Event", events.LastUpdatedName);
        Assert.Equal("events.updated", events.LastUpdatedTopic);
        Assert.Null(events.LastUpdatedDescription);
    }

    [Fact]
    public async Task CommandEditLoadsValidatesAndSavesTrimmedUpdates()
    {
        var command = CreateCommand();
        var commands = new CommandServiceFake([command]);
        var commandEdit = new CommandEditModel(commands);

        Assert.IsType<NotFoundResult>(await commandEdit.OnGetAsync(Guid.NewGuid(), CancellationToken.None));
        Assert.IsType<PageResult>(await commandEdit.OnGetAsync(command.Id, CancellationToken.None));
        Assert.Equal(command.Topic, commandEdit.Input.Topic);

        commandEdit.ModelState.AddModelError("Input.Name", "bad");
        Assert.IsType<PageResult>(await commandEdit.OnPostAsync(CancellationToken.None));

        Assert.IsType<NotFoundResult>(await new CommandEditModel(commands) { Id = Guid.NewGuid() }.OnPostAsync(CancellationToken.None));

        var commandSuccess = new CommandEditModel(commands)
        {
            Id = command.Id,
            Input = new CommandEditModel.CommandInput
            {
                Name = " Updated Command ",
                Topic = null,
                Description = " Command description "
            }
        };
        Assert.IsType<RedirectToPageResult>(await commandSuccess.OnPostAsync(CancellationToken.None));
        Assert.Equal("Updated Command", commands.LastUpdatedName);
        Assert.Equal(string.Empty, commands.LastUpdatedTopic);
        Assert.Equal("Command description", commands.LastUpdatedDescription);
    }

    [Fact]
    public async Task RuntimeEnvironmentIndexCoversSearchCreateUpdateAndValidationBranches()
    {
        var existing = new RuntimeEnvironment
        {
            Name = "Production",
            Code = "prod",
            Description = "Primary",
            IsEnabled = true
        };
        var repository = new RuntimeEnvironmentRepositoryFake([existing]);
        var model = new RuntimeEnvironmentIndexModel(repository)
        {
            Search = " disabled "
        };

        await model.OnGetAsync(CancellationToken.None);
        Assert.Empty(model.Environments);
        Assert.Equal(1, model.TotalEnvironments);

        model.ModelState.AddModelError("Input.Name", "bad");
        Assert.IsType<PageResult>(await model.OnPostAsync(CancellationToken.None));
        Assert.True(model.ShowEnvironmentModal);

        var create = new RuntimeEnvironmentIndexModel(repository)
        {
            Input = new RuntimeEnvironmentInput
            {
                Name = " QA ",
                Code = " qa ",
                Description = " ",
                IsEnabled = false
            }
        };
        Assert.IsType<RedirectToPageResult>(await create.OnPostAsync(CancellationToken.None));
        Assert.Equal("QA", repository.Environments.Last().Name);
        Assert.Equal("qa", repository.Environments.Last().Code);
        Assert.Null(repository.Environments.Last().Description);
        Assert.False(repository.Environments.Last().IsEnabled);

        var update = new RuntimeEnvironmentIndexModel(repository)
        {
            Input = new RuntimeEnvironmentInput
            {
                Id = existing.Id,
                Name = " Production Updated ",
                Code = " prod2 ",
                Description = " Updated ",
                IsEnabled = false
            }
        };
        Assert.IsType<RedirectToPageResult>(await update.OnPostAsync(CancellationToken.None));
        Assert.Equal("Production Updated", existing.Name);
        Assert.Equal("prod2", existing.Code);
        Assert.Equal("Updated", existing.Description);
        Assert.False(existing.IsEnabled);

        var missing = new RuntimeEnvironmentIndexModel(repository)
        {
            Input = new RuntimeEnvironmentInput { Id = Guid.NewGuid(), Name = "Missing", Code = "missing" }
        };
        Assert.IsType<NotFoundResult>(await missing.OnPostAsync(CancellationToken.None));
    }

    [Fact]
    public async Task MetadataFieldIndexCoversSearchInactiveVersionsAndDeactivateBranches()
    {
        var active = new ContractFieldMetadataDefinition
        {
            Name = "Trace Id",
            Key = "trace-id",
            Description = "Correlation",
            IsActive = true,
            Versions =
            {
                new ContractFieldMetadataVersion
                {
                    VersionNumber = "1.0.0",
                    DefinitionJson = """{"name":"Trace Id","dataType":"string","isRequired":true,"appliesTo":["Schema","Field"],"validation":{}}""",
                    IsActive = true
                }
            }
        };
        var withoutActiveVersion = new ContractFieldMetadataDefinition
        {
            Name = "Dormant",
            Key = "dormant",
            IsActive = false,
            Versions =
            {
                new ContractFieldMetadataVersion
                {
                    VersionNumber = "0.1.0",
                    DefinitionJson = "{}",
                    IsActive = false
                }
            }
        };
        var metadata = new MetadataFieldServiceFake([active, withoutActiveVersion]);
        var model = new MetadataIndexModel(metadata)
        {
            Search = " required "
        };

        await model.OnGetAsync(CancellationToken.None);

        var item = Assert.Single(model.Fields);
        Assert.Equal("Trace Id", item.Name);
        Assert.Equal("string", item.DataType);
        Assert.True(item.IsRequired);
        Assert.Equal(2, model.TotalFields);

        Assert.IsType<RedirectToPageResult>(await model.OnPostDeactivateAsync(active.Id, CancellationToken.None));
        Assert.Equal(active.Id, metadata.LastUpdatedId);
        Assert.False(metadata.LastUpdatedIsActive);

        Assert.IsType<RedirectToPageResult>(await model.OnPostDeactivateAsync(Guid.NewGuid(), CancellationToken.None));

        var allModel = new MetadataIndexModel(metadata);
        await allModel.OnGetAsync(CancellationToken.None);
        Assert.Contains(allModel.Fields, x => x.Key == "dormant" && x.Version == string.Empty && x.DataType == string.Empty);
    }

    private static EventDefinition CreateEvent()
    {
        var entity = new EventDefinition
        {
            Name = "Customer Created",
            Topic = "events.customer.created",
            Description = "Event description"
        };
        entity.Versions.Add(new EventVersion
        {
            EventDefinitionId = entity.Id,
            VersionNumber = "1.0.0",
            PayloadSchemaJson = """{"type":"object","properties":{"id":{"type":"string"}}}""",
            Comment = "draft comment",
            Status = ContractVersionStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        });
        entity.Versions.Add(new EventVersion
        {
            EventDefinitionId = entity.Id,
            VersionNumber = "1.0.1",
            PayloadSchemaJson = "{",
            Comment = "approved comment",
            Status = ContractVersionStatus.Approved,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        return entity;
    }

    private static CommandDefinition CreateCommand()
    {
        var entity = new CommandDefinition
        {
            Name = "Register Customer",
            Topic = "commands.customer.register",
            Description = "Command description"
        };
        entity.Versions.Add(new CommandVersion
        {
            CommandDefinitionId = entity.Id,
            VersionNumber = "1.0.0",
            PayloadSchemaJson = """{"type":"object","properties":{"id":{"type":"string"}}}""",
            ReplyPayloadSchemaJson = """{"type":"object","properties":{"accepted":{"type":"boolean"}}}""",
            Comment = "draft comment",
            Status = ContractVersionStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        });
        entity.Versions.Add(new CommandVersion
        {
            CommandDefinitionId = entity.Id,
            VersionNumber = "1.0.1",
            PayloadSchemaJson = "{",
            ReplyPayloadSchemaJson = null,
            Comment = "request only",
            Status = ContractVersionStatus.Approved,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        return entity;
    }

    private sealed class PromotionServiceFake : IContractVersionPromotionService
    {
        public Exception? Exception { get; set; }
        public ContractVersionStatus? LastEventTarget { get; private set; }
        public ContractVersionStatus? LastCommandTarget { get; private set; }

        public Task TransitionEventVersion(Guid versionId, ContractVersionStatus targetStatus, CancellationToken cancellationToken = default)
        {
            if (Exception is not null) throw Exception;
            LastEventTarget = targetStatus;
            return Task.CompletedTask;
        }

        public Task TransitionCommandVersion(Guid versionId, ContractVersionStatus targetStatus, CancellationToken cancellationToken = default)
        {
            if (Exception is not null) throw Exception;
            LastCommandTarget = targetStatus;
            return Task.CompletedTask;
        }

        public IReadOnlyCollection<ContractVersionStatus> GetAllowedTargets(ContractVersionStatus currentStatus)
            => currentStatus switch
            {
                ContractVersionStatus.Draft => [ContractVersionStatus.InReview, ContractVersionStatus.Abandoned],
                ContractVersionStatus.Approved => [ContractVersionStatus.Deployed],
                _ => []
            };
    }

    private sealed class ArtifactBuilderFake : IContractArtifactBuilder
    {
        public bool EventArtifactWasBuilt { get; private set; }
        public bool CommandArtifactWasBuilt { get; private set; }

        public Task<ContractArtifact> BuildEventArtifact(Guid versionId, CancellationToken cancellationToken = default)
        {
            EventArtifactWasBuilt = true;
            return Task.FromResult(CreateArtifact(ContractArtifactType.Event, "Customer Created", "events.customer.created", versionId));
        }

        public Task<IReadOnlyList<ContractArtifact>> BuildCommandArtifacts(Guid versionId, CancellationToken cancellationToken = default)
        {
            CommandArtifactWasBuilt = true;
            return Task.FromResult<IReadOnlyList<ContractArtifact>>([CreateArtifact(ContractArtifactType.Command, "Register Customer", "commands.customer.register", versionId)]);
        }

        public Task<ContractArtifact> BuildCommandArtifact(Guid versionId, CancellationToken cancellationToken = default)
        {
            CommandArtifactWasBuilt = true;
            return Task.FromResult(CreateArtifact(ContractArtifactType.Command, "Register Customer", "commands.customer.register", versionId));
        }

        private static ContractArtifact CreateArtifact(ContractArtifactType type, string name, string topic, Guid versionId)
            => new()
            {
                ArtifactType = type,
                DefinitionId = Guid.NewGuid(),
                VersionId = versionId,
                Name = name,
                Topic = topic,
                VersionNumber = "1.0.0",
                PayloadSchemaJson = "{}",
                ContentHash = "hash",
                SourceStatus = ContractVersionStatus.Deployed.ToString()
            };
    }

    private sealed class EventServiceFake(IReadOnlyList<EventDefinition>? seed = null) : IEventInteractionService
    {
        private readonly List<EventDefinition> events = seed?.ToList() ?? [];

        public string? LastUpdatedName { get; private set; }
        public string? LastUpdatedTopic { get; private set; }
        public string? LastUpdatedDescription { get; private set; }
        public Guid? LastDeletedId { get; private set; }

        public Task<IReadOnlyList<EventDefinition>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EventDefinition>>(events);

        public Task<EventDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
            => Task.FromResult(events.FirstOrDefault(x => x.Id == id));

        public Task<EventVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult(events.SelectMany(x => x.Versions).FirstOrDefault(x => x.Id == versionId));

        public Task<bool> VersionExists(Guid eventId, string versionNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(events.FirstOrDefault(x => x.Id == eventId)?.Versions.Any(x => x.VersionNumber == versionNumber) == true);

        public Task Create(EventDefinition eventDefinition, CancellationToken cancellationToken = default)
        {
            events.Add(eventDefinition);
            return Task.CompletedTask;
        }

        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            LastUpdatedName = name;
            LastUpdatedTopic = topic;
            LastUpdatedDescription = description;
            return Task.CompletedTask;
        }

        public Task AddVersion(Guid eventId, EventVersion version, CancellationToken cancellationToken = default)
        {
            events.First(x => x.Id == eventId).Versions.Add(version);
            return Task.CompletedTask;
        }

        public Task Delete(Guid id, CancellationToken cancellationToken = default)
        {
            LastDeletedId = id;
            return Task.CompletedTask;
        }
    }

    private sealed class CommandServiceFake(IReadOnlyList<CommandDefinition>? seed = null) : ICommandInteractionService
    {
        private readonly List<CommandDefinition> commands = seed?.ToList() ?? [];

        public string? LastUpdatedName { get; private set; }
        public string? LastUpdatedTopic { get; private set; }
        public string? LastUpdatedDescription { get; private set; }
        public Guid? LastDeletedId { get; private set; }

        public Task<IReadOnlyList<CommandDefinition>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CommandDefinition>>(commands);

        public Task<CommandDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
            => Task.FromResult(commands.FirstOrDefault(x => x.Id == id));

        public Task<CommandVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult(commands.SelectMany(x => x.Versions).FirstOrDefault(x => x.Id == versionId));

        public Task<bool> VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(commands.FirstOrDefault(x => x.Id == commandId)?.Versions.Any(x => x.VersionNumber == versionNumber) == true);

        public Task Create(CommandDefinition commandDefinition, CancellationToken cancellationToken = default)
        {
            commands.Add(commandDefinition);
            return Task.CompletedTask;
        }

        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            LastUpdatedName = name;
            LastUpdatedTopic = topic;
            LastUpdatedDescription = description;
            return Task.CompletedTask;
        }

        public Task AddVersion(Guid commandId, CommandVersion version, CancellationToken cancellationToken = default)
        {
            commands.First(x => x.Id == commandId).Versions.Add(version);
            return Task.CompletedTask;
        }

        public Task Delete(Guid id, CancellationToken cancellationToken = default)
        {
            LastDeletedId = id;
            return Task.CompletedTask;
        }
    }

    private sealed class RuntimeEnvironmentRepositoryFake(IReadOnlyList<RuntimeEnvironment>? seed = null) : IRuntimeEnvironmentRepository
    {
        public List<RuntimeEnvironment> Environments { get; } = seed?.ToList() ?? [];

        public Task<IReadOnlyList<RuntimeEnvironment>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RuntimeEnvironment>>(Environments);

        public Task<IReadOnlyList<RuntimeEnvironment>> GetEnabled(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RuntimeEnvironment>>(Environments.Where(x => x.IsEnabled).ToArray());

        public Task<RuntimeEnvironment?> GetById(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Environments.FirstOrDefault(x => x.Id == id));

        public Task Create(RuntimeEnvironment environment, CancellationToken cancellationToken = default)
        {
            Environments.Add(environment);
            return Task.CompletedTask;
        }

        public Task Update(RuntimeEnvironment environment, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class MetadataFieldServiceFake(IReadOnlyList<ContractFieldMetadataDefinition>? seed = null) : IContractFieldMetadataInteractionService
    {
        private readonly List<ContractFieldMetadataDefinition> metadata = seed?.ToList() ?? [];

        public Guid? LastUpdatedId { get; private set; }
        public bool? LastUpdatedIsActive { get; private set; }
        public bool ThrowOnSetVersionActive { get; set; }

        public Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ContractFieldMetadataDefinition>>(metadata);

        public Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetActive(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ContractFieldMetadataDefinition>>(metadata.Where(x => x.IsActive).ToArray());

        public Task<ContractFieldMetadataDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
            => Task.FromResult(metadata.FirstOrDefault(x => x.Id == id));

        public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(metadata.Any(x => x.Key == key && (!excludingId.HasValue || x.Id != excludingId)));

        public Task<bool> VersionExists(Guid metadataFieldId, string versionNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(metadata.FirstOrDefault(x => x.Id == metadataFieldId)?.Versions.Any(x => x.VersionNumber == versionNumber) == true);

        public Task Create(ContractFieldMetadataDefinition metadataField, CancellationToken cancellationToken = default)
        {
            metadata.Add(metadataField);
            return Task.CompletedTask;
        }

        public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            LastUpdatedId = id;
            LastUpdatedIsActive = isActive;
            return Task.CompletedTask;
        }

        public Task UpsertVersion(Guid metadataFieldId, ContractFieldMetadataVersion version, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SetVersionActive(Guid metadataFieldId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            if (ThrowOnSetVersionActive)
            {
                throw new KeyNotFoundException("missing metadata version");
            }

            var version = metadata.First(x => x.Id == metadataFieldId).Versions.First(x => x.Id == versionId);
            version.IsActive = isActive;
            version.UpdatedAtUtc = updatedAtUtc;
            return Task.CompletedTask;
        }
    }

    private sealed class SchemaTypeServiceFake(IReadOnlyList<SchemaTypeDefinition>? seed = null) : ISchemaTypeInteractionService
    {
        private readonly List<SchemaTypeDefinition> schemaTypes = seed?.ToList() ?? [];

        public bool ThrowOnSetVersionActive { get; set; }

        public Task<IReadOnlyList<SchemaTypeDefinition>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SchemaTypeDefinition>>(schemaTypes);

        public Task<IReadOnlyList<SchemaTypeVersion>> GetActiveVersions(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SchemaTypeVersion>>(schemaTypes.SelectMany(x => x.Versions).Where(x => x.IsActive).ToArray());

        public Task<SchemaTypeDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
            => Task.FromResult(schemaTypes.FirstOrDefault(x => x.Id == id));

        public Task<SchemaTypeVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult(schemaTypes.SelectMany(x => x.Versions).FirstOrDefault(x => x.Id == versionId));

        public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(schemaTypes.Any(x => x.Key == key && (!excludingId.HasValue || x.Id != excludingId)));

        public Task<bool> VersionExists(Guid typeId, string versionNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(schemaTypes.FirstOrDefault(x => x.Id == typeId)?.Versions.Any(x => x.VersionNumber == versionNumber) == true);

        public Task Create(SchemaTypeDefinition schemaType, CancellationToken cancellationToken = default)
        {
            schemaTypes.Add(schemaType);
            return Task.CompletedTask;
        }

        public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddVersion(Guid typeId, SchemaTypeVersion version, CancellationToken cancellationToken = default)
        {
            schemaTypes.First(x => x.Id == typeId).Versions.Add(version);
            return Task.CompletedTask;
        }

        public Task SetVersionActive(Guid typeId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            if (ThrowOnSetVersionActive)
            {
                throw new KeyNotFoundException("missing type version");
            }

            var version = schemaTypes.First(x => x.Id == typeId).Versions.First(x => x.Id == versionId);
            version.IsActive = isActive;
            version.UpdatedAtUtc = updatedAtUtc;
            return Task.CompletedTask;
        }
    }
}
