using System.Runtime.CompilerServices;
using GlyphGit.Application.Abstractions;
using GlyphGit.Application.Models;
using GlyphGit.Logging;
using Microsoft.VisualBasic;

namespace GlyphGit.Application.UseCases;

public sealed class BranchUseCase
{
    private readonly IRefStore _refStore;
    private readonly IRepositoryLock _repositoryLock;
    private readonly IExecutionLogger _logger;

    public BranchUseCase(
        IRefStore refStore,
        IRepositoryLock repositoryLock,
        IExecutionLogger logger)
    {
        _refStore = refStore;
        _repositoryLock = repositoryLock;
        _logger = logger;
    }

    public async Task<CommandResult<IReadOnlyList<BranchInfo>>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = await _logger.BeginCommandAsync("branch", cancellationToken: cancellationToken);

        var headRef = await _refStore.ReadHeadReferenceAsync(cancellationToken);
        var branches = await _refStore.ListBranchesAsync(cancellationToken);

        var result = new List<BranchInfo>();
        foreach (var branch in branches)
        {
            var commit = await _refStore.ReadBranchCommitAsync(branch, cancellationToken);
            var isCurrent = string.Equals(headRef, $"refs/heads/{branch}", StringComparison.Ordinal);
            result.Add(new BranchInfo(branch, isCurrent, commit));
        }

        return CommandResult<IReadOnlyList<BranchInfo>>.Ok("Branches loaded.", result);
    }

    public async Task<CommandResult> CreateAsync(string branchName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return CommandResult.Fail(ExitCode.InvalidUsage, "Branch name is required.");
        }

        await using var scope = await _logger.BeginCommandAsync("branch", cancellationToken: cancellationToken);
        await using var repoLock = await _repositoryLock.AcquireAsync(cancellationToken);

        var exists = await _refStore.BranchExistsAsync(branchName, cancellationToken);
        if (exists)
        {
            return CommandResult.Fail(ExitCode.Conflict, $"Branch '{branchName}' already exists.");
        }

        var headCommit = await _refStore.ReadHeadCommitAsync(cancellationToken);
        await _refStore.CreateBranchAsync(branchName.Trim(), headCommit, cancellationToken);

        return CommandResult.Ok($"Created branch '{branchName}'.");
    }
}

public sealed record BranchInfo(string Name, bool isCurrent, string? CommitHash);