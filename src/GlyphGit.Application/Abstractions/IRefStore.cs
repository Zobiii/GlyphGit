namespace GlyphGit.Application.Abstractions;

public interface IRefStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<string> ReadHeadReferenceAsync(CancellationToken cancellationToken = default);
    Task<string?> ReadHeadCommitAsync(CancellationToken cancellationToken = default);
    Task WriteHeadCommitAsync(string commitHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListBranchesAsync(CancellationToken cancellationToken = default);
    Task<bool> BranchExistsAsync(string branchName, CancellationToken cancellationToken = default);
    Task<string?> ReadBranchCommitAsync(string branchName, CancellationToken cancellationToken = default);
    Task CreateBranchAsync(string branchName, string? commitHash, CancellationToken cancellationToken = default);
}
