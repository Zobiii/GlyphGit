namespace GlyphGit.Infrastructure.Security;

public static class PathGuard
{
    public static string NormalizeRelativePath(string input)
    {
        var p = input.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(p))
        {
            throw new ArgumentException("Path must not be empty.");
        }

        while (p.StartsWith("./", StringComparison.Ordinal))
        {
            p = p[2..];
        }

        if (p.StartsWith("/", StringComparison.Ordinal) ||
            p.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Illegal path: {input}");
        }

        return p.Trim('/');
    }

    public static string ToAbsoluteWithinRoot(string root, string relative)
    {
        var normalized = NormalizeRelativePath(relative);
        var absolute = Path.GetFullPath(Path.Combine(root, normalized));
        var fullRoot = Path.GetFullPath(root);

        if (!absolute.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Path escapes repository root: {relative}");
        }

        return absolute;
    }
}
