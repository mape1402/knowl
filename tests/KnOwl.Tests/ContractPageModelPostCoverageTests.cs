using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.WebUI.SchemaTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CommandNewModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Commands.NewModel;
using CommandNewVersionModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Commands.NewVersionModel;
using EventNewVersionModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Events.NewVersionModel;
using TypeEditModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Types.EditModel;
using TypeIndexModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Types.IndexModel;
using TypeNewModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Types.NewModel;
using TypeNewVersionModel = KnOwl.ControlPlane.WebUI.Pages.Contracts.Types.NewVersionModel;
using PayloadSchemaDefinition = ButterMorph.SchemaDesign.PayloadSchemaDefinition;

namespace KnOwl.Tests;

public sealed class ContractPageModelPostCoverageTests
{
    [Fact]
    public void CommandNewGetInitializesDraftContexts()
    {
        var model = new CommandNewModel(new CommandServiceFake());

        var result = model.OnGet();

        Assert.IsType<PageResult>(result);
        Assert.NotEqual(Guid.Empty, model.DraftId);
        Assert.Contains(model.DraftId.ToString(), model.ButterMorphContext);
        Assert.Contains(model.DraftId.ToString(), model.ReplyButterMorphContext);
    }

    [Fact]
    public async Task CommandNewPostCreatesDefinitionFromRequestSchema()
    {
        var commands = new CommandServiceFake();
        var model = new CommandNewModel(commands)
        {
            DraftId = Guid.Empty,
            PayloadSchemaJson = PayloadJson("register.customer", "Register Customer", "1.0.0", topicObjectValue: "commands.customer.register"),
            ReplyPayloadSchemaJson = """{"type":"object","properties":{"accepted":{"type":"boolean"}}}"""
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Contracts/Commands/View", redirect.PageName);
        var command = Assert.Single(commands.Commands);
        Assert.Equal("commands.customer.register", command.Topic);
        Assert.Equal("Register Customer", command.Name);
        var version = Assert.Single(command.Versions);
        Assert.Equal("1.0.0", version.VersionNumber);
        Assert.NotNull(version.ReplyPayloadSchemaJson);
    }

    [Theory]
    [InlineData("{", null, nameof(CommandNewModel.PayloadSchemaJson))]
    [InlineData(null, "{", nameof(CommandNewModel.ReplyPayloadSchemaJson))]
    public async Task CommandNewPostRejectsInvalidJson(string? requestJson, string? replyJson, string expectedKey)
    {
        var model = new CommandNewModel(new CommandServiceFake())
        {
            PayloadSchemaJson = requestJson ?? PayloadJson("register.customer", "Register Customer", "1.0.0"),
            ReplyPayloadSchemaJson = replyJson
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Contains(expectedKey, model.ModelState.Keys);
    }

    [Fact]
    public async Task CommandNewPostReturnsPageWhenModelStateIsInvalid()
    {
        var model = new CommandNewModel(new CommandServiceFake());
        model.ModelState.AddModelError("x", "bad");

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(string.IsNullOrWhiteSpace(model.ButterMorphContext));
    }

    [Fact]
    public async Task CommandNewPostRejectsNullPayloadDefinition()
    {
        var model = new CommandNewModel(new CommandServiceFake())
        {
            PayloadSchemaJson = "null"
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Contains(nameof(CommandNewModel.PayloadSchemaJson), model.ModelState.Keys);
    }

    [Fact]
    public async Task CommandNewPostRejectsInvalidMetadataShape()
    {
        var model = new CommandNewModel(new CommandServiceFake())
        {
            PayloadSchemaJson = """
                {
                  "key": "register.customer",
                  "name": "Register Customer",
                  "version": "1.0.0",
                  "type": "object",
                  "metadata": []
                }
                """
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Contains(nameof(CommandNewModel.PayloadSchemaJson), model.ModelState.Keys);
    }

    [Fact]
    public async Task CommandNewPostUsesStringTopicMetadata()
    {
        var commands = new CommandServiceFake();
        var model = new CommandNewModel(commands)
        {
            PayloadSchemaJson = """
                {
                  "key": "register.customer",
                  "name": "Register Customer",
                  "description": "Payload description",
                  "version": "1.0.0",
                  "versionComment": "Initial",
                  "type": "object",
                  "metadata": {
                    "topic": "commands.literal"
                  }
                }
                """
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("commands.literal", Assert.Single(commands.Commands).Topic);
    }

    [Theory]
    [InlineData("", "Register Customer", "1.0.0")]
    [InlineData("register.customer", "", "1.0.0")]
    [InlineData("register.customer", "Register Customer", "")]
    public async Task CommandNewPostRejectsMissingDefinitionFields(string key, string name, string version)
    {
        var model = new CommandNewModel(new CommandServiceFake())
        {
            PayloadSchemaJson = PayloadJson(key, name, version)
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task CommandNewPostRejectsOversizedTopicAndInvalidTopicMetadata()
    {
        var model = new CommandNewModel(new CommandServiceFake())
        {
            PayloadSchemaJson = PayloadJson(new string('x', 71), "Register Customer", "1.0.0")
        };
        var oversized = await model.OnPostAsync(CancellationToken.None);

        var invalidTopic = new CommandNewModel(new CommandServiceFake())
        {
            PayloadSchemaJson = PayloadJson("register.customer", "Register Customer", "1.0.0", topicNumber: true)
        };
        var invalid = await invalidTopic.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(oversized);
        Assert.IsType<PageResult>(invalid);
        Assert.False(model.ModelState.IsValid);
        Assert.False(invalidTopic.ModelState.IsValid);
    }

    [Fact]
    public async Task CommandNewVersionGetAndPostCoverSuccessAndValidationBranches()
    {
        var command = CreateCommand("Register Customer", "register.customer", "1.0.9");
        var commands = new CommandServiceFake([command]);
        var schemaTypes = new SchemaTypeServiceFake();
        var metadata = new MetadataFieldServiceFake();
        var model = new CommandNewVersionModel(commands, schemaTypes, metadata);

        var getResult = await model.OnGetAsync(command.Id, CancellationToken.None);
        model.Input.Version = "1.0.10";
        model.Input.Comment = " Patch ";
        model.PayloadSchemaJson = """{"type":"object","properties":{"id":{"type":"string"}}}""";
        model.ReplyPayloadSchemaJson = """{"type":"object"}""";
        var postResult = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(getResult);
        Assert.Equal("1.0.10", model.Input.Version);
        var redirect = Assert.IsType<RedirectToPageResult>(postResult);
        Assert.Equal("/Contracts/Commands/Version", redirect.PageName);
        Assert.Equal(2, command.Versions.Count);
    }

    [Fact]
    public async Task CommandNewVersionPostReturnsNotFoundForMissingCommand()
    {
        var model = new CommandNewVersionModel(new CommandServiceFake(), new SchemaTypeServiceFake(), new MetadataFieldServiceFake())
        {
            CommandId = Guid.NewGuid()
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CommandNewVersionPostReturnsPageWhenModelStateIsInvalid()
    {
        var (command, commands, schemaTypes, metadata) = CreateCommandVersionFixture();
        var model = CreateCommandVersionModel(commands, schemaTypes, metadata, command.Id, "1.0.1");
        model.ModelState.AddModelError("x", "bad");

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task CommandNewVersionPostRejectsInvalidReplyJson()
    {
        var (command, commands, schemaTypes, metadata) = CreateCommandVersionFixture();
        var model = CreateCommandVersionModel(commands, schemaTypes, metadata, command.Id, "1.0.1");
        model.ReplyPayloadSchemaJson = "{";

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Contains(nameof(CommandNewVersionModel.ReplyPayloadSchemaJson), model.ModelState.Keys);
    }

    [Fact]
    public async Task CommandNewVersionPostRejectsInvalidPayloadJson()
    {
        var (command, commands, schemaTypes, metadata) = CreateCommandVersionFixture();
        var model = CreateCommandVersionModel(commands, schemaTypes, metadata, command.Id, "1.0.1");
        model.PayloadSchemaJson = "{";

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Contains(nameof(CommandNewVersionModel.PayloadSchemaJson), model.ModelState.Keys);
    }

    [Fact]
    public async Task CommandNewVersionPostRejectsDuplicateVersion()
    {
        var (command, commands, schemaTypes, metadata) = CreateCommandVersionFixture();
        var model = CreateCommandVersionModel(commands, schemaTypes, metadata, command.Id, "1.0.0");

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task EventNewVersionGetAndPostCreatesNewVersion()
    {
        var eventDefinition = CreateEvent("Customer Created", "customer.created", "2.0.0");
        var events = new EventServiceFake([eventDefinition]);
        var schemaTypes = new SchemaTypeServiceFake();
        var metadata = new MetadataFieldServiceFake();
        var model = new EventNewVersionModel(events, schemaTypes, metadata);

        var getResult = await model.OnGetAsync(eventDefinition.Id, CancellationToken.None);
        model.Input.Version = "2.0.1";
        model.PayloadSchemaJson = """{"type":"object","properties":{"id":{"type":"string"}}}""";
        var postResult = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(getResult);
        Assert.Equal("2.0.1", model.Input.Version);
        Assert.IsType<RedirectToPageResult>(postResult);
        Assert.Equal(2, eventDefinition.Versions.Count);
    }

    [Fact]
    public async Task EventNewVersionPostReturnsNotFoundForMissingEvent()
    {
        var model = new EventNewVersionModel(new EventServiceFake(), new SchemaTypeServiceFake(), new MetadataFieldServiceFake())
        {
            EventId = Guid.NewGuid()
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EventNewVersionPostReturnsPageWhenModelStateIsInvalid()
    {
        var (eventDefinition, events, schemaTypes, metadata) = CreateEventVersionFixture();
        var model = CreateEventVersionModel(events, schemaTypes, metadata, eventDefinition.Id, "2.0.2");
        model.ModelState.AddModelError("x", "bad");

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task EventNewVersionPostRejectsInvalidPayloadJson()
    {
        var (eventDefinition, events, schemaTypes, metadata) = CreateEventVersionFixture();
        var model = CreateEventVersionModel(events, schemaTypes, metadata, eventDefinition.Id, "2.0.2");
        model.PayloadSchemaJson = "{";

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Contains(nameof(EventNewVersionModel.PayloadSchemaJson), model.ModelState.Keys);
    }

    [Fact]
    public async Task EventNewVersionPostRejectsDuplicateVersion()
    {
        var (eventDefinition, events, schemaTypes, metadata) = CreateEventVersionFixture();
        var model = CreateEventVersionModel(events, schemaTypes, metadata, eventDefinition.Id, "2.0.0");

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task TypeNewPostCreatesType()
    {
        var activeItemVersion = new SchemaTypeVersion { Id = Guid.NewGuid(), VersionNumber = "1.0.0", DefinitionJson = """{"type":"string"}""", IsActive = true };
        var schemaTypes = new SchemaTypeServiceFake(activeVersions: [activeItemVersion]);
        var model = new TypeNewModel(schemaTypes)
        {
            Input = new TypeNewModel.TypeInput { Name = "CustomerList", Description = "Customers" },
            Version = new TypeVersionInput
            {
                VersionNumber = "1.0.0",
                BaseType = "array",
                ArrayItemTypeVersionId = activeItemVersion.Id,
                MinItems = 1,
                MaxItems = 5,
                Comment = "Initial"
            }
        };

        await model.OnGetAsync(CancellationToken.None);
        var result = await model.OnPostAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Contracts/Types/View", redirect.PageName);
        Assert.Single(schemaTypes.Definitions);
    }

    [Fact]
    public async Task TypeNewPostRejectsMissingArrayItemType()
    {
        var model = new TypeNewModel(new SchemaTypeServiceFake())
        {
            Input = new TypeNewModel.TypeInput { Name = "BrokenArray" },
            Version = new TypeVersionInput
            {
                VersionNumber = "1.0.0",
                BaseType = "array",
                ArrayItemTypeVersionId = Guid.NewGuid()
            }
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task TypeNewPostRejectsDuplicateKeyAndBuilderValidationErrors()
    {
        var schemaTypes = new SchemaTypeServiceFake(existingKeys: ["Duplicate"]);
        var model = new TypeNewModel(schemaTypes)
        {
            Input = new TypeNewModel.TypeInput { Name = "Duplicate" },
            Version = new TypeVersionInput
            {
                VersionNumber = "1.0.0",
                BaseType = "string",
                MinLength = 20,
                MaxLength = 5
            }
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Empty(schemaTypes.Definitions);
    }

    [Fact]
    public async Task TypeNewVersionGetAndPostCreatesVersion()
    {
        var (schemaType, arrayItem, schemaTypes) = CreateTypeVersionFixture();
        var model = new TypeNewVersionModel(schemaTypes);

        var getResult = await model.OnGetAsync(schemaType.Id, CancellationToken.None);
        model.Version.VersionNumber = "1.0.1";
        model.Version.BaseType = "array";
        model.Version.ArrayItemTypeVersionId = arrayItem.Id;
        model.Version.MinItems = 1;
        model.Version.MaxItems = 3;
        model.Version.Comment = " Patch ";
        var postResult = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(getResult);
        Assert.Equal("1.0.1", model.Version.VersionNumber);
        var redirect = Assert.IsType<RedirectToPageResult>(postResult);
        Assert.Equal("/Contracts/Types/Version", redirect.PageName);
        Assert.Equal(2, schemaType.Versions.Count);
        Assert.Equal("Patch", schemaType.Versions.Last().Comment);
    }

    [Fact]
    public async Task TypeNewVersionPostReturnsNotFoundForMissingType()
    {
        var (_, _, schemaTypes) = CreateTypeVersionFixture();
        var model = new TypeNewVersionModel(schemaTypes) { TypeId = Guid.NewGuid() };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task TypeNewVersionGetReturnsNotFoundForSystemType()
    {
        var (_, _, schemaTypes) = CreateTypeVersionFixture();
        var systemType = new SchemaTypeDefinition { Id = Guid.NewGuid(), Key = "sys", Name = "System", IsSystem = true };
        schemaTypes.Definitions.Add(systemType);

        var result = await new TypeNewVersionModel(schemaTypes).OnGetAsync(systemType.Id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task TypeNewVersionPostRejectsDuplicateVersion()
    {
        var (schemaType, _, schemaTypes) = CreateTypeVersionFixture();

        var duplicate = new TypeNewVersionModel(schemaTypes)
        {
            TypeId = schemaType.Id,
            Version = new TypeVersionInput { VersionNumber = "1.0.0", BaseType = "string" }
        };

        var result = await duplicate.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(duplicate.ModelState.IsValid);
    }

    [Fact]
    public async Task TypeNewVersionPostRejectsMissingArrayItemType()
    {
        var (schemaType, _, schemaTypes) = CreateTypeVersionFixture();
        var model = new TypeNewVersionModel(schemaTypes)
        {
            TypeId = schemaType.Id,
            Version = new TypeVersionInput
            {
                VersionNumber = "1.0.2",
                BaseType = "array",
                ArrayItemTypeVersionId = Guid.NewGuid()
            }
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task TypeNewVersionPostRejectsInvalidStringLengthRange()
    {
        var (schemaType, _, schemaTypes) = CreateTypeVersionFixture();
        var model = new TypeNewVersionModel(schemaTypes)
        {
            TypeId = schemaType.Id,
            Version = new TypeVersionInput
            {
                VersionNumber = "1.0.2",
                BaseType = "string",
                MinLength = 10,
                MaxLength = 2
            }
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task TypeNewVersionGetHandlesNonSemanticVersionAndInvalidStoredSchema()
    {
        var schemaType = new SchemaTypeDefinition
        {
            Id = Guid.NewGuid(),
            Key = "preview",
            Name = "Preview",
            IsActive = true,
            Versions =
            {
                new SchemaTypeVersion
                {
                    VersionNumber = "preview",
                    DefinitionJson = "{",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                }
            }
        };
        var schemaTypes = new SchemaTypeServiceFake();
        schemaTypes.Definitions.Add(schemaType);
        var model = new TypeNewVersionModel(schemaTypes);

        var result = await model.OnGetAsync(schemaType.Id, CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Equal("preview.1", model.Version.VersionNumber);
        Assert.Equal("Preview", model.TypeName);
    }

    [Fact]
    public async Task TypeEditReturnsNotFoundForMissingType()
    {
        var (_, schemaTypes) = CreateEditableSchemaTypeFixture();

        var result = await new TypeEditModel(schemaTypes).OnGetAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task TypeEditLoadsExistingType()
    {
        var (schemaType, schemaTypes) = CreateEditableSchemaTypeFixture();
        var model = new TypeEditModel(schemaTypes);

        var result = await model.OnGetAsync(schemaType.Id, CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Equal(schemaType.Id, model.Id);
        Assert.Equal("Customer", model.Input.Name);
        Assert.Equal("Customer type", model.Input.Description);
        Assert.True(model.Input.IsActive);
    }

    [Fact]
    public async Task TypeEditInvalidPostReturnsPage()
    {
        var (schemaType, schemaTypes) = CreateEditableSchemaTypeFixture();
        var model = new TypeEditModel(schemaTypes) { Id = schemaType.Id };
        model.ModelState.AddModelError("Input.Name", "bad");

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task TypeEditPostReturnsNotFoundForMissingType()
    {
        var (_, schemaTypes) = CreateEditableSchemaTypeFixture();

        var result = await new TypeEditModel(schemaTypes) { Id = Guid.NewGuid() }.OnPostAsync(CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task TypeEditRejectsDuplicateKey()
    {
        var (schemaType, schemaTypes) = CreateEditableSchemaTypeFixture();
        var model = new TypeEditModel(schemaTypes)
        {
            Id = schemaType.Id,
            Input = new TypeEditModel.TypeInput { Name = "Existing", Description = "Duplicate", IsActive = true }
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task TypeEditPostUpdatesTrimmedValues()
    {
        var (schemaType, schemaTypes) = CreateEditableSchemaTypeFixture();
        var model = new TypeEditModel(schemaTypes)
        {
            Id = schemaType.Id,
            Input = new TypeEditModel.TypeInput { Name = "CustomerUpdated", Description = " Updated ", IsActive = false }
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Contracts/Types/View", redirect.PageName);
        Assert.Equal("CustomerUpdated", schemaTypes.LastUpdatedKey);
        Assert.Equal("Updated", schemaTypes.LastUpdatedDescription);
        Assert.False(schemaTypes.LastUpdatedIsActive);
    }

    [Fact]
    public async Task TypeEditRejectsSystemTypes()
    {
        var (_, schemaTypes) = CreateEditableSchemaTypeFixture();
        var systemType = new SchemaTypeDefinition { Id = Guid.NewGuid(), Key = "sys", Name = "System", IsSystem = true };
        schemaTypes.Definitions.Add(systemType);

        var result = await new TypeEditModel(schemaTypes).OnGetAsync(systemType.Id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task TypeIndexSearchFiltersByDefinitionJson()
    {
        var (_, schemaTypes) = CreateEditableSchemaTypeFixture();
        var model = new TypeIndexModel(schemaTypes) { Search = " needle " };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("needle", model.Search);
        Assert.Single(model.Types);
        Assert.Equal(schemaTypes.Definitions.Count, model.TotalTypes);
    }

    [Fact]
    public async Task TypeIndexDeactivateUpdatesSelectedType()
    {
        var (schemaType, schemaTypes) = CreateEditableSchemaTypeFixture();
        var model = new TypeIndexModel(schemaTypes);

        var result = await model.OnPostDeactivateAsync(schemaType.Id, CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(schemaType.Key, schemaTypes.LastUpdatedKey);
        Assert.False(schemaTypes.LastUpdatedIsActive);
    }

    [Fact]
    public async Task TypeIndexDeactivateIgnoresMissingType()
    {
        var (_, schemaTypes) = CreateEditableSchemaTypeFixture();
        var model = new TypeIndexModel(schemaTypes);

        var result = await model.OnPostDeactivateAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Null(schemaTypes.LastUpdatedKey);
    }

    private static (CommandDefinition Command, CommandServiceFake Commands, SchemaTypeServiceFake SchemaTypes, MetadataFieldServiceFake Metadata)
        CreateCommandVersionFixture()
    {
        var command = CreateCommand("Register Customer", "register.customer", "1.0.0");
        return (command, new CommandServiceFake([command]), new SchemaTypeServiceFake(), new MetadataFieldServiceFake());
    }

    private static (EventDefinition EventDefinition, EventServiceFake Events, SchemaTypeServiceFake SchemaTypes, MetadataFieldServiceFake Metadata)
        CreateEventVersionFixture()
    {
        var eventDefinition = CreateEvent("Customer Created", "customer.created", "2.0.0");
        return (eventDefinition, new EventServiceFake([eventDefinition]), new SchemaTypeServiceFake(), new MetadataFieldServiceFake());
    }

    private static (SchemaTypeDefinition SchemaType, SchemaTypeVersion ArrayItem, SchemaTypeServiceFake SchemaTypes) CreateTypeVersionFixture()
    {
        var arrayItem = new SchemaTypeVersion
        {
            Id = Guid.NewGuid(),
            VersionNumber = "1.0.0",
            DefinitionJson = """{"type":"string"}""",
            IsActive = true
        };
        var schemaType = new SchemaTypeDefinition
        {
            Id = Guid.NewGuid(),
            Key = "customer-list",
            Name = "CustomerList",
            Description = "Customer list",
            IsActive = true,
            IsSystem = false,
            Versions =
            {
                new SchemaTypeVersion
                {
                    Id = Guid.NewGuid(),
                    VersionNumber = "1.0.0",
                    DefinitionJson = """{"schema":{"type":"array","items":{"type":"string"}}}""",
                    Comment = "Initial",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
                }
            }
        };
        var schemaTypes = new SchemaTypeServiceFake(activeVersions: [arrayItem]);
        schemaTypes.Definitions.Add(schemaType);
        return (schemaType, arrayItem, schemaTypes);
    }

    private static (SchemaTypeDefinition SchemaType, SchemaTypeServiceFake SchemaTypes) CreateEditableSchemaTypeFixture()
    {
        var schemaType = new SchemaTypeDefinition
        {
            Id = Guid.NewGuid(),
            Key = "customer",
            Name = "Customer",
            Description = "Customer type",
            IsActive = true,
            Versions =
            {
                new SchemaTypeVersion
                {
                    VersionNumber = "1.0.0",
                    DefinitionJson = """{"description":"needle"}""",
                    Comment = "searchable",
                    IsActive = true
                }
            }
        };
        var schemaTypes = new SchemaTypeServiceFake(existingKeys: ["Existing"]);
        schemaTypes.Definitions.Add(schemaType);
        return (schemaType, schemaTypes);
    }

    private static CommandNewVersionModel CreateCommandVersionModel(
        CommandServiceFake commands,
        SchemaTypeServiceFake schemaTypes,
        MetadataFieldServiceFake metadata,
        Guid commandId,
        string version)
        => new(commands, schemaTypes, metadata)
        {
            CommandId = commandId,
            Input = new CommandNewVersionModel.VersionInput { Version = version },
            PayloadSchemaJson = """{"type":"object"}"""
        };

    private static EventNewVersionModel CreateEventVersionModel(
        EventServiceFake events,
        SchemaTypeServiceFake schemaTypes,
        MetadataFieldServiceFake metadata,
        Guid eventId,
        string version)
        => new(events, schemaTypes, metadata)
        {
            EventId = eventId,
            Input = new EventNewVersionModel.VersionInput { Version = version },
            PayloadSchemaJson = """{"type":"object"}"""
        };

    private static string PayloadJson(
        string key,
        string name,
        string version,
        string? topicObjectValue = null,
        bool topicNumber = false)
    {
        PayloadSchemaDefinition definition = new()
        {
            Key = key,
            Name = name,
            Description = "Payload description",
            Version = version,
            VersionComment = "Initial",
            Type = "object"
        };

        if (topicObjectValue is not null)
        {
            definition.Metadata = new Dictionary<string, JsonElement>
            {
                ["topic"] = JsonSerializer.Deserialize<JsonElement>($$"""{"value":{{JsonSerializer.Serialize(topicObjectValue)}}}""")
            };
        }
        else if (topicNumber)
        {
            definition.Metadata = new Dictionary<string, JsonElement>
            {
                ["topic"] = JsonSerializer.Deserialize<JsonElement>("123")
            };
        }

        return JsonSerializer.Serialize(definition, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static CommandDefinition CreateCommand(string name, string topic, string version)
    {
        var command = new CommandDefinition { Name = name, Topic = topic, Description = "Description" };
        command.Versions.Add(new CommandVersion
        {
            CommandDefinitionId = command.Id,
            VersionNumber = version,
            PayloadSchemaJson = """{"type":"object"}""",
            ReplyPayloadSchemaJson = """{"type":"object"}""",
            CreatedAtUtc = DateTime.UtcNow
        });
        return command;
    }

    private static EventDefinition CreateEvent(string name, string topic, string version)
    {
        var eventDefinition = new EventDefinition { Name = name, Topic = topic, Description = "Description" };
        eventDefinition.Versions.Add(new EventVersion
        {
            EventDefinitionId = eventDefinition.Id,
            VersionNumber = version,
            PayloadSchemaJson = """{"type":"object"}""",
            CreatedAtUtc = DateTime.UtcNow
        });
        return eventDefinition;
    }

    private sealed class CommandServiceFake(IReadOnlyList<CommandDefinition>? seed = null) : ICommandInteractionService
    {
        public List<CommandDefinition> Commands { get; } = seed?.ToList() ?? [];

        public Task<IReadOnlyList<CommandDefinition>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CommandDefinition>>(Commands);

        public Task<CommandDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
            => Task.FromResult(Commands.FirstOrDefault(x => x.Id == id));

        public Task<CommandVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult(Commands.SelectMany(x => x.Versions).FirstOrDefault(x => x.Id == versionId));

        public Task<bool> VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(Commands.First(x => x.Id == commandId).Versions.Any(x => x.VersionNumber == versionNumber));

        public Task Create(CommandDefinition commandDefinition, CancellationToken cancellationToken = default)
        {
            Commands.Add(commandDefinition);
            return Task.CompletedTask;
        }

        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddVersion(Guid commandId, CommandVersion version, CancellationToken cancellationToken = default)
        {
            var command = Commands.First(x => x.Id == commandId);
            version.CommandDefinitionId = commandId;
            command.Versions.Add(version);
            return Task.CompletedTask;
        }

        public Task Delete(Guid id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class EventServiceFake(IReadOnlyList<EventDefinition>? seed = null) : IEventInteractionService
    {
        private readonly List<EventDefinition> events = seed?.ToList() ?? [];

        public Task<IReadOnlyList<EventDefinition>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EventDefinition>>(events);

        public Task<EventDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
            => Task.FromResult(events.FirstOrDefault(x => x.Id == id));

        public Task<EventVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult(events.SelectMany(x => x.Versions).FirstOrDefault(x => x.Id == versionId));

        public Task<bool> VersionExists(Guid eventId, string versionNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(events.First(x => x.Id == eventId).Versions.Any(x => x.VersionNumber == versionNumber));

        public Task Create(EventDefinition eventDefinition, CancellationToken cancellationToken = default)
        {
            events.Add(eventDefinition);
            return Task.CompletedTask;
        }

        public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddVersion(Guid eventId, EventVersion version, CancellationToken cancellationToken = default)
        {
            var eventDefinition = events.First(x => x.Id == eventId);
            version.EventDefinitionId = eventId;
            eventDefinition.Versions.Add(version);
            return Task.CompletedTask;
        }

        public Task Delete(Guid id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class SchemaTypeServiceFake(
        IReadOnlyList<SchemaTypeVersion>? activeVersions = null,
        IReadOnlyCollection<string>? existingKeys = null) : ISchemaTypeInteractionService
    {
        private readonly HashSet<string> keys = existingKeys?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        public List<SchemaTypeDefinition> Definitions { get; } = [];
        public string? LastUpdatedKey { get; private set; }
        public string? LastUpdatedDescription { get; private set; }
        public bool LastUpdatedIsActive { get; private set; }

        public Task<IReadOnlyList<SchemaTypeDefinition>> GetAll(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SchemaTypeDefinition>>(Definitions);

        public Task<IReadOnlyList<SchemaTypeVersion>> GetActiveVersions(CancellationToken cancellationToken = default)
            => Task.FromResult(activeVersions ?? []);

        public Task<SchemaTypeDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
            => Task.FromResult(Definitions.FirstOrDefault(x => x.Id == id));

        public Task<SchemaTypeVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult((activeVersions ?? []).FirstOrDefault(x => x.Id == versionId));

        public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(keys.Contains(key) || Definitions.Any(x => x.Key == key && (!excludingId.HasValue || x.Id != excludingId)));

        public Task<bool> VersionExists(Guid typeId, string versionNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(Definitions.FirstOrDefault(x => x.Id == typeId)?.Versions.Any(x => x.VersionNumber == versionNumber) == true);

        public Task Create(SchemaTypeDefinition schemaType, CancellationToken cancellationToken = default)
        {
            Definitions.Add(schemaType);
            return Task.CompletedTask;
        }

        public Task AddVersion(Guid typeId, SchemaTypeVersion version, CancellationToken cancellationToken = default)
        {
            Definitions.First(x => x.Id == typeId).Versions.Add(version);
            return Task.CompletedTask;
        }

        public Task SetVersionActive(Guid typeId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken)
        {
            var definition = Definitions.First(x => x.Id == id);
            definition.Key = key;
            definition.Name = name;
            definition.Description = description;
            definition.IsActive = isActive;
            definition.UpdatedAtUtc = updatedAtUtc;
            LastUpdatedKey = key;
            LastUpdatedDescription = description;
            LastUpdatedIsActive = isActive;
            return Task.CompletedTask;
        }
    }

    private sealed class MetadataFieldServiceFake : IContractFieldMetadataInteractionService
    {
        private readonly List<ContractFieldMetadataDefinition> metadata =
        [
            new()
            {
                Key = "trace-id",
                Name = "Trace Id",
                IsActive = true,
                Versions =
                {
                    new ContractFieldMetadataVersion
                    {
                        VersionNumber = "1.0.0",
                        IsActive = true,
                        DefinitionJson = """{"appliesTo":["commands","events"],"dataType":"string","isRequired":false,"validation":{}}"""
                    }
                }
            }
        ];

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
            => Task.CompletedTask;

        public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpsertVersion(Guid metadataFieldId, ContractFieldMetadataVersion version, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SetVersionActive(Guid metadataFieldId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
