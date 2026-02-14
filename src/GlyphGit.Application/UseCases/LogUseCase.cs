using GlyphGit.Application.Abstractions;
using GlyphGit.Application.Models;
using GlyphGit.Domain.Objects;
using GlyphGit.Logging;

namespace GlyphGit.Application.UseCases;

public sealed class LogUseCase
{
    private readonly IRefStore _refStore;
    private readonly IObjectStore _objectStore;
    private readonly IExecutionLogger _logger;

    public LogUseCase(IRefStore refStore, IObjectStore objectStore, IExecutionLogger logger)
    {
        _refStore = refStore;
        _objectStore = objectStore;
        _logger = logger;
    }

    public async Task<CommandResult<IReadOnlyList<(string Hash, CommitObject Commit)>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = await _logger.BeginCommandAsync("log", cancellationToken: cancellationToken);

        var result = new List<(string Hash, CommitObject Commit)>();
        var cursor = await _refStore.ReadHeadCommitAsync(cancellationToken);

        while (!string.IsNullOrWhiteSpace(cursor))
        {
            var obj = await _objectStore.ReadAsync(cursor, cancellationToken);
            if (obj is null || obj.Type != GlyphGit.Domain.Objects.GitObjectType.Commit)
            {
                break;
            }

            var commit = ObjectCodec.DeserializeCommit(obj.Payload);
            result.Add((cursor, commit));
            cursor = commit.Parents.FirstOrDefault();
        }

        return CommandResult<IReadOnlyList<(string Hash, CommitObject Commit)>>.Ok("Log loaded.", result);
    }
}
