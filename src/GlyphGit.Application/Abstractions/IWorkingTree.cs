namespace GlyphGit.Application.Abstractions;

public interface IWorkingTree
{
    Task<IReadOnlyList<string>> EnumerateFilesAsync(IReadOnlyList<string>? inputPaths = null, CancellationToken cancellationToken = default);
    Task<byte[]> ReadFileAsync(string relativePath, CancellationToken cancellationToken = default);
    Task WriteFileAsync(string relativePath, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default);
    Task<DateTimeOffset?> GetLastWriteTimeUtcAsync(string relativePath, CancellationToken cancellationToken = default);
    Task<long?> GetFileSizeAsync(string relativePath, CancellationToken cancellationToken = default);
}
