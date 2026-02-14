using System.Text;
using GlyphGit.Application.Abstractions;
using GlyphGit.Application.Models;
using GlyphGit.Domain.Objects;
using GlyphGit.Infrastructure.Filesystem;

namespace GlyphGit.Infrastructure.Storage;

public sealed class FileObjectStore : IObjectStore
{
    private readonly string _objectsPath;
    private readonly IHasher _hasher;

    public FileObjectStore(string objectsPath, IHasher hasher)
    {
        _objectsPath = objectsPath;
        _hasher = hasher;
    }

    public async Task<string> WriteAsync(GitObjectType type, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        var typeName = type.ToString().ToLowerInvariant();
        var prefix = Encoding.UTF8.GetBytes($"{typeName} {payload.Length}\0");

        var full = new byte[prefix.Length + payload.Length];
        Buffer.BlockCopy(prefix, 0, full, 0, prefix.Length);
        payload.CopyTo(full.AsMemory(prefix.Length));

        var hash = _hasher.ComputeHash(full);
        var dir = Path.Combine(_objectsPath, hash[..2]);
        var file = Path.Combine(dir, hash[2..]);

        if (!File.Exists(file))
        {
            await FileHelpers.AtomicWriteAsync(file, full, cancellationToken);
        }

        return hash;
    }

    public async Task<StoredObject?> ReadAsync(string hash, CancellationToken cancellationToken = default)
    {
        if (hash.Length < 3)
        {
            return null;
        }

        var file = Path.Combine(_objectsPath, hash[..2], hash[2..]);
        if (!File.Exists(file))
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(file, cancellationToken);
        var nullSplit = Array.IndexOf(bytes, (byte)'\0');
        if (nullSplit >= 0)
        {
            var header = Encoding.UTF8.GetString(bytes, 0, nullSplit);
            var headerParts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (headerParts.Length != 2)
            {
                return null;
            }

            if (!Enum.TryParse<GitObjectType>(headerParts[0], true, out var parsedType))
            {
                return null;
            }

            if (!int.TryParse(headerParts[1], out var declaredSize) || declaredSize < 0)
            {
                return null;
            }

            var payloadOffset = nullSplit + 1;
            var actualSize = bytes.Length - payloadOffset;
            if (actualSize != declaredSize)
            {
                return null;
            }

            var parsedPayload = new byte[actualSize];
            Buffer.BlockCopy(bytes, payloadOffset, parsedPayload, 0, actualSize);
            return new StoredObject(parsedType, parsedPayload);
        }

        var lineSplit = Array.IndexOf(bytes, (byte)'\n');
        if (lineSplit < 0)
        {
            return null;
        }

        var legacyTypeRaw = Encoding.UTF8.GetString(bytes, 0, lineSplit);
        if (!Enum.TryParse<GitObjectType>(legacyTypeRaw, true, out var legacyType))
        {
            return null;
        }

        var legacyPayload = new byte[bytes.Length - lineSplit - 1];
        Buffer.BlockCopy(bytes, lineSplit + 1, legacyPayload, 0, legacyPayload.Length);
        return new StoredObject(legacyType, legacyPayload);
    }
}
