using GlyphGit.Application.Abstractions;
using GlyphGit.Application.Models;
using GlyphGit.Domain.Diff;
using GlyphGit.Domain.Objects;
using GlyphGit.Domain.Status;
using GlyphGit.Logging;

namespace GlyphGit.Application.UseCases;

public sealed class DiffUseCase
{
    private readonly IIndexStore _indexStore;
    private readonly IRefStore _refStore;
    private readonly IObjectStore _objectStore;
    private readonly IWorkingTree _workingTree;
    private readonly IHasher _hasher;
    private readonly IExecutionLogger _logger;

    public DiffUseCase(
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

    public async Task<CommandResult<IReadOnlyList<DiffEntry>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = await _logger.BeginCommandAsync("diff", cancellationToken: cancellationToken);

        var index = await _indexStore.ReadAsync(cancellationToken);
        var indexMap = index.ToDictionary(x => x.Path, x => x.BlobHash, StringComparer.Ordinal);

        var headMap = await LoadHeadMapAsync(cancellationToken);
        var diffs = new List<DiffEntry>();

        foreach (var kv in indexMap)
        {
            if (!headMap.TryGetValue(kv.Key, out var headHash))
            {
                diffs.Add(new DiffEntry(DiffScope.IndexToHead, kv.Key, FileState.Added, null, kv.Value));
            }
            else if (!string.Equals(headHash, kv.Value, StringComparison.Ordinal))
            {
                diffs.Add(new DiffEntry(DiffScope.IndexToHead, kv.Key, FileState.Modified, headHash, kv.Value));
            }
        }

        foreach (var kv in headMap)
        {
            if (!indexMap.ContainsKey(kv.Key))
            {
                diffs.Add(new DiffEntry(DiffScope.IndexToHead, kv.Key, FileState.Deleted, kv.Value, null));
            }
        }

        var wtFiles = await _workingTree.EnumerateFilesAsync(null, cancellationToken);
        var wtSet = wtFiles.ToHashSet(StringComparer.Ordinal);

        foreach (var kv in indexMap)
        {
            if (!wtSet.Contains(kv.Key))
            {
                diffs.Add(new DiffEntry(DiffScope.WorktreeToIndex, kv.Key, FileState.Deleted, kv.Value, null));
                continue;
            }

            var content = await _workingTree.ReadFileAsync(kv.Key, cancellationToken);
            var wtHash = _hasher.ComputeHash(BuildObjectBytes("blob", content));
            var legacyHash = _hasher.ComputeHash(BuildLegacyObjectBytes("blob", content));
            if (!string.Equals(wtHash, kv.Value, StringComparison.Ordinal) &&
                !string.Equals(legacyHash, kv.Value, StringComparison.Ordinal))
            {
                diffs.Add(new DiffEntry(DiffScope.WorktreeToIndex, kv.Key, FileState.Modified, kv.Value, wtHash));
            }
        }

        foreach (var file in wtFiles)
        {
            if (!indexMap.ContainsKey(file))
            {
                diffs.Add(new DiffEntry(DiffScope.WorktreeToIndex, file, FileState.Added, null, null));
            }
        }

        return CommandResult<IReadOnlyList<DiffEntry>>.Ok("Diff created.", diffs);
    }

    private async Task<Dictionary<string, string>> LoadHeadMapAsync(CancellationToken cancellationToken)
    {
        var head = await _refStore.ReadHeadCommitAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(head))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var commitObj = await _objectStore.ReadAsync(head, cancellationToken);
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

        return ObjectCodec
            .DeserializeTree(treeObj.Payload)
            .Entries
            .ToDictionary(x => x.Path, x => x.Hash, StringComparer.Ordinal);
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
