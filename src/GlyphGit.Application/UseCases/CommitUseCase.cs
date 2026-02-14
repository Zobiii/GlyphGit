using GlyphGit.Application.Abstractions;
using GlyphGit.Application.Models;
using GlyphGit.Domain.Objects;
using GlyphGit.Logging;

namespace GlyphGit.Application.UseCases;

public sealed class CommitUseCase
{
    private readonly IIndexStore _indexStore;
    private readonly IObjectStore _objectStore;
    private readonly IRefStore _refStore;
    private readonly IRepositoryLock _repositoryLock;
    private readonly IExecutionLogger _logger;

    public CommitUseCase(
        IIndexStore indexStore,
        IObjectStore objectStore,
        IRefStore refStore,
        IRepositoryLock repositoryLock,
        IExecutionLogger logger)
    {
        _indexStore = indexStore;
        _objectStore = objectStore;
        _refStore = refStore;
        _repositoryLock = repositoryLock;
        _logger = logger;
    }

    public async Task<CommandResult<string>> ExecuteAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return CommandResult<string>.Fail(ExitCode.InvalidUsage, "Commit message must not be empty.");
        }

        await using var scope = await _logger.BeginCommandAsync("commit", cancellationToken: cancellationToken);
        await using var repoLock = await _repositoryLock.AcquireAsync(cancellationToken);

        var indexEntries = await scope.RunStepAsync<IReadOnlyList<GlyphGit.Domain.Index.IndexEntry>>("read-index", ct => _indexStore.ReadAsync(ct), cancellationToken: cancellationToken);
        if (indexEntries.Count == 0)
        {
            return CommandResult<string>.Fail(ExitCode.Conflict, "Nothing to commit. Index is empty.");
        }

        var tree = new TreeObject(indexEntries
            .OrderBy(x => x.Path, StringComparer.Ordinal)
            .Select(x => new TreeEntry(x.Path, x.Mode, x.BlobHash))
            .ToArray());

        var treeHash = await _objectStore.WriteAsync(GitObjectType.Tree, ObjectCodec.SerializeTree(tree), cancellationToken);
        var parent = await _refStore.ReadHeadCommitAsync(cancellationToken);

        var user = Environment.UserName;
        var identity = $"{user} <{user}@localhost>";

        var commit = new CommitObject(
            treeHash,
            string.IsNullOrWhiteSpace(parent) ? [] : [parent],
            identity,
            identity,
            DateTimeOffset.UtcNow,
            message.Trim());

        var commitHash = await _objectStore.WriteAsync(GitObjectType.Commit, ObjectCodec.SerializeCommit(commit), cancellationToken);
        await _refStore.WriteHeadCommitAsync(commitHash, cancellationToken);

        await scope.LogAsync(
            LogLevel.Info,
            "CommitCreated",
            $"Commit '{commitHash[..7]}' created.",
            new Dictionary<string, string> { ["tree"] = treeHash, ["commit"] = commitHash },
            cancellationToken);

        return CommandResult<string>.Ok($"[{commitHash[..7]}] {message.Trim()}", commitHash);
    }
}

