namespace KnOwl.Documentation.Application;

public interface IDocumentationPdfRenderer
{
    Task<Stream> RenderPdf(string title, string html, CancellationToken cancellationToken = default);
}
