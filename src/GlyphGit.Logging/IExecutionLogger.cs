namespace GlyphGit.Logging;

public interface IExecutionLogger
{
    Task<ILogScope> BeginCommandAsync(
        string command,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default
    );
}