namespace KnOwl.Documentation.Application;

/// <summary>
/// Normalizes and validates documentation package paths.
/// </summary>
public static class DocumentationPath
{
    public static string Normalize(string path)
    {
        var value = (path ?? string.Empty).Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(value) ||
            value.StartsWith("../", StringComparison.Ordinal) ||
            value.Contains("/../", StringComparison.Ordinal) ||
            value == ".." ||
            Path.IsPathRooted(value))
        {
            throw new InvalidOperationException($"Documentation path '{path}' is not allowed.");
        }

        return value;
    }

    public static string CombineRelative(string currentPath, string relativePath)
    {
        var normalizedCurrent = Normalize(currentPath);
        var normalizedRelative = (relativePath ?? string.Empty).Replace('\\', '/');
        if (Path.IsPathRooted(normalizedRelative) || normalizedRelative.StartsWith("/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Documentation path '{relativePath}' is not allowed.");
        }

        var baseSegments = normalizedCurrent.Split('/', StringSplitOptions.RemoveEmptyEntries).SkipLast(1);
        List<string> segments = [.. baseSegments];
        foreach (var segment in normalizedRelative.Split("/", StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (segments.Count == 0)
                {
                    throw new InvalidOperationException($"Documentation path '{relativePath}' is not allowed.");
                }

                segments.RemoveAt(segments.Count - 1);
                continue;
            }

            segments.Add(segment);
        }

        return Normalize(string.Join('/', segments));
    }
}
