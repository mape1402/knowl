using System.Text;
using System.Text.RegularExpressions;

namespace KnOwl.Documentation.Application;

/// <summary>
/// Minimal PDF renderer used as an extendable default implementation.
/// </summary>
public sealed class SimpleDocumentationPdfRenderer : IDocumentationPdfRenderer
{
    public Task<Stream> RenderPdf(string title, string html, CancellationToken cancellationToken = default)
    {
        var text = Regex.Replace(html, "<[^>]+>", " ");
        text = Regex.Replace(System.Net.WebUtility.HtmlDecode(text), "\\s+", " ").Trim();
        var lines = Wrap($"{title}\n\n{text}", 88).Take(45).ToArray();
        var content = string.Join("\\n", lines.Select(EscapePdfLine));
        var streamText = $"BT /F1 11 Tf 50 780 Td 14 TL ({content}) Tj ET";
        var pdf = BuildPdf(streamText);
        return Task.FromResult<Stream>(new MemoryStream(pdf, writable: false));
    }

    private static IEnumerable<string> Wrap(string text, int width)
    {
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            while (line.Length > width)
            {
                var cut = line.LastIndexOf(' ', width);
                if (cut <= 0)
                {
                    cut = width;
                }

                yield return line[..cut];
                line = line[cut..].Trim();
            }

            yield return line;
        }
    }

    private static string EscapePdfLine(string line)
        => line.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("(", "\\(", StringComparison.Ordinal).Replace(")", "\\)", StringComparison.Ordinal).Replace("\r", string.Empty, StringComparison.Ordinal);

    private static byte[] BuildPdf(string pageContent)
    {
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(pageContent)} >>\nstream\n{pageContent}\nendstream"
        };
        using MemoryStream ms = new();
        using StreamWriter writer = new(ms, Encoding.ASCII, leaveOpen: true);
        writer.Write("%PDF-1.4\n");
        List<long> offsets = [0];
        for (var i = 0; i < objects.Length; i++)
        {
            writer.Flush();
            offsets.Add(ms.Position);
            writer.Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        writer.Flush();
        var xref = ms.Position;
        writer.Write($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            writer.Write($"{offset:0000000000} 00000 n \n");
        }

        writer.Write($"trailer << /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        writer.Flush();
        return ms.ToArray();
    }
}
