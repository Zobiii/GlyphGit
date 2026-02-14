using FluentAssertions;
using GlyphGit.Domain.Objects;

namespace GlyphGit.Tests.Unit;

public sealed class ObjectCodecTests
{
    [Fact]
    public void Commit_Roundtrip_ShouldBeDeterministic()
    {
        var commit = new CommitObject(
            "treehash",
            ["parent1"],
            "alice <alice@local>",
            "alice <alice@local>",
            DateTimeOffset.FromUnixTimeSeconds(1700000000),
            "hello");

        var bytes = ObjectCodec.SerializeCommit(commit);
        var parsed = ObjectCodec.DeserializeCommit(bytes);

        parsed.Should().BeEquivalentTo(commit);
    }

    [Fact]
    public void Tree_Roundtrip_ShouldKeepEntries()
    {
        var tree = new TreeObject([
            new TreeEntry("b.txt", "100644", "h2"),
            new TreeEntry("a.txt", "100644", "h1")
        ]);

        var bytes = ObjectCodec.SerializeTree(tree);
        var parsed = ObjectCodec.DeserializeTree(bytes);

        parsed.Entries.Select(x => x.Path).Should().ContainInOrder("a.txt", "b.txt");
    }
}

