using System.Globalization;
using System.Text;

namespace GlyphGit.Domain.Objects;

public static class ObjectCodec
{
    public static byte[] SerializeTree(TreeObject tree)
    {
        var sb = new StringBuilder();
        foreach (var e in tree.Entries.OrderBy(x => x.Path, StringComparer.Ordinal))
        {
            sb.Append(e.Mode).Append('\t').Append(e.Path).Append('\t').Append(e.Hash).Append('\n');
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public static TreeObject DeserializeTree(ReadOnlySpan<byte> payload)
    {
        var text = Encoding.UTF8.GetString(payload);
        var entries = new List<TreeEntry>();

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t');
            if (parts.Length != 3)
            {
                continue;
            }

            entries.Add(new TreeEntry(parts[1], parts[0], parts[2]));
        }

        return new TreeObject(entries.OrderBy(x => x.Path, StringComparer.Ordinal).ToArray());
    }

    public static byte[] SerializeCommit(CommitObject commit)
    {
        var sb = new StringBuilder();
        sb.Append("tree ").Append(commit.TreeHash).Append('\n');
        foreach (var parent in commit.Parents)
        {
            sb.Append("parent ").Append(parent).Append('\n');
        }

        sb.Append("author ").Append(commit.Author).Append('\n');
        sb.Append("committer ").Append(commit.Committer).Append('\n');
        sb.Append("timestamp ").Append(commit.TimestampUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)).Append('\n');
        sb.Append('\n');
        sb.Append(commit.Message.Replace("\r\n", "\n", StringComparison.Ordinal));

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public static CommitObject DeserializeCommit(ReadOnlySpan<byte> payload)
    {
        var text = Encoding.UTF8.GetString(payload);
        var splitIndex = text.IndexOf("\n\n", StringComparison.Ordinal);
        var header = splitIndex < 0 ? text : text[..splitIndex];
        var message = splitIndex < 0 ? string.Empty : text[(splitIndex + 2)..];

        var tree = string.Empty;
        var parents = new List<string>();
        var author = string.Empty;
        var committer = string.Empty;
        var timestamp = DateTimeOffset.UnixEpoch;

        foreach (var line in header.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("tree ", StringComparison.Ordinal))
            {
                tree = line["tree ".Length..];
            }
            else if (line.StartsWith("parent ", StringComparison.Ordinal))
            {
                parents.Add(line["parent ".Length..]);
            }
            else if (line.StartsWith("author ", StringComparison.Ordinal))
            {
                author = line["author ".Length..];
            }
            else if (line.StartsWith("committer ", StringComparison.Ordinal))
            {
                committer = line["committer ".Length..];
            }
            else if (line.StartsWith("timestamp ", StringComparison.Ordinal))
            {
                var ts = long.Parse(line["timestamp ".Length..], CultureInfo.InvariantCulture);
                timestamp = DateTimeOffset.FromUnixTimeSeconds(ts);
            }
        }

        return new CommitObject(tree, parents, author, committer, timestamp, message);
    }
}
