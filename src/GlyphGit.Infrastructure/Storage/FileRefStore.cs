using System.Text;
using System.Text.RegularExpressions;
using GlyphGit.Application.Abstractions;
using GlyphGit.Infrastructure.Filesystem;

namespace GlyphGit.Infrastructure.Storage;

public sealed class FileRefStore : IRefStore
{
    private static readonly Regex BranchNameRegex =
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

    public Task<IReadOnlyList<string>> ListBranchesAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_paths.RefsHeadsPath))
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var branches = Directory
            .EnumerateFiles(_paths.RefsHeadsPath, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(_paths.RefsHeadsPath, path).Replace('\\', '/'))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult<IReadOnlyList<string>>(branches);
    }

    public async Task<bool> BranchExistsAsync(string branchName, CancellationToken cancellationToken = default)
    {
        ValidateBranchName(branchName);
        var refPath = ToRefPath($"refs/heads/{branchName}");
        _ = await Task.FromResult(0);
        return File.Exists(refPath);
    }

    public async Task<string?> ReadBranchCommitAsync(string branchName, CancellationToken cancellationToken = default)
    {
        ValidateBranchName(branchName);
        var refPath = ToRefPath($"refs/heads/{branchName}");
        var content = (await FileHelpers.ReadTextIfExistsAsync(refPath, cancellationToken) ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(content) ? null : content;
    }

    public async Task CreateBranchAsync(string branchName, string? commitHash, CancellationToken cancellationToken = default)
    {
        ValidateBranchName(branchName);

        var refPath = ToRefPath($"refs/heads/{branchName}");
        if (File.Exists(refPath))
        {
            throw new InvalidOperationException($"Branch '{branchName}' already exists.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(refPath)!);
        var body = string.IsNullOrWhiteSpace(commitHash) ? string.Empty : $"{commitHash}\n";
        await FileHelpers.AtomicWriteAsync(refPath, Encoding.UTF8.GetBytes(body), cancellationToken);
    }

    private static void ValidateBranchName(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            throw new InvalidOperationException("Branch name must not be empty.");
        }

        if (branchName.StartsWith('/') || branchName.EndsWith('/') || branchName.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Invalid branch name: {branchName}");
        }

        if (!BranchNameRegex.IsMatch(branchName))
        {
            throw new InvalidOperationException($"Invalid branch name: {branchName}");
        }
    }

    private string ToRefPath(string refName)
    {
        return Path.Combine(_paths.MetaPath, refName.Replace('/', Path.DirectorySeparatorChar));
    }
}
