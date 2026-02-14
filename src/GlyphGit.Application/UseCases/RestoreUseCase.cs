using GlyphGit.Application.Abstractions;
using GlyphGit.Application.Models;
using GlyphGit.Domain.Objects;
using GlyphGit.Logging;

namespace GlyphGit.Application.UseCases;

public sealed class RestoreUseCase
{
    private readonly IIndexStore _indexStore;
    private readonly IRefStore _refStore;
    private readonly IObjectStore _objectStore;
    private readonly IWorkingTree _workingTree;
    private readonly IRepositoryLock _repositoryLock;
    private readonly IExecutionLogger _logger;

    public RestoreUseCase(
        IIndexStore indexStore,
        IRefStore refStore,
        IObjectStore objectStore,
        IWorkingTree workingTree,
        IRepositoryLock repositoryLock,
        IExecutionLogger logger)
    {
        _indexStore = indexStore;
        _refStore = refStore;
        _objectStore = objectStore;
        _workingTree = workingTree;
        _repositoryLock = repositoryLock;
        _logger = logger;
    }

    public async Task<CommandResult> ExecuteAsync(
        bool staged,
        bool worktree,
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken = default)
    {
        if (!staged && !worktree)
        {
            return CommandResult.Fail(ExitCode.InvalidUsage, "Specify --staged and/or --worktree.");
        }

        await using var scope = await _logger.BeginCommandAsync("restore", cancellationToken: cancellationToken);
        await using var repoLock = await _repositoryLock.AcquireAsync(cancellationToken);

        var normalizedTargets = paths.Count == 0
            ? null
            : paths.ToHashSet(StringComparer.Ordinal);

        if (staged)
        {
            var headMap = await LoadHeadMapAsync(cancellationToken);
            var current = await _indexStore.ReadAsync(cancellationToken);
            var currentMap = current.ToDictionary(x => x.Path, x => x, StringComparer.Ordinal);

            var untouched = current
                .Where(x => normalizedTargets is not null && !normalizedTargets.Contains(x.Path))
                .ToList();

            var candidatePaths = normalizedTargets is null
                ? headMap.Keys.ToHashSet(StringComparer.Ordinal)
                : normalizedTargets;

            foreach (var path in candidatePaths)
            {
                if (!headMap.TryGetValue(path, out var hash))
                {
                    continue;
                }

                var fileSize = currentMap.TryGetValue(path, out var currentEntry)
                    ? currentEntry.FileSize
                    : 0;

                var lastWriteUtc = currentMap.TryGetValue(path, out currentEntry)
                    ? currentEntry.LastWriteUtc
                    : DateTimeOffset.UtcNow;

                untouched.Add(new GlyphGit.Domain.Index.IndexEntry(path, hash, "100644", fileSize, lastWriteUtc));
            }

            await _indexStore.WriteAsync(untouched.OrderBy(x => x.Path, StringComparer.Ordinal).ToArray(), cancellationToken);
        }

        if (worktree)
        {
            var index = await _indexStore.ReadAsync(cancellationToken);
            var selected = index.Where(x => normalizedTargets is null || normalizedTargets.Contains(x.Path));

            foreach (var entry in selected)
            {
                var blobObj = await _objectStore.ReadAsync(entry.BlobHash, cancellationToken);
                if (blobObj is null || blobObj.Type != GitObjectType.Blob)
                {
                    continue;
                }

                await _workingTree.WriteFileAsync(entry.Path, blobObj.Payload, cancellationToken);
            }
        }

        return CommandResult.Ok("Restore completed.");
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

        return ObjectCodec.DeserializeTree(treeObj.Payload)
            .Entries
            .ToDictionary(x => x.Path, x => x.Hash, StringComparer.Ordinal);
    }
}
