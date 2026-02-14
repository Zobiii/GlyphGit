using GlyphGit.Application.Abstractions;
using GlyphGit.Infrastructure.Filesystem;
using GlyphGit.Infrastructure.Hashing;
using GlyphGit.Infrastructure.Locking;
using GlyphGit.Infrastructure.Storage;

namespace GlyphGit.Infrastructure;

public sealed class RepositoryRuntime
{
    public required RepositoryPaths Paths { get; init; }
    public required IHasher Hasher { get; init; }
    public required IObjectStore ObjectStore { get; init; }
    public required IRefStore RefStore { get; init; }
    public required IIndexStore IndexStore { get; init; }
    public required IWorkingTree WorkingTree { get; init; }
    public required IRepositoryLock RepositoryLock { get; init; }

    public static RepositoryRuntime OpenExisting(string workingDirectory)
    {
        var paths = RepositoryPaths.Discover(workingDirectory);
        return Create(paths);
    }

    public static RepositoryRuntime ForInit(string workingDirectory)
    {
        var paths = RepositoryPaths.ForInit(workingDirectory);
        return Create(paths);
    }

    private static RepositoryRuntime Create(RepositoryPaths paths)
    {
        var hasher = new Sha1Hasher();
        return new RepositoryRuntime
        {
            Paths = paths,
            Hasher = hasher,
            ObjectStore = new FileObjectStore(paths.ObjectsPath, hasher),
            RefStore = new FileRefStore(paths),
            IndexStore = new FileIndexStore(paths.IndexPath),
            WorkingTree = new FileWorkingTree(paths.RootPath),
            RepositoryLock = new FileRepositoryLock(paths.LockPath)
        };
    }
}
