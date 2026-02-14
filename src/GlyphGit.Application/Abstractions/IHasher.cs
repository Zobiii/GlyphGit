namespace GlyphGit.Application.Abstractions;

public interface IHasher
{
    string ComputeHash(ReadOnlySpan<byte> data);
}
