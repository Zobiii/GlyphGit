using System.Text;
using GlyphGit.Application.Abstractions;
using GlyphGit.Infrastructure.Filesystem;

namespace GlyphGit.Infrastructure.Storage;

public sealed class FileRefStore : IRefStore
{
    private readonly RepositoryPaths _paths;

    public FileRefStore(RepositoryPaths paths)
    {
        _paths = paths;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_paths.MetaPath);
        Directory.CreateDirectory(_paths.ObjectsPath);
        Directory.CreateDirectory(_paths.RefsHeadsPath);
        Directory.CreateDirectory(Path.GetDirectoryName(_paths.LockPath)!);
        Directory.CreateDirectory(_paths.LogsPath);

        await FileHelpers.AtomicWriteAsync(_paths.HeadPath, Encoding.UTF8.GetBytes("ref: refs/heads/main\n"), cancellationToken);
        if (!File.Exists(Path.Combine(_paths.RefsHeadsPath, "main")))
        {
            await FileHelpers.AtomicWriteAsync(Path.Combine(_paths.RefsHeadsPath, "main"), Encoding.UTF8.GetBytes(string.Empty), cancellationToken);
        }

        if (!File.Exists(_paths.ConfigPath))
        {
            await FileHelpers.AtomicWriteAsync(
                _paths.ConfigPath,
                Encoding.UTF8.GetBytes("[glyphgit]\nversion=2\n"),
                cancellationToken);
        }
    }

    public async Task<string> ReadHeadReferenceAsync(CancellationToken cancellationToken = default)
    {
        var head = (await FileHelpers.ReadTextIfExistsAsync(_paths.HeadPath, cancellationToken) ?? string.Empty).Trim();
        if (head.StartsWith("ref: ", StringComparison.Ordinal))
        {
            return head["ref: ".Length..].Trim();
        }

        return "refs/heads/main";
    }

    public async Task<string?> ReadHeadCommitAsync(CancellationToken cancellationToken = default)
    {
        var head = (await FileHelpers.ReadTextIfExistsAsync(_paths.HeadPath, cancellationToken) ?? string.Empty).Trim();
        if (head.StartsWith("ref: ", StringComparison.Ordinal))
        {
            var refName = head["ref: ".Length..].Trim();
            var refPath = Path.Combine(_paths.MetaPath, refName.Replace('/', Path.DirectorySeparatorChar));
            var refValue = (await FileHelpers.ReadTextIfExistsAsync(refPath, cancellationToken) ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(refValue) ? null : refValue;
        }

        return string.IsNullOrWhiteSpace(head) ? null : head;
    }

    public async Task WriteHeadCommitAsync(string commitHash, CancellationToken cancellationToken = default)
    {
        var headRef = await ReadHeadReferenceAsync(cancellationToken);
        var refPath = Path.Combine(_paths.MetaPath, headRef.Replace('/', Path.DirectorySeparatorChar));
        await FileHelpers.AtomicWriteAsync(refPath, Encoding.UTF8.GetBytes($"{commitHash}\n"), cancellationToken);
    }
}
