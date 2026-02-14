using GlyphGit.Domain.Objects;
using GlyphGit.Application.Models;

namespace GlyphGit.Application.Abstractions;

public interface IObjectStore
{
    Task<string> WriteAsync(GitObjectType type, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default);
    Task<StoredObject?> ReadAsync(string hash, CancellationToken cancellationToken = default);
}
