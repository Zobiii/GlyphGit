using GlyphGit.Application.Abstractions;
using GlyphGit.Application.Models;
using GlyphGit.Domain.Objects;
using GlyphGit.Domain.Status;
using GlyphGit.Logging;

namespace GlyphGit.Application.UseCases;

public sealed class StatusUseCase
{
    private readonly IIndexStore _indexStore;
    private readonly IRefStore _refStore;
    private readonly IObjectStore _objectStore;
    private readonly IWorkingTree _workingTree;
    private readonly IHasher _hasher;
    private readonly IExecutionLogger _logger;

    public StatusUseCase(
        IIndexStore indexStore,
        IRefStore refStore,
        IObjectStore objectStore,
        IWorkingTree workingTree,
        IHasher hasher,
        IExecutionLogger logger)
    {
        _indexStore = indexStore;
        _refStore = refStore;
        _objectStore = objectStore;
        _workingTree = workingTree;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task<CommandResult<IReadOnlyList<StatusItem>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = await _logger.BeginCommandAsync("status", cancellationToken: cancellationToken);

        var index = await _indexStore.ReadAsync(cancellationToken);
        var headCommitHash = await _refStore.ReadHeadCommitAsync(cancellationToken);
        var headTreeMap = await LoadHeadMapAsync(headCommitHash, cancellationToken);
        var files = await _workingTree.EnumerateFilesAsync(null, cancellationToken);

        var indexMap = index.ToDictionary(x => x.Path, x => x, StringComparer.Ordinal);
        var worktreeSet = files.ToHashSet(StringComparer.Ordinal);
        var items = new List<StatusItem>();

        foreach (var entry in index)
        {
            if (!headTreeMap.TryGetValue(entry.Path, out var headHash))
            {
                items.Add(new StatusItem(StatusArea.Staged, entry.Path, FileState.Added));
            }
            else if (!string.Equals(headHash, entry.BlobHash, StringComparison.Ordinal))
            {
                items.Add(new StatusItem(StatusArea.Staged, entry.Path, FileState.Modified));
            }

            if (!worktreeSet.Contains(entry.Path))
            {
                items.Add(new StatusItem(StatusArea.Worktree, entry.Path, FileState.Deleted));
                continue;
            }

            var content = await _workingTree.ReadFileAsync(entry.Path, cancellationToken);
            var currentHash = _hasher.ComputeHash(BuildObjectBytes("blob", content));
            var legacyHash = _hasher.ComputeHash(BuildLegacyObjectBytes("blob", content));
            if (!string.Equals(currentHash, entry.BlobHash, StringComparison.Ordinal) &&
                !string.Equals(legacyHash, entry.BlobHash, StringComparison.Ordinal))
            {
                items.Add(new StatusItem(StatusArea.Worktree, entry.Path, FileState.Modified));
            }
        }

        foreach (var file in files)
        {
            if (!indexMap.ContainsKey(file))
            {
                items.Add(new StatusItem(StatusArea.Untracked, file, FileState.Added));
            }
        }

        return CommandResult<IReadOnlyList<StatusItem>>.Ok("Status calculated.", items);
    }

    private async Task<Dictionary<string, string>> LoadHeadMapAsync(string? commitHash, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(commitHash))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var commitObj = await _objectStore.ReadAsync(commitHash, cancellationToken);
        if (commitObj is null || commitObj.Type != GitObjectType.Commit)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var commit = ObjectCodec.DeserializeCommit(commitObj.Payload);
        var treeObj = await _objectStore.ReadAsync(commit.TreeHash, cancellationToken);
        if (treeObj is null || treeObj.Type != GitObjectType.Tree)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var tree = ObjectCodec.DeserializeTree(treeObj.Payload);
        return tree.Entries.ToDictionary(x => x.Path, x => x.Hash, StringComparer.Ordinal);
    }

    private static byte[] BuildObjectBytes(string kind, byte[] payload)
    {
        var head = System.Text.Encoding.UTF8.GetBytes($"{kind} {payload.Length}\0");
        var result = new byte[head.Length + payload.Length];
        Buffer.BlockCopy(head, 0, result, 0, head.Length);
        Buffer.BlockCopy(payload, 0, result, head.Length, payload.Length);
        return result;
    }

    private static byte[] BuildLegacyObjectBytes(string kind, byte[] payload)
    {
        var head = System.Text.Encoding.UTF8.GetBytes($"{kind}\n");
        var result = new byte[head.Length + payload.Length];
        Buffer.BlockCopy(head, 0, result, 0, head.Length);
        Buffer.BlockCopy(payload, 0, result, head.Length, payload.Length);
        return result;
    }
}
