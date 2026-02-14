using System.Security.Cryptography;
using System.Text;
using GlyphGit.Application.Abstractions;

namespace GlyphGit.Infrastructure.Hashing;

public sealed class Sha1Hasher : IHasher
{
    public string ComputeHash(ReadOnlySpan<byte> data)
    {
        var hash = SHA1.HashData(data);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}
