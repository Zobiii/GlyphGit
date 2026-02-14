namespace GlyphGit.Application.Abstractions;

public interface IRepositoryLock
{
    Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken = default);
}
