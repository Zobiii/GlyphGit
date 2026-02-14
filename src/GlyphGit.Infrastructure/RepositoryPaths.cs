namespace GlyphGit.Infrastructure;

public sealed record RepositoryPaths(
    string RootPath,
    string MetaPath,
    string ObjectsPath,
    string RefsHeadsPath,
    string HeadPath,
    string IndexPath,
    string ConfigPath,
    string LogsPath,
    string LockPath)
{
    public static RepositoryPaths ForInit(string workingDirectory)
    {
        var root = Path.GetFullPath(workingDirectory);
        return FromRoot(root);
    }

    public static RepositoryPaths Discover(string workingDirectory)
    {
        var cursor = Path.GetFullPath(workingDirectory);

        while (!string.IsNullOrWhiteSpace(cursor))
        {
            var meta = Path.Combine(cursor, ".glyphgit");
            if (Directory.Exists(meta))
            {
                return FromRoot(cursor);
            }

            var parent = Directory.GetParent(cursor)?.FullName;
            if (parent is null || string.Equals(parent, cursor, StringComparison.Ordinal))
            {
                break;
            }

            cursor = parent;
        }

        throw new InvalidOperationException("No .glyphgit repository found.");
    }

    private static RepositoryPaths FromRoot(string root)
    {
        var meta = Path.Combine(root, ".glyphgit");
        return new RepositoryPaths(
            root,
            meta,
            Path.Combine(meta, "objects"),
            Path.Combine(meta, "refs", "heads"),
            Path.Combine(meta, "HEAD"),
            Path.Combine(meta, "index"),
            Path.Combine(meta, "config"),
            Path.Combine(meta, "logs"),
            Path.Combine(meta, "locks", "repo.lock"));
    }
}
