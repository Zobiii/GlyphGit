using System.Text;

namespace GlyphGit.Infrastructure.Filesystem;

public static class FileHelpers
{
    public static async Task AtomicWriteAsync(string targetPath, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        var tempPath = $"{targetPath}.tmp-{Guid.NewGuid():N}";

        await using (var fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await fs.WriteAsync(content, cancellationToken);
            await fs.FlushAsync(cancellationToken);
        }

        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        File.Move(tempPath, targetPath);
    }

    public static async Task<string?> ReadTextIfExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        return await File.ReadAllTextAsync(path, new UTF8Encoding(false), cancellationToken);
    }
}
