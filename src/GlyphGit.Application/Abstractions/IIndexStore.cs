using GlyphGit.Domain.Index;

namespace GlyphGit.Application.Abstractions;

public interface IIndexStore
{
    Task<IReadOnlyList<IndexEntry>> ReadAsync(CancellationToken cancellationToken = default);
    Task WriteAsync(IReadOnlyList<IndexEntry> entries, CancellationToken cancellationToken = default);
    Task UpsertAsync(IEnumerable<IndexEntry> entries, CancellationToken cancellationToken = default);
    Task RemoveAsync(IEnumerable<string> relativePaths, CancellationToken cancellationToken = default);
}
