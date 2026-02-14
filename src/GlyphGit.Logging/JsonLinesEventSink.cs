using System.Text;
using System.Text.Json;

namespace GlyphGit.Logging.Sinks;

public sealed class JsonLinesEventSink : IEventSink
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonLinesEventSink(string filePath)
    {
        _filePath = filePath;
    }

    public async ValueTask WriteAsync(LogEvent logEvent, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        var json = JsonSerializer.Serialize(logEvent);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var stream = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            await writer.WriteLineAsync(json.AsMemory(), cancellationToken);
            await writer.FlushAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }
}
