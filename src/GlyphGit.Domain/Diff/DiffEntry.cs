using GlyphGit.Domain.Status;

namespace GlyphGit.Domain.Diff;

public sealed record DiffEntry(
    DiffScope Scope,
    string Path,
    FileState State,
    string? OldHash,
    string? NewHash
);