using KnOwl.ControlPlane.WebUI.ButterMorph;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts;

public class ButterMorphDraftsModel(KnOwlButterMorphDraftStore draftStore) : PageModel
{
    public IActionResult OnGetPayloadSchema(string context)
    {
        var schema = draftStore.TakePayloadSchema(context);
        return new JsonResult(new
        {
            payloadSchemaJson = schema
        });
    }

    public IActionResult OnGetCreatedEvent(string context)
    {
        var eventId = draftStore.TakeCreatedEvent(context);
        return new JsonResult(new
        {
            eventId,
            redirectUrl = eventId.HasValue ? Url.Page("/Contracts/Events/View", new { id = eventId.Value }) : string.Empty
        });
    }

    public IActionResult OnGetCreatedCommand(string context)
    {
        var commandId = draftStore.TakeCreatedCommand(context);
        return new JsonResult(new
        {
            commandId,
            redirectUrl = commandId.HasValue ? Url.Page("/Contracts/Commands/View", new { id = commandId.Value }) : string.Empty
        });
    }
}
