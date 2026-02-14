namespace GlyphGit.Domain.Index;

public sealed record IndexEntry(
    string Path,
    string BlobHash,
    string Mode,
    long FileSize,
    DateTimeOffset LastWriteUtc
);