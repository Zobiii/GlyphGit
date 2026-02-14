namespace GlyphGit.Logging.Sinks;

public sealed class InMemoryEventSink : IEventSink
{
    private readonly List<LogEvent> _events = [];

    public IReadOnlyList<LogEvent> Events => _events;

    public ValueTask WriteAsync(LogEvent logEvent, CancellationToken cancellationToken = default)
    {
        _events.Add(logEvent);
        return ValueTask.CompletedTask;
    }
}
