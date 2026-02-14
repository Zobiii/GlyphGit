namespace GlyphGit.Logging;

public sealed record LogEvent(
    DateTimeOffset TimestampUtc,
    LogLevel Level,
    string EventName,
    string Message,
    string Command,
    string CorrelationId,
    IReadOnlyDictionary<string, string> Data
);