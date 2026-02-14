using System.Diagnostics;

namespace GlyphGit.Logging;

public sealed class ExecutionLogger : IExecutionLogger
{
    private readonly IReadOnlyList<IEventSink> _sinks;

    public ExecutionLogger(IEnumerable<IEventSink> sinks)
    {
        _sinks = sinks.ToArray();
    }

    public async Task<ILogScope> BeginCommandAsync(
        string command,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var scope = new Scope(_sinks, command, Guid.NewGuid().ToString("N"));
        await scope.LogAsync(LogLevel.Info, "CommandStarted", $"Command '{command}' started.", data, cancellationToken);
        return scope;
    }

    private sealed class Scope : ILogScope
    {
        private readonly IReadOnlyList<IEventSink> _sinks;
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        private bool _isDisposed;

        public Scope(IReadOnlyList<IEventSink> sinks, string command, string correlationId)
        {
            _sinks = sinks;
            Command = command;
            CorrelationId = correlationId;
        }

        public string Command { get; }
        public string CorrelationId { get; }

        public async Task RunStepAsync(
            string stepName,
            Func<CancellationToken, Task> action,
            IReadOnlyDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
        {
            await RunStepAsync<object?>(
                stepName,
                async ct =>
                {
                    await action(ct);
                    return null;
                },
                data,
                cancellationToken);
        }

        public async Task<T> RunStepAsync<T>(
            string stepName,
            Func<CancellationToken, Task<T>> action,
            IReadOnlyDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
        {
            var stepWatch = Stopwatch.StartNew();
            await LogAsync(LogLevel.Info, "StepStarted", $"Step '{stepName}' started.", Merge(data, ("step", stepName)), cancellationToken);

            try
            {
                var value = await action(cancellationToken);
                await LogAsync(
                    LogLevel.Info,
                    "StepCompleted",
                    $"Step '{stepName}' completed.",
                    Merge(data, ("step", stepName), ("durationMs", stepWatch.ElapsedMilliseconds.ToString())),
                    cancellationToken);
                return value;
            }
            catch (Exception ex)
            {
                await LogAsync(
                    LogLevel.Error,
                    "StepFailed",
                    $"Step '{stepName}' failed: {ex.Message}",
                    Merge(data, ("step", stepName), ("durationMs", stepWatch.ElapsedMilliseconds.ToString())),
                    cancellationToken);
                throw;
            }
        }

        public async ValueTask LogAsync(
            LogLevel level,
            string eventName,
            string message,
            IReadOnlyDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
            {
                return;
            }

            var payload = data is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(data, StringComparer.Ordinal);

            payload["command"] = Command;
            payload["correlationId"] = CorrelationId;

            var evt = new LogEvent(
                DateTimeOffset.UtcNow,
                level,
                eventName,
                message,
                Command,
                CorrelationId,
                payload);

            foreach (var sink in _sinks)
            {
                await sink.WriteAsync(evt, cancellationToken);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            await LogAsync(
                LogLevel.Info,
                "CommandCompleted",
                $"Command '{Command}' completed.",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["durationMs"] = _watch.ElapsedMilliseconds.ToString()
                });
            _isDisposed = true;
        }

        private static IReadOnlyDictionary<string, string> Merge(
            IReadOnlyDictionary<string, string>? input,
            params (string Key, string Value)[] pairs)
        {
            var output = input is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(input, StringComparer.Ordinal);

            foreach (var (k, v) in pairs)
            {
                output[k] = v;
            }

            return output;
        }
    }
}

