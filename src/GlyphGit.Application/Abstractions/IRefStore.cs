namespace GlyphGit.Application.Abstractions;

public interface IRefStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<string> ReadHeadReferenceAsync(CancellationToken cancellationToken = default);
    Task<string?> ReadHeadCommitAsync(CancellationToken cancellationToken = default);
    Task WriteHeadCommitAsync(string commitHash, CancellationToken cancellationToken = default);
}
