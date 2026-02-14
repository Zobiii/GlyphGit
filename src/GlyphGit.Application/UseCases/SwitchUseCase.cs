using GlyphGit.Application.Abstractions;
using GlyphGit.Application.Models;
using GlyphGit.Domain.Index;
using GlyphGit.Domain.Objects;
using GlyphGit.Logging;

namespace GlyphGit.Application.UseCases;

public sealed class SwitchUseCase
{
    private readonly IRefStore _refStore;
    private readonly IIndexStore _indexStore;
    private readonly IObjectStore _objectStore;
    private readonly IWorkingTree _workingTree;
    private readonly IRepositoryLock _repositoryLock;
    private readonly IExecutionLogger _logger;

    public SwitchUseCase(
        IRefStore refStore,
        IIndexStore indexStore,
        IObjectStore objectStore,
        IWorkingTree workingTree,
        IRepositoryLock repositoryLock,
        IExecutionLogger logger)
    {
        _refStore = refStore;
        _indexStore = indexStore;
        _objectStore = objectStore;
        _workingTree = workingTree;
        _repositoryLock = repositoryLock;
        _logger = logger;
    }

    public async Task<CommandResult> ExecuteAsync(string branchName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return CommandResult.Fail(ExitCode.InvalidUsage, "Branch name is required.");
        }

        await using var scope = await _logger.BeginCommandAsync("switch", cancellationToken: cancellationToken);
        await using var repoLock = await _repositoryLock.AcquireAsync(cancellationToken);

        var exists = await _refStore.BranchExistsAsync(branchName, cancellationToken);
        if (!exists)
        {
            return CommandResult.Fail(ExitCode.RepositoryError, $"Branch '{branchName}' does not exist.");
        }

        var targetCommit = await _refStore.ReadBranchCommitAsync(branchName, cancellationToken);

        await _refStore.SetHeadReferenceAsync(branchName, cancellationToken);

        if (string.IsNullOrWhiteSpace(targetCommit))
        {
            await _indexStore.WriteAsync([], cancellationToken);
            return CommandResult.Ok($"Switched to branch '{branchName}'.");
        }

        var commitObj = await _objectStore.ReadAsync(targetCommit, cancellationToken);
        if (commitObj is null || commitObj.Type != GitObjectType.Commit)
        {
            return CommandResult.Fail(ExitCode.RepositoryError, "Target branch points to invalid commit object.");
        }

        var commit = ObjectCodec.DeserializeCommit(commitObj.Payload);
        var treeObj = await _objectStore.ReadAsync(commit.TreeHash, cancellationToken);
        if (treeObj is null || treeObj.Type != GitObjectType.Tree)
        {
            return CommandResult.Fail(ExitCode.RepositoryError, "Commit tree is missing or invalid.");
        }

        var tree = ObjectCodec.DeserializeTree(treeObj.Payload);
        var targetMap = tree.Entries.ToDictionary(x => x.Path, x => x, StringComparer.Ordinal);

        var currentIndex = await _indexStore.ReadAsync(cancellationToken);
        var currentPaths = currentIndex.Select(x => x.Path).ToHashSet(StringComparer.Ordinal);

        foreach (var path in currentPaths)
        {
            if (!targetMap.ContainsKey(path))
            {
                await _workingTree.DeleteFileAsync(path, cancellationToken);
            }
        }

        var nextIndex = new List<IndexEntry>();
        foreach (var entry in targetMap.Values.OrderBy(x => x.Path, StringComparer.Ordinal))
        {
            var blobObj = await _objectStore.ReadAsync(entry.Hash, cancellationToken);
            if (blobObj is null || blobObj.Type != GitObjectType.Blob)
            {
                return CommandResult.Fail(ExitCode.RepositoryError, $"Missing blob for '{entry.Path}'.");
            }

            await _workingTree.WriteFileAsync(entry.Path, blobObj.Payload, cancellationToken);

            var size = await _workingTree.GetFileSizeAsync(entry.Path, cancellationToken) ?? blobObj.Payload.Length;
            var mtime = await _workingTree.GetLastWriteTimeUtcAsync(entry.Path, cancellationToken) ?? DateTimeOffset.UtcNow;
            nextIndex.Add(new IndexEntry(entry.Path, entry.Hash, entry.Mode, size, mtime));
        }

        await _indexStore.WriteAsync(nextIndex, cancellationToken);

        return CommandResult.Ok($"Switched to branch '{branchName}'.");
    }
}
