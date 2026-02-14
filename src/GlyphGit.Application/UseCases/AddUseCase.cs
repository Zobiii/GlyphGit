using GlyphGit.Application.Abstractions;
using GlyphGit.Application.Models;
using GlyphGit.Domain.Index;
using GlyphGit.Domain.Objects;
using GlyphGit.Logging;

namespace GlyphGit.Application.UseCases;

public sealed class AddUseCase
{
    private readonly IWorkingTree _workingTree;
    private readonly IObjectStore _objectStore;
    private readonly IIndexStore _indexStore;
    private readonly IRepositoryLock _repositoryLock;
    private readonly IExecutionLogger _logger;

    public AddUseCase(
        IWorkingTree workingTree,
        IObjectStore objectStore,
        IIndexStore indexStore,
        IRepositoryLock repositoryLock,
        IExecutionLogger logger)
    {
        _workingTree = workingTree;
        _objectStore = objectStore;
        _indexStore = indexStore;
        _repositoryLock = repositoryLock;
        _logger = logger;
    }

    public async Task<CommandResult<int>> ExecuteAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken = default)
    {
        await using var scope = await _logger.BeginCommandAsync("add", cancellationToken: cancellationToken);
        await using var repoLock = await _repositoryLock.AcquireAsync(cancellationToken);

        var files = await scope.RunStepAsync<IReadOnlyList<string>>(
            "scan-files",
            ct => _workingTree.EnumerateFilesAsync(paths, ct),
            new Dictionary<string, string> { ["inputCount"] = paths.Count.ToString() },
            cancellationToken);

        if (files.Count == 0)
        {
            return CommandResult<int>.Ok("No files matched.", 0);
        }

        var staged = new List<IndexEntry>();
        foreach (var file in files)
        {
            var content = await _workingTree.ReadFileAsync(file, cancellationToken);
            var hash = await _objectStore.WriteAsync(GitObjectType.Blob, content, cancellationToken);
            var mtime = await _workingTree.GetLastWriteTimeUtcAsync(file, cancellationToken) ?? DateTimeOffset.UtcNow;
            var size = await _workingTree.GetFileSizeAsync(file, cancellationToken) ?? content.Length;

            staged.Add(new IndexEntry(file, hash, "100644", size, mtime));

            await scope.LogAsync(
                LogLevel.Trace,
                "FileStaged",
                $"Staged '{file}'.",
                new Dictionary<string, string> { ["path"] = file, ["blob"] = hash },
                cancellationToken);
        }

        await scope.RunStepAsync("write-index", ct => _indexStore.UpsertAsync(staged, ct), cancellationToken: cancellationToken);
        return CommandResult<int>.Ok($"Staged {staged.Count} file(s).", staged.Count);
    }
}

