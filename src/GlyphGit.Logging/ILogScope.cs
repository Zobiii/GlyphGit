namespace GlyphGit.Logging;

public interface ILogScope : IAsyncDisposable
{
    Task RunStepAsync(
        string stepName,
        Func<CancellationToken, Task> action,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default
    );

    Task<T> RunStepAsync<T>(
        string stepName,
        Func<CancellationToken, Task<T>> action,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default
    );

    ValueTask LogAsync(
        LogLevel level,
        string eventName,
        string message,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default
    );
}
