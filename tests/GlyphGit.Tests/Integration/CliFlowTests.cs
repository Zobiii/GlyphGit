using FluentAssertions;
using GlyphGit.Cli;

namespace GlyphGit.Tests.Integration;

public sealed class CliFlowTests
{
    [Fact]
    public async Task InitAddCommitBranchSwitchTagCheckout_ShouldWork()
    {
        var temp = Path.Combine(Path.GetTempPath(), "glyphgit-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);

        var old = Environment.CurrentDirectory;
        Environment.CurrentDirectory = temp;

        try
        {
            (await Program.RunAsync(["init"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);

            await File.WriteAllTextAsync(Path.Combine(temp, "a.txt"), "hello");
            (await Program.RunAsync(["add", "a.txt"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);
            (await Program.RunAsync(["commit", "-m", "first"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);

            (await Program.RunAsync(["branch", "feature/x"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);
            (await Program.RunAsync(["switch", "feature/x", "--yes"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);
            (await Program.RunAsync(["tag", "v0.1.0"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);
            (await Program.RunAsync(["checkout", "main", "--yes"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);

            var headMain = await File.ReadAllTextAsync(Path.Combine(temp, ".glyphgit", "HEAD"));
            headMain.Trim().Should().Be("ref: refs/heads/main");

            await File.WriteAllTextAsync(Path.Combine(temp, "a.txt"), "changed");
            (await Program.RunAsync(["checkout", "a.txt"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);
            var content = await File.ReadAllTextAsync(Path.Combine(temp, "a.txt"));
            content.Should().NotBe("changed");

            var tagRef = Path.Combine(temp, ".glyphgit", "refs", "tags", "v0.1.0");
            File.Exists(tagRef).Should().BeTrue();
        }
        finally
        {
            Environment.CurrentDirectory = old;
            if (Directory.Exists(temp))
            {
                Directory.Delete(temp, true);
            }
        }
    }
}
