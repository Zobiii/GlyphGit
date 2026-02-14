using GlyphGit.Application.Abstractions;
using GlyphGit.Application.Models;
using GlyphGit.Logging;

namespace GlyphGit.Application.UseCases;

public sealed class TagUseCase
{
    private readonly IRefStore _refStore;
    private readonly IRepositoryLock _repositoryLock;
    private readonly IExecutionLogger _logger;

    public TagUseCase(
        IRefStore refStore,
        IRepositoryLock repositoryLock,
        IExecutionLogger logger)
    {
        _refStore = refStore;
        _repositoryLock = repositoryLock;
        _logger = logger;
    }

    public async Task<CommandResult<IReadOnlyList<TagInfo>>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = await _logger.BeginCommandAsync("tag", cancellationToken: cancellationToken);

        var tags = await _refStore.ListTagsAsync(cancellationToken);
        var result = new List<TagInfo>();

        foreach (var tag in tags)
        {
            var commit = await _refStore.ReadTagCommitAsync(tag, cancellationToken);
            result.Add(new TagInfo(tag, commit));
        }

        return CommandResult<IReadOnlyList<TagInfo>>.Ok("Tags loaded.", result);
    }

    public async Task<CommandResult> CreateAsync(string tagName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return CommandResult.Fail(ExitCode.InvalidUsage, "Tag name is required.");
        }

        await using var scope = await _logger.BeginCommandAsync("tag", cancellationToken: cancellationToken);
        await using var repoLock = await _repositoryLock.AcquireAsync(cancellationToken);

        var exists = await _refStore.TagExistsAsync(tagName, cancellationToken);
        if (exists)
        {
            return CommandResult.Fail(ExitCode.Conflict, $"Tag '{tagName}' already exists.");
        }

        var head = await _refStore.ReadHeadCommitAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(head))
        {
            return CommandResult.Fail(ExitCode.Conflict, "Cannot create tag without a commit.");
        }

        await _refStore.CreateTagAsync(tagName.Trim(), head, cancellationToken);
        return CommandResult.Ok($"Created tag '{tagName}'.");
    }
}

public sealed record TagInfo(string Name, string? CommitHash);
