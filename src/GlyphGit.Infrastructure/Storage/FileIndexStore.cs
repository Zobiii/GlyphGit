using System.Globalization;
using System.Text;
using GlyphGit.Application.Abstractions;
using GlyphGit.Domain.Index;
using GlyphGit.Infrastructure.Filesystem;

namespace GlyphGit.Infrastructure.Storage;

public sealed class FileIndexStore : IIndexStore
{
    private readonly string _indexPath;

    public FileIndexStore(string indexPath)
    {
        _indexPath = indexPath;
    }

    public async Task<IReadOnlyList<IndexEntry>> ReadAsync(CancellationToken cancellationToken = default)
    {
        var text = await FileHelpers.ReadTextIfExistsAsync(_indexPath, cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var entries = new List<IndexEntry>();
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t');
            if (parts.Length != 5)
            {
                continue;
            }

            var path = parts[0];
            var hash = parts[1];
            var mode = parts[2];
            var size = long.Parse(parts[3], CultureInfo.InvariantCulture);
            var ts = long.Parse(parts[4], CultureInfo.InvariantCulture);
            entries.Add(new IndexEntry(path, hash, mode, size, DateTimeOffset.FromUnixTimeSeconds(ts)));
        }

        return entries.OrderBy(x => x.Path, StringComparer.Ordinal).ToArray();
    }

    public async Task WriteAsync(IReadOnlyList<IndexEntry> entries, CancellationToken cancellationToken = default)
    {
        var sorted = entries.OrderBy(x => x.Path, StringComparer.Ordinal);
        var sb = new StringBuilder();

        foreach (var e in sorted)
        {
            sb.Append(e.Path).Append('\t')
              .Append(e.BlobHash).Append('\t')
              .Append(e.Mode).Append('\t')
              .Append(e.FileSize.ToString(CultureInfo.InvariantCulture)).Append('\t')
              .Append(e.LastWriteUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))
              .Append('\n');
        }

        await FileHelpers.AtomicWriteAsync(_indexPath, Encoding.UTF8.GetBytes(sb.ToString()), cancellationToken);
    }

    public async Task UpsertAsync(IEnumerable<IndexEntry> entries, CancellationToken cancellationToken = default)
    {
        var current = await ReadAsync(cancellationToken);
        var map = current.ToDictionary(x => x.Path, x => x, StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            map[entry.Path] = entry;
        }

        await WriteAsync(map.Values.OrderBy(x => x.Path, StringComparer.Ordinal).ToArray(), cancellationToken);
    }

    public async Task RemoveAsync(IEnumerable<string> relativePaths, CancellationToken cancellationToken = default)
    {
        var set = relativePaths.ToHashSet(StringComparer.Ordinal);
        var current = await ReadAsync(cancellationToken);
        var filtered = current.Where(x => !set.Contains(x.Path)).ToArray();
        await WriteAsync(filtered, cancellationToken);
    }
}

