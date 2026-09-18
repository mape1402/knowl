using KnOwl.Documentation.Application;
using KnOwl.Security.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace KnOwl.Documentation.Api;

public static class KnOwlDocumentationEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapKnOwlDocumentationApi(this IEndpointRouteBuilder endpoints, KnOwlApiAuthorizationOptions? authorization = null)
    {
        authorization ??= new KnOwlApiAuthorizationOptions();
        var api = endpoints.MapGroup("/api/v1/documentation").WithTags("KnOwl Documentation API");

        api.MapGet("/spaces", async ([FromServices] IDocumentationInteractionService docs, CancellationToken cancellationToken)
            => Results.Ok((await docs.GetSpaces(cancellationToken)).Select(x => x.ToResponse())))
            .RequireKnOwlPolicy(authorization.DocumentationReadPolicy);

        api.MapPost("/spaces", async ([FromBody] UpsertDocumentationSpaceRequest request, [FromServices] IDocumentationInteractionService docs, CancellationToken cancellationToken)
            => Results.Ok((await docs.UpsertSpace(request.Key, request.Name, request.Description, request.IsActive, cancellationToken)).ToResponse()))
            .RequireKnOwlPolicy(authorization.DocumentationWritePolicy);

        api.MapGet("/spaces/{spaceKey}/topics", async (string spaceKey, [FromServices] IDocumentationInteractionService docs, CancellationToken cancellationToken)
            => Results.Ok((await docs.GetTopics(spaceKey, cancellationToken)).Select(x => x.ToResponse())))
            .RequireKnOwlPolicy(authorization.DocumentationReadPolicy);

        api.MapPost("/spaces/{spaceKey}/topics", async (string spaceKey, [FromBody] UpsertDocumentationTopicRequest request, [FromServices] IDocumentationInteractionService docs, CancellationToken cancellationToken)
            => Results.Ok((await docs.UpsertTopic(spaceKey, request.Key, request.Name, request.Description, request.IsActive, cancellationToken)).ToResponse()))
            .RequireKnOwlPolicy(authorization.DocumentationWritePolicy);

        api.MapGet("/spaces/{spaceKey}/topics/{topicKey}/pages", async (string spaceKey, string topicKey, [FromServices] IDocumentationInteractionService docs, CancellationToken cancellationToken)
            => Results.Ok((await docs.GetPages(spaceKey, topicKey, cancellationToken)).Select(x => x.ToResponse())))
            .RequireKnOwlPolicy(authorization.DocumentationReadPolicy);

        api.MapPost("/spaces/{spaceKey}/topics/{topicKey}/pages", async (string spaceKey, string topicKey, [FromBody] UpsertDocumentationPageRequest request, [FromServices] IDocumentationInteractionService docs, CancellationToken cancellationToken)
            => Results.Ok((await docs.UpsertPage(spaceKey, topicKey, request.Key, request.Title, request.Description, request.IsActive, cancellationToken)).ToResponse()))
            .RequireKnOwlPolicy(authorization.DocumentationWritePolicy);

        api.MapPost("/pages/{pageId:guid}/versions", async (Guid pageId, HttpRequest request, [FromServices] IDocumentationInteractionService docs, CancellationToken cancellationToken) =>
        {
            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files["file"];
            if (file is null)
            {
                return Results.BadRequest("A multipart file field named 'file' is required.");
            }

            var version = form["versionNumber"].ToString();
            var entryPath = form["entryPath"].ToString();
            await using var stream = file.OpenReadStream();
            var imported = await docs.ImportVersion(new DocumentationVersionInput(pageId, version, file.FileName, file.ContentType, stream, string.IsNullOrWhiteSpace(entryPath) ? null : entryPath), cancellationToken);
            return Results.Created($"/api/v1/documentation/page-versions/{imported.Id}", imported.ToResponse());
        }).DisableAntiforgery().RequireKnOwlPolicy(authorization.DocumentationWritePolicy);

        endpoints.MapKnOwlDocumentationBrowseEndpoints(authorization);
        return endpoints;
    }

    public static IEndpointRouteBuilder MapKnOwlDocumentationBrowseEndpoints(this IEndpointRouteBuilder endpoints, KnOwlApiAuthorizationOptions? authorization = null)
    {
        authorization ??= new KnOwlApiAuthorizationOptions();

        endpoints.MapGet("/docs/{spaceKey}/{topicKey}/{pageKey}", async (string spaceKey, string topicKey, string pageKey, [FromServices] IDocumentationInteractionService docs, CancellationToken cancellationToken)
            => await RenderPage(spaceKey, topicKey, pageKey, "latest", docs, cancellationToken))
            .RequireKnOwlPolicy(authorization.DocumentationReadPolicy);

        endpoints.MapGet("/docs/{spaceKey}/{topicKey}/{pageKey}/{version}", async (string spaceKey, string topicKey, string pageKey, string version, [FromServices] IDocumentationInteractionService docs, CancellationToken cancellationToken)
            => await RenderPage(spaceKey, topicKey, pageKey, version, docs, cancellationToken))
            .RequireKnOwlPolicy(authorization.DocumentationReadPolicy);

        endpoints.MapGet("/docs/{spaceKey}/{topicKey}/{pageKey}/{version}/source.md", async (string spaceKey, string topicKey, string pageKey, string version, [FromServices] IDocumentationInteractionService docs, CancellationToken cancellationToken) =>
        {
            var rendered = await docs.Render(spaceKey, topicKey, pageKey, version, cancellationToken);
            return rendered is null ? Results.NotFound() : Results.File(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(rendered.Markdown)), "text/markdown", $"{pageKey}-{rendered.Version.VersionNumber}.md");
        }).RequireKnOwlPolicy(authorization.DocumentationDownloadPolicy);

        endpoints.MapGet("/docs/{spaceKey}/{topicKey}/{pageKey}/{version}/package.zip", async (string spaceKey, string topicKey, string pageKey, string version, [FromServices] IDocumentationInteractionService docs, CancellationToken cancellationToken) =>
        {
            var rendered = await docs.Render(spaceKey, topicKey, pageKey, version, cancellationToken);
            if (rendered is null) return Results.NotFound();
            var package = await docs.BuildSourcePackage(rendered.Version.Id, cancellationToken);
            return Results.File(package.Content, "application/zip", package.FileName);
        }).RequireKnOwlPolicy(authorization.DocumentationDownloadPolicy);

        endpoints.MapGet("/docs/assets/{assetId:guid}", async (Guid assetId, [FromServices] IDocumentationInteractionService docs, CancellationToken cancellationToken) =>
        {
            var asset = await docs.GetAsset(assetId, cancellationToken);
            if (asset is null) return Results.NotFound();
            var content = await docs.OpenAsset(asset, cancellationToken);
            return Results.File(content, asset.ContentType, asset.IsDownloadable ? asset.FileName : null);
        }).RequireKnOwlPolicy(authorization.DocumentationReadPolicy);

        return endpoints;
    }

    private static async Task<IResult> RenderPage(string spaceKey, string topicKey, string pageKey, string version, IDocumentationInteractionService docs, CancellationToken cancellationToken)
    {
        var rendered = await docs.Render(spaceKey, topicKey, pageKey, version, cancellationToken);
        if (rendered is null) return Results.NotFound();
        var html = $$"""
        <!doctype html>
        <html>
        <head><meta charset="utf-8"><title>{{System.Net.WebUtility.HtmlEncode(pageKey)}}</title><style>body{font-family:system-ui;margin:2rem;max-width:920px}img{max-width:100%}.missing-doc-asset{color:#b00020}</style></head>
        <body>{{rendered.Html}}</body>
        </html>
        """;
        return Results.Content(html, "text/html");
    }

    private static RouteHandlerBuilder RequireKnOwlPolicy(this RouteHandlerBuilder builder, string? policy)
        => string.IsNullOrWhiteSpace(policy) ? builder : builder.RequireAuthorization(policy);
}
