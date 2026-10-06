using KnOwl.ControlPlane.WebUI.ButterMorph;
using KnOwl.ControlPlane.WebUI.Pages.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace KnOwl.Tests;

public sealed class ButterMorphDraftsPageModelTests
{
    [Fact]
    public void DraftEndpointsReturnStoredPayloadsAndRedirects()
    {
        var store = new KnOwlButterMorphDraftStore();
        var eventId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        store.SavePayloadSchema("ctx", """{"type":"object"}""");
        store.SaveCreatedEvent("ctx", eventId);
        store.SaveCreatedCommand("ctx", commandId);
        var model = new ButterMorphDraftsModel(store)
        {
            Url = new FakeUrlHelper()
        };

        var payload = Assert.IsType<JsonResult>(model.OnGetPayloadSchema("ctx"));
        var missingPayload = Assert.IsType<JsonResult>(model.OnGetPayloadSchema("missing"));
        var createdEvent = Assert.IsType<JsonResult>(model.OnGetCreatedEvent("ctx"));
        var missingEvent = Assert.IsType<JsonResult>(model.OnGetCreatedEvent("missing"));
        var createdCommand = Assert.IsType<JsonResult>(model.OnGetCreatedCommand("ctx"));
        var missingCommand = Assert.IsType<JsonResult>(model.OnGetCreatedCommand("missing"));

        Assert.Equal("""{"type":"object"}""", Read<string>(payload.Value, "payloadSchemaJson"));
        Assert.Equal(string.Empty, Read<string>(missingPayload.Value, "payloadSchemaJson"));
        Assert.Equal(eventId, Read<Guid?>(createdEvent.Value, "eventId"));
        Assert.Equal("/Contracts/Events/View?id=" + eventId, Read<string>(createdEvent.Value, "redirectUrl"));
        Assert.Null(Read<Guid?>(missingEvent.Value, "eventId"));
        Assert.Equal(string.Empty, Read<string>(missingEvent.Value, "redirectUrl"));
        Assert.Equal(commandId, Read<Guid?>(createdCommand.Value, "commandId"));
        Assert.Equal("/Contracts/Commands/View?id=" + commandId, Read<string>(createdCommand.Value, "redirectUrl"));
        Assert.Null(Read<Guid?>(missingCommand.Value, "commandId"));
        Assert.Equal(string.Empty, Read<string>(missingCommand.Value, "redirectUrl"));
    }

    private static T? Read<T>(object? value, string property)
        => (T?)value?.GetType().GetProperty(property)?.GetValue(value);

    private sealed class FakeUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new()
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData(),
            ActionDescriptor = new()
        };

        public string? Action(UrlActionContext actionContext) => "/";

        public string? Content(string? contentPath) => contentPath;

        public bool IsLocalUrl(string? url) => true;

        public string? Link(string? routeName, object? values) => "/";

        public string? RouteUrl(UrlRouteContext routeContext)
        {
            var values = new RouteValueDictionary(routeContext.Values);
            values.TryGetValue("page", out var page);
            values.TryGetValue("id", out var id);
            return $"{page}?id={id}";
        }
    }
}
