using System.Text.Json;
using global::ButterMorph.SchemaDesign;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.WebUI.ButterMorph;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Commands;

public class NewModel(ICommandInteractionService commands) : PageModel
{
    [BindProperty]
    public string PayloadSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{}}";

    [BindProperty]
    public string? ReplyPayloadSchemaJson { get; set; }

    [BindProperty]
    public Guid DraftId { get; set; }

    public string ButterMorphContext { get; private set; } = string.Empty;
    public string ReplyButterMorphContext { get; private set; } = string.Empty;

    public IActionResult OnGet()
    {
        DraftId = Guid.NewGuid();
        SetButterMorphContexts();
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
            return Page();
        }

        if (!TryParseJson(PayloadSchemaJson, nameof(PayloadSchemaJson), "The request schema is not valid JSON."))
        {
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(ReplyPayloadSchemaJson) &&
            !TryParseJson(ReplyPayloadSchemaJson, nameof(ReplyPayloadSchemaJson), "The reply schema is not valid JSON."))
        {
            return Page();
        }

        if (!TryReadRequestDefinition(out var definition))
        {
            return Page();
        }

        if (!TryReadDefinitionKey(definition, out var key))
        {
            ModelState.AddModelError(nameof(PayloadSchemaJson), "Command schema key is required in the request schema.");
            return Page();
        }

        if (key.Length > 100)
        {
            ModelState.AddModelError(nameof(PayloadSchemaJson), "Command schema key must be 100 characters or fewer.");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            ModelState.AddModelError(nameof(PayloadSchemaJson), "Command name is required in the request schema.");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(definition.Version))
        {
            ModelState.AddModelError(nameof(PayloadSchemaJson), "Command version is required in the request schema.");
            return Page();
        }

        var version = definition.Version.Trim();
        var now = DateTime.UtcNow;
        CommandDefinition commandDefinition = new()
        {
            Name = definition.Name.Trim(),
            Key = key,
            Description = string.IsNullOrWhiteSpace(definition.Description) ? null : definition.Description.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        commandDefinition.Versions.Add(new CommandVersion
        {
            VersionNumber = version,
            PayloadSchemaJson = PayloadSchemaJson,
            ReplyPayloadSchemaJson = string.IsNullOrWhiteSpace(ReplyPayloadSchemaJson) ? null : ReplyPayloadSchemaJson,
            Comment = string.IsNullOrWhiteSpace(definition.VersionComment) ? null : definition.VersionComment.Trim(),
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

    private bool TryReadRequestDefinition(out PayloadSchemaDefinition definition)
    {
        definition = new PayloadSchemaDefinition();
        try
        {
            var parsed = JsonSerializer.Deserialize<PayloadSchemaDefinition>(
                PayloadSchemaJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (parsed is null)
            {
                ModelState.AddModelError(nameof(PayloadSchemaJson), "The request schema is not valid.");
                return false;
            }

            definition = parsed;
            return true;
        }
        catch (JsonException)
        {
            ModelState.AddModelError(nameof(PayloadSchemaJson), "The request schema is not valid.");
            return false;
        }
    }

    private static bool TryReadDefinitionKey(PayloadSchemaDefinition definition, out string key)
    {
        key = definition.Key?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(key);
    }
}
