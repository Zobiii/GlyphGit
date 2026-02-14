using System.Text;
using System.Text.RegularExpressions;
using GlyphGit.Application.Abstractions;
using GlyphGit.Infrastructure.Filesystem;

namespace GlyphGit.Infrastructure.Storage;

public sealed class FileRefStore : IRefStore
{
    private static readonly Regex RefNameRegex =
        new("^[A-Za-z0-9._/-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
        Directory.CreateDirectory(Path.Combine(_paths.MetaPath, "refs", "tags"));
        Directory.CreateDirectory(Path.GetDirectoryName(_paths.LockPath)!);
        Directory.CreateDirectory(_paths.LogsPath);

        await FileHelpers.AtomicWriteAsync(_paths.HeadPath, Encoding.UTF8.GetBytes("ref: refs/heads/main\n"), cancellationToken);

        var mainRef = Path.Combine(_paths.RefsHeadsPath, "main");
        if (!File.Exists(mainRef))
        {
            await FileHelpers.AtomicWriteAsync(mainRef, Encoding.UTF8.GetBytes(string.Empty), cancellationToken);
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
        var headRef = await ReadHeadReferenceAsync(cancellationToken);
        var refPath = ToRefPath(headRef);
        var content = (await FileHelpers.ReadTextIfExistsAsync(refPath, cancellationToken) ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(content) ? null : content;
    }

    public async Task WriteHeadCommitAsync(string commitHash, CancellationToken cancellationToken = default)
    {
        var headRef = await ReadHeadReferenceAsync(cancellationToken);
        var refPath = ToRefPath(headRef);
        await FileHelpers.AtomicWriteAsync(refPath, Encoding.UTF8.GetBytes($"{commitHash}\n"), cancellationToken);
    }

    public async Task SetHeadReferenceAsync(string branchName, CancellationToken cancellationToken = default)
    {
        ValidateRefName(branchName);
        await FileHelpers.AtomicWriteAsync(
            _paths.HeadPath,
            Encoding.UTF8.GetBytes($"ref: refs/heads/{branchName}\n"),
            cancellationToken);
    }

    public Task<IReadOnlyList<string>> ListBranchesAsync(CancellationToken cancellationToken = default)
        => ListRefNamesAsync(Path.Combine(_paths.MetaPath, "refs", "heads"));

    public Task<bool> BranchExistsAsync(string branchName, CancellationToken cancellationToken = default)
        => RefExistsAsync($"refs/heads/{branchName}");

    public Task<string?> ReadBranchCommitAsync(string branchName, CancellationToken cancellationToken = default)
        => ReadRefCommitAsync($"refs/heads/{branchName}", cancellationToken);

    public async Task CreateBranchAsync(string branchName, string? commitHash, CancellationToken cancellationToken = default)
    {
        ValidateRefName(branchName);
        await CreateRefAsync($"refs/heads/{branchName}", commitHash ?? string.Empty, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ListTagsAsync(CancellationToken cancellationToken = default)
        => ListRefNamesAsync(Path.Combine(_paths.MetaPath, "refs", "tags"));

    public Task<bool> TagExistsAsync(string tagName, CancellationToken cancellationToken = default)
        => RefExistsAsync($"refs/tags/{tagName}");

    public Task<string?> ReadTagCommitAsync(string tagName, CancellationToken cancellationToken = default)
        => ReadRefCommitAsync($"refs/tags/{tagName}", cancellationToken);

    public async Task CreateTagAsync(string tagName, string commitHash, CancellationToken cancellationToken = default)
    {
        ValidateRefName(tagName);
        if (string.IsNullOrWhiteSpace(commitHash))
        {
            throw new InvalidOperationException("Tag target commit must not be empty.");
        }

        await CreateRefAsync($"refs/tags/{tagName}", commitHash, cancellationToken);
    }

    private async Task CreateRefAsync(string refName, string commitHash, CancellationToken cancellationToken)
    {
        var refPath = ToRefPath(refName);
        if (File.Exists(refPath))
        {
            throw new InvalidOperationException($"Reference '{refName}' already exists.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(refPath)!);
        var body = string.IsNullOrWhiteSpace(commitHash) ? string.Empty : $"{commitHash}\n";
        await FileHelpers.AtomicWriteAsync(refPath, Encoding.UTF8.GetBytes(body), cancellationToken);
    }

    private async Task<string?> ReadRefCommitAsync(string refName, CancellationToken cancellationToken)
    {
        var refPath = ToRefPath(refName);
        var content = (await FileHelpers.ReadTextIfExistsAsync(refPath, cancellationToken) ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(content) ? null : content;
    }

    private Task<bool> RefExistsAsync(string refName)
    {
        var refPath = ToRefPath(refName);
        return Task.FromResult(File.Exists(refPath));
    }

    private Task<IReadOnlyList<string>> ListRefNamesAsync(string root)
    {
        if (!Directory.Exists(root))
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var names = Directory
            .EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult<IReadOnlyList<string>>(names);
    }

    private static void ValidateRefName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Reference name must not be empty.");
        }

        if (name.StartsWith('/') || name.EndsWith('/') || name.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Invalid reference name: {name}");
        }

        if (!RefNameRegex.IsMatch(name))
        {
            throw new InvalidOperationException($"Invalid reference name: {name}");
        }
    }

    private string ToRefPath(string refName)
    {
        return Path.Combine(_paths.MetaPath, refName.Replace('/', Path.DirectorySeparatorChar));
    }
}
