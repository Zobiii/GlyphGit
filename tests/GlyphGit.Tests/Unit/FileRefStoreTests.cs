using FluentAssertions;
using GlyphGit.Infrastructure;
using GlyphGit.Infrastructure.Storage;

namespace GlyphGit.Tests.Unit;

public sealed class FileRefStoreTests
{
    [Fact]
    public async Task CreateBranch_InvalidName_ShouldThrow()
    {
        var temp = CreateTempDir();
        try
        {
            var store = new FileRefStore(RepositoryPaths.ForInit(temp));
            await store.InitializeAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.CreateBranchAsync("/bad", null));
            await Assert.ThrowsAsync<InvalidOperationException>(() => store.CreateBranchAsync("bad/", null));
            await Assert.ThrowsAsync<InvalidOperationException>(() => store.CreateBranchAsync("a..b", null));
        }
        finally
        {
            Directory.Delete(temp, true);
        }
    }

    [Fact]
    public async Task ListBranches_ShouldBeDeterministicallySorted()
    {
        var temp = CreateTempDir();
        try
        {
            var store = new FileRefStore(RepositoryPaths.ForInit(temp));
            await store.InitializeAsync();

            await store.CreateBranchAsync("zeta", null);
            await store.CreateBranchAsync("alpha", null);
            await store.CreateBranchAsync("feature/x", null);

            var branches = await store.ListBranchesAsync();

            branches.Should().Equal(branches.OrderBy(x => x, StringComparer.Ordinal));
            branches.Should().Contain("main");
            branches.Should().Contain("alpha");
            branches.Should().Contain("feature/x");
            branches.Should().Contain("zeta");
        }
        finally
        {
            Directory.Delete(temp, true);
        }
    }

    [Fact]
    public async Task ListTags_ShouldBeDeterministicallySorted()
    {
        var temp = CreateTempDir();
        try
        {
            var store = new FileRefStore(RepositoryPaths.ForInit(temp));
            await store.InitializeAsync();

            await store.CreateTagAsync("v2.0.0", "abc123");
            await store.CreateTagAsync("v1.0.0", "abc123");
            await store.CreateTagAsync("release/candidate", "abc123");

            var tags = await store.ListTagsAsync();

            tags.Should().Equal(tags.OrderBy(x => x, StringComparer.Ordinal));
            tags.Should().Contain("v1.0.0");
            tags.Should().Contain("v2.0.0");
            tags.Should().Contain("release/candidate");
        }
        finally
        {
            Directory.Delete(temp, true);
        }
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "glyphgit-refstore-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}