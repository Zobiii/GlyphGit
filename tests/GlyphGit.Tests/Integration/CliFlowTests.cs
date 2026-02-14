using FluentAssertions;
using GlyphGit.Cli;

namespace GlyphGit.Tests.Integration;

public sealed class CliFlowTests
{
    [Fact]
    public async Task InitAddCommitStatusLog_ShouldWork()
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
            (await Program.RunAsync(["branch"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);
            (await Program.RunAsync(["status"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);
            (await Program.RunAsync(["log", "--oneline"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);

            var mainRef = Path.Combine(temp, ".glyphgit", "refs", "heads", "main");
            var featureRef = Path.Combine(temp, ".glyphgit", "refs", "heads", "feature", "x");

            File.Exists(mainRef).Should().BeTrue();
            File.Exists(featureRef).Should().BeTrue();

            var mainHash = (await File.ReadAllTextAsync(mainRef)).Trim();
            var featureHash = (await File.ReadAllTextAsync(featureRef)).Trim();
            mainHash.Should().NotBeNullOrWhiteSpace();
            featureHash.Should().Be(mainHash);
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
