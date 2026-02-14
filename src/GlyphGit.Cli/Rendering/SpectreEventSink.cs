using GlyphGit.Logging;
using Spectre.Console;

namespace GlyphGit.Cli.Rendering;

public sealed class SpectreEventSink : IEventSink
{
    private readonly IAnsiConsole _console;

    public SpectreEventSink(IAnsiConsole console)
    {
        _console = console;
    }

    public ValueTask WriteAsync(LogEvent logEvent, CancellationToken cancellationToken = default)
    {
        var color = logEvent.Level switch
        {
            LogLevel.Error => "red",
            LogLevel.Warning => "yellow",
            LogLevel.Info => "grey",
            _ => "darkslategray1"
        };

        _console.MarkupLine($"[{color}]log[/] {logEvent.EventName}: {Markup.Escape(logEvent.Message)}");
        return ValueTask.CompletedTask;
    }
}
