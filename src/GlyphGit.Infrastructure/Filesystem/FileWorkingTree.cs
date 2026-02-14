using GlyphGit.Application.Abstractions;
using GlyphGit.Infrastructure.Security;

namespace GlyphGit.Infrastructure.Filesystem;

public sealed class FileWorkingTree : IWorkingTree
{
    private readonly string _root;

    public FileWorkingTree(string root)
    {
        _root = Path.GetFullPath(root);
    }

    public Task<IReadOnlyList<string>> EnumerateFilesAsync(IReadOnlyList<string>? inputPaths = null, CancellationToken cancellationToken = default)
    {
        var results = new HashSet<string>(StringComparer.Ordinal);

        if (inputPaths is null || inputPaths.Count == 0)
        {
            ScanDirectory(_root, results);
        }
        else
        {
            foreach (var raw in inputPaths)
            {
                var relative = PathGuard.NormalizeRelativePath(raw);
                var abs = PathGuard.ToAbsoluteWithinRoot(_root, relative);

                if (File.Exists(abs))
                {
                    results.Add(ToRelative(abs));
                }
                else if (Directory.Exists(abs))
                {
                    ScanDirectory(abs, results);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(results.OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    public async Task<byte[]> ReadFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var abs = PathGuard.ToAbsoluteWithinRoot(_root, relativePath);
        return await File.ReadAllBytesAsync(abs, cancellationToken);
    }

    public async Task WriteFileAsync(string relativePath, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        var abs = PathGuard.ToAbsoluteWithinRoot(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(abs)!);
        await File.WriteAllBytesAsync(abs, content.ToArray(), cancellationToken);
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var abs = PathGuard.ToAbsoluteWithinRoot(_root, relativePath);
        if (File.Exists(abs))
        {
            File.Delete(abs);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var abs = PathGuard.ToAbsoluteWithinRoot(_root, relativePath);
        return Task.FromResult(File.Exists(abs));
    }

    public Task<DateTimeOffset?> GetLastWriteTimeUtcAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var abs = PathGuard.ToAbsoluteWithinRoot(_root, relativePath);
        if (!File.Exists(abs))
        {
            return Task.FromResult<DateTimeOffset?>(null);
        }

        return Task.FromResult<DateTimeOffset?>(File.GetLastWriteTimeUtc(abs));
    }

    public Task<long?> GetFileSizeAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var abs = PathGuard.ToAbsoluteWithinRoot(_root, relativePath);
        if (!File.Exists(abs))
        {
            return Task.FromResult<long?>(null);
        }

        return Task.FromResult<long?>(new FileInfo(abs).Length);
    }

    private void ScanDirectory(string absDir, HashSet<string> results)
    {
        foreach (var file in Directory.EnumerateFiles(absDir, "*", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}.glyphgit{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(ToRelative(file));
        }
    }

    private string ToRelative(string absoluteFile)
    {
        var rel = Path.GetRelativePath(_root, absoluteFile).Replace('\\', '/');
        return PathGuard.NormalizeRelativePath(rel);
    }
}
