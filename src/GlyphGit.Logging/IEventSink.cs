namespace GlyphGit.Logging;

public interface IEventSink
{
    ValueTask WriteAsync(LogEvent logEvent, CancellationToken cancellationToken = default);
}