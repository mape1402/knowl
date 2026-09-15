using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.WebUI.ButterMorph;
using KnOwl.ControlPlane.WebUI.ContractMetadata;
using KnOwl.ControlPlane.WebUI.SchemaTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Commands;

public class NewModel(
    ICommandInteractionService commands,
    ISchemaTypeInteractionService schemaTypes,
    IContractFieldMetadataInteractionService metadataFields) : PageModel
{
    [BindProperty]
    public CommandInput Input { get; set; } = new();

    [BindProperty]
    [Required]
    public string PayloadSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{}}";

    [BindProperty]
    public string? ReplyPayloadSchemaJson { get; set; }

    [BindProperty]
    public Guid DraftId { get; set; }

    public string SchemaTypesJson { get; private set; } = "[]";
    public string MetadataFieldsJson { get; private set; } = "[]";
    public string ButterMorphContext { get; private set; } = string.Empty;
    public string ReplyButterMorphContext { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        DraftId = Guid.NewGuid();
        Input.Version = "1.0.0";
        SetButterMorphContexts();
        await LoadCatalogs(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (DraftId == Guid.Empty)
        {
            DraftId = Guid.NewGuid();
        }

        SetButterMorphContexts();
        if (!ModelState.IsValid)
        {
            await LoadCatalogs(cancellationToken);
            return Page();
        }

        if (!TryParseJson(PayloadSchemaJson, nameof(PayloadSchemaJson), "The request schema is not valid JSON."))
        {
            await LoadCatalogs(cancellationToken);
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(ReplyPayloadSchemaJson) &&
            !TryParseJson(ReplyPayloadSchemaJson, nameof(ReplyPayloadSchemaJson), "The reply schema is not valid JSON."))
        {
            await LoadCatalogs(cancellationToken);
            return Page();
        }

        var topic = string.IsNullOrWhiteSpace(Input.Topic)
            ? ReadTopicFromPayloadSchema(PayloadSchemaJson)
            : Input.Topic.Trim();

        if (string.IsNullOrWhiteSpace(topic))
        {
            ModelState.AddModelError(nameof(Input.Topic), "Topic is required when the request schema does not define a key or topic metadata.");
            await LoadCatalogs(cancellationToken);
            return Page();
        }

        if (topic.Length > 70)
        {
            ModelState.AddModelError(nameof(Input.Topic), "Topic must be 70 characters or fewer.");
            await LoadCatalogs(cancellationToken);
            return Page();
        }

        var version = Input.Version.Trim();
        var now = DateTime.UtcNow;
        CommandDefinition commandDefinition = new()
        {
            Name = Input.Name.Trim(),
            Topic = topic,
            Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        commandDefinition.Versions.Add(new CommandVersion
        {
            VersionNumber = version,
            PayloadSchemaJson = PayloadSchemaJson,
            ReplyPayloadSchemaJson = string.IsNullOrWhiteSpace(ReplyPayloadSchemaJson) ? null : ReplyPayloadSchemaJson,
            Comment = string.IsNullOrWhiteSpace(Input.Comment) ? null : Input.Comment.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        await commands.Create(commandDefinition, cancellationToken);
        return RedirectToPage("/Contracts/Commands/View", new { id = commandDefinition.Id, version });
    }

    private void SetButterMorphContexts()
    {
        ButterMorphContext = KnOwlButterMorphContext.CommandCreateRequestDraft(DraftId);
        ReplyButterMorphContext = KnOwlButterMorphContext.CommandCreateReplyDraft(DraftId);
    }

    private async Task LoadCatalogs(CancellationToken cancellationToken)
    {
        SchemaTypesJson = await SchemaTypeCatalog.GetSelectableTypesJsonAsync(schemaTypes);
        MetadataFieldsJson = await ContractFieldMetadataCatalog.GetApplicableMetadataJsonAsync(metadataFields, "commands");
    }

    private bool TryParseJson(string? json, string key, string message)
    {
        try
        {
            JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            return true;
        }
        catch (JsonException)
        {
            ModelState.AddModelError(key, message);
            return false;
        }
    }

    private static string ReadTopicFromPayloadSchema(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            var root = document.RootElement;
            if (root.TryGetProperty("metadata", out var metadata) &&
                metadata.ValueKind == JsonValueKind.Object &&
                metadata.TryGetProperty("topic", out var topicElement) &&
                TryReadMetadataValue(topicElement, out var topic))
            {
                return topic;
            }

            if (root.TryGetProperty("key", out var keyElement) && keyElement.ValueKind == JsonValueKind.String)
            {
                return keyElement.GetString()?.Trim() ?? string.Empty;
            }
        }
        catch (JsonException)
        {
        }

        return string.Empty;
    }

    private static bool TryReadMetadataValue(JsonElement element, out string value)
    {
        value = string.Empty;
        if (element.ValueKind == JsonValueKind.String)
        {
            value = element.GetString()?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("value", out var valueElement) &&
            valueElement.ValueKind == JsonValueKind.String)
        {
            value = valueElement.GetString()?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }

    public class CommandInput
    {
        [Required]
        [MaxLength(200)]
        [Display(Name = "Command Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(70)]
        [Display(Name = "Topic")]
        public string? Topic { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [MaxLength(13)]
        [Display(Name = "Version Number")]
        public string Version { get; set; } = string.Empty;

        [MaxLength(1000)]
        [Display(Name = "Version Comment")]
        public string? Comment { get; set; }
    }
}
