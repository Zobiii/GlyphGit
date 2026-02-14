using GlyphGit.Application.Abstractions;

namespace GlyphGit.Infrastructure.Locking;

public sealed class FileRepositoryLock : IRepositoryLock
{
    private readonly string _lockFilePath;

    public FileRepositoryLock(string lockFilePath)
    {
        _lockFilePath = lockFilePath;
    }

    public async Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_lockFilePath)!);

        var fs = new FileStream(_lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        await fs.FlushAsync(cancellationToken);

        return new AsyncDisposable(fs);
    }

    private sealed class AsyncDisposable : IAsyncDisposable
    {
        private readonly FileStream _stream;

        public AsyncDisposable(FileStream stream)
        {
            _stream = stream;
        }

        public ValueTask DisposeAsync()
        {
            _stream.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
