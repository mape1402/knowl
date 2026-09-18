using System.IO.Compression;
using System.Text;
using KnOwl.Documentation.Application;
using KnOwl.Documentation.Storage.EntityFramework.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlaneHost.Sample.Documentation;

/// <summary>
/// Prepares demo documentation content for the sample Control Plane host.
/// </summary>
internal static class SampleDocumentationSeeder
{
    public static async Task Initialize(WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<KnOwlDocumentationDbContext>();
        await db.Database.MigrateAsync();

        var documentation = scope.ServiceProvider.GetRequiredService<IDocumentationInteractionService>();
        var space = await documentation.UpsertSpace("orchestrator", "Orchestrator", "Sample documentation space for the control plane.", true);
        var topic = await documentation.UpsertTopic(space.Key, "getting-started", "Getting Started", "Introductory pages for KnOwl documentation.", true);
        var page = await documentation.UpsertPage(space.Key, topic.Key, "control-plane", "Control Plane Guide", "Sample Markdown page with linked assets.", true);

        if (await documentation.Render(space.Key, topic.Key, page.Key, "1.0.0") is not null)
        {
            return;
        }

        await using var package = CreateDocumentationPackage();
        var version = await documentation.ImportVersion(
            new DocumentationVersionInput(
                page.Id,
                "1.0.0",
                "knowl-control-plane-docs.zip",
                "application/zip",
                package,
                "docs/index.md"));

        await documentation.PublishVersion(version.Id);
    }

    private static MemoryStream CreateDocumentationPackage()
    {
        MemoryStream output = new();
        using (ZipArchive zip = new(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteText(
                zip,
                "docs/index.md",
                """
                # Control Plane Guide

                This sample page is loaded from a Markdown package when the Control Plane sample starts.

                ![KnOwl documentation flow](assets/flow.svg)

                ## What you can test

                - Browse this page as HTML.
                - Download the original Markdown.
                - Download the original ZIP package.
                - Download the generated PDF.

                [Open the KnOwl repository](https://github.com/mape1402/knowl)
                """);

            WriteText(
                zip,
                "docs/assets/flow.svg",
                """
                <svg xmlns="http://www.w3.org/2000/svg" width="760" height="220" viewBox="0 0 760 220" role="img" aria-label="KnOwl documentation flow">
                  <rect width="760" height="220" rx="12" fill="#f8fafc"/>
                  <rect x="42" y="72" width="150" height="76" rx="8" fill="#0f766e"/>
                  <rect x="304" y="72" width="150" height="76" rx="8" fill="#2563eb"/>
                  <rect x="566" y="72" width="150" height="76" rx="8" fill="#7c3aed"/>
                  <path d="M206 110h84" stroke="#334155" stroke-width="4" stroke-linecap="round"/>
                  <path d="M468 110h84" stroke="#334155" stroke-width="4" stroke-linecap="round"/>
                  <path d="M280 100l14 10-14 10" fill="none" stroke="#334155" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>
                  <path d="M542 100l14 10-14 10" fill="none" stroke="#334155" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>
                  <text x="117" y="104" text-anchor="middle" fill="#fff" font-family="Arial, sans-serif" font-size="18" font-weight="700">Space</text>
                  <text x="117" y="128" text-anchor="middle" fill="#ccfbf1" font-family="Arial, sans-serif" font-size="13">Orchestrator</text>
                  <text x="379" y="104" text-anchor="middle" fill="#fff" font-family="Arial, sans-serif" font-size="18" font-weight="700">Topic</text>
                  <text x="379" y="128" text-anchor="middle" fill="#dbeafe" font-family="Arial, sans-serif" font-size="13">Getting Started</text>
                  <text x="641" y="104" text-anchor="middle" fill="#fff" font-family="Arial, sans-serif" font-size="18" font-weight="700">Page</text>
                  <text x="641" y="128" text-anchor="middle" fill="#ede9fe" font-family="Arial, sans-serif" font-size="13">Version 1.0.0</text>
                </svg>
                """);
        }

        output.Position = 0;
        return output;
    }

    private static void WriteText(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }
}
