using System.Text;
using FluentAssertions;
using GlyphGit.Domain.Objects;
using GlyphGit.Infrastructure.Hashing;
using GlyphGit.Infrastructure.Storage;

namespace GlyphGit.Tests.Unit;

public sealed class FileObjectStoreTests
{
    [Fact]
    public async Task WriteRead_NewFormat_ShouldRoundtrip()
    {
        var temp = CreateTempDir();
        try
        {
            var hasher = new Sha1Hasher();
            var store = new FileObjectStore(Path.Combine(temp, "objects"), hasher);

            var payload = Encoding.UTF8.GetBytes("hello-new");
            var hash = await store.WriteAsync(GitObjectType.Blob, payload);

            var read = await store.ReadAsync(hash);
            read.Should().NotBeNull();
            read!.Type.Should().Be(GitObjectType.Blob);
            read.Payload.Should().Equal(payload);
        }
        finally
        {
            Directory.Delete(temp, true);
        }
    }

    [Fact]
    public async Task Read_LegacyFormat_ShouldStillWork()
    {
        var temp = CreateTempDir();
        try
        {
            var hasher = new Sha1Hasher();
            var objectPaths = Path.Combine(temp, "objects");
            var store = new FileObjectStore(objectPaths, hasher);

            var raw = Encoding.UTF8.GetBytes("blob\nhello-legacy");
            var hash = hasher.ComputeHash(raw);

            var objectFile = Path.Combine(objectPaths, hash[..2], hash[2..]);
            Directory.CreateDirectory(Path.GetDirectoryName(objectFile)!);
            await File.WriteAllBytesAsync(objectFile, raw);

            var read = await store.ReadAsync(hash);
            read.Should().NotBeNull();
            read!.Type.Should().Be(GitObjectType.Blob);
            Encoding.UTF8.GetString(read.Payload).Should().Be("hello-legacy");
        }
        finally
        {
            Directory.Delete(temp, true);
        }
    }

    [Fact]
    public async Task Read_CorruptHeader_ShouldReturnNull()
    {
        var temp = CreateTempDir();
        try
        {
            var hasher = new Sha1Hasher();
            var objectPaths = Path.Combine(temp, "objects");
            var store = new FileObjectStore(objectPaths, hasher);

            var raw = Encoding.UTF8.GetBytes("blob nope\0abc");
            var hash = hasher.ComputeHash(raw);

            var objectFile = Path.Combine(objectPaths, hash[..2], hash[2..]);
            Directory.CreateDirectory(Path.GetDirectoryName(objectFile)!);
            await File.WriteAllBytesAsync(objectFile, raw);

            var read = await store.ReadAsync(hash);
            read.Should().BeNull();
        }
        finally
        {
            Directory.Delete(temp, true);
        }
    }

    public static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "glyphgit-objectstore-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}