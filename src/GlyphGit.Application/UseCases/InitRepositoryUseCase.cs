using GlyphGit.Application.Abstractions;
using GlyphGit.Application.Models;
using GlyphGit.Logging;

namespace GlyphGit.Application.UseCases;

public sealed class InitRepositoryUseCase
{
    private readonly IRefStore _refStore;
    private readonly IIndexStore _indexStore;
    private readonly IExecutionLogger _logger;

    public InitRepositoryUseCase(IRefStore refStore, IIndexStore indexStore, IExecutionLogger logger)
    {
        _refStore = refStore;
        _indexStore = indexStore;
        _logger = logger;
    }

    public async Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = await _logger.BeginCommandAsync("init", cancellationToken: cancellationToken);

        await scope.RunStepAsync("initialize-refs", ct => _refStore.InitializeAsync(ct), cancellationToken: cancellationToken);
        await scope.RunStepAsync("initialize-index", ct => _indexStore.WriteAsync([], ct), cancellationToken: cancellationToken);

        return CommandResult.Ok("Initialized empty GlyphGit repository (.glyphgit).");
    }
}
