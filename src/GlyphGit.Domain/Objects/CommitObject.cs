namespace GlyphGit.Domain.Objects;

public sealed record CommitObject(
    string TreeHash,
    IReadOnlyList<string> Parents,
    string Author,
    string Committer,
    DateTimeOffset TimestampUtc,
    string Message
);