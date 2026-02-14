namespace GlyphGit.Domain.Status;

public sealed record StatusItem(StatusArea Area, string Path, FileState State);