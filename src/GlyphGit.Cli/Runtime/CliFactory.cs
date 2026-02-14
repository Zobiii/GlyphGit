using GlyphGit.Application.UseCases;
using GlyphGit.Cli.Rendering;
using GlyphGit.Infrastructure;
using GlyphGit.Logging;
using GlyphGit.Logging.Sinks;
using Spectre.Console;

namespace GlyphGit.Cli.Runtime;

public sealed class CliFactory
{
    private readonly IAnsiConsole _console;

    public CliFactory(IAnsiConsole console)
    {
        _console = console;
    }

    public RuntimeScope Create(bool forInit)
    {
        var repo = forInit
            ? RepositoryRuntime.ForInit(Environment.CurrentDirectory)
            : RepositoryRuntime.OpenExisting(Environment.CurrentDirectory);

        var logFile = Path.Combine(repo.Paths.LogsPath, $"{DateTime.UtcNow:yyyyMMdd}.jsonl");

        var logger = new ExecutionLogger(
        [
            new SpectreEventSink(_console),
            new JsonLinesEventSink(logFile)
        ]);

        return new RuntimeScope(
            repo,
            new InitRepositoryUseCase(repo.RefStore, repo.IndexStore, logger),
            new AddUseCase(repo.WorkingTree, repo.ObjectStore, repo.IndexStore, repo.RepositoryLock, logger),
            new CommitUseCase(repo.IndexStore, repo.ObjectStore, repo.RefStore, repo.RepositoryLock, logger),
            new StatusUseCase(repo.IndexStore, repo.RefStore, repo.ObjectStore, repo.WorkingTree, repo.Hasher, logger),
            new LogUseCase(repo.RefStore, repo.ObjectStore, logger),
            new DiffUseCase(repo.IndexStore, repo.RefStore, repo.ObjectStore, repo.WorkingTree, repo.Hasher, logger),
            new RestoreUseCase(repo.IndexStore, repo.RefStore, repo.ObjectStore, repo.WorkingTree, repo.RepositoryLock, logger),
            new BranchUseCase(repo.RefStore, repo.RepositoryLock, logger),
            new SwitchUseCase(repo.RefStore, repo.IndexStore, repo.ObjectStore, repo.WorkingTree, repo.RepositoryLock, logger));
    }
}

public sealed record RuntimeScope(
    RepositoryRuntime Repository,
    InitRepositoryUseCase Init,
    AddUseCase Add,
    CommitUseCase Commit,
    StatusUseCase Status,
    LogUseCase Log,
    DiffUseCase Diff,
    RestoreUseCase Restore,
    BranchUseCase Branch,
    SwitchUseCase Switch);
