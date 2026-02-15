using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
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

    [Fact]
    public async Task Switch_NonExistingBranch_ShouldReturnRepositoryError()
    {
        await WithTempRepoAsync(async _ =>
        {
            (await Program.RunAsync(["init"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);

            var exit = await Program.RunAsync(["switch", "does-not-exist", "--yes"], Spectre.Console.AnsiConsole.Console);
            exit.Should().Be(3);
        });
    }

    [Fact]
    public async Task Branch_DuplicateName_ShouldReturnConflict()
    {
        await WithTempRepoAsync(async _ =>
        {
            (await Program.RunAsync(["init"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);

            (await Program.RunAsync(["branch", "feature/x"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);
            (await Program.RunAsync(["branch", "feature/x"], Spectre.Console.AnsiConsole.Console)).Should().Be(4);
        });
    }

    [Fact]
    public async Task Tag_WithoutCommit_ShouldReturnConflict()
    {
        await WithTempRepoAsync(async _ =>
        {
            (await Program.RunAsync(["init"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);

            var exit = await Program.RunAsync(["tag", "v0.0.1"], Spectre.Console.AnsiConsole.Console);
            exit.Should().Be(4);
        });
    }

    [Fact]
    public async Task Tag_DuplicateName_ShouldReturnConflict()
    {
        await WithTempRepoAsync(async temp =>
        {
            (await Program.RunAsync(["init"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);

            await File.WriteAllTextAsync(Path.Combine(temp, "a.txt"), "hello");
            (await Program.RunAsync(["add", "a.txt"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);
            (await Program.RunAsync(["commit", "-m", "first"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);

            (await Program.RunAsync(["tag", "v1.0.0"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);
            (await Program.RunAsync(["tag", "v1.0.0"], Spectre.Console.AnsiConsole.Console)).Should().Be(4);
        });
    }

    [Fact]
    public async Task Restore_Staged_ShouldInvertIndexButKeepWorktreeChange()
    {
        await WithTempRepoAsync(async temp =>
        {
            (await Program.RunAsync(["init"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);

            var file = Path.Combine(temp, "a.txt");
            await File.WriteAllTextAsync(file, "hello");
            (await Program.RunAsync(["add", "a.txt"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);
            (await Program.RunAsync(["commit", "-m", "first"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);


            var originalIndexHash = await ReadIndexBlobHashAsync(temp, "a.txt");
            originalIndexHash.Should().NotBeNullOrWhiteSpace();

            await File.WriteAllTextAsync(file, "changed");
            (await Program.RunAsync(["add", "a.txt"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);


            var changedIndexHash = await ReadIndexBlobHashAsync(temp, "a.txt");
            changedIndexHash.Should().NotBe(originalIndexHash);

            (await Program.RunAsync(["restore", "--staged", "a.txt"], Spectre.Console.AnsiConsole.Console)).Should().Be(0);

            var revertedIndexHash = await ReadIndexBlobHashAsync(temp, "a.txt");
            revertedIndexHash.Should().Be(originalIndexHash);

            var content = await File.ReadAllTextAsync(file);
            content.Should().Be("changed");
        });
    }

    private static async Task WithTempRepoAsync(Func<string, Task> action)
    {
        var temp = Path.Combine(Path.GetTempPath(), "glyphgit-test-" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(temp);

        var old = Environment.CurrentDirectory;
        Environment.CurrentDirectory = temp;

        try
        {
            await action(temp);
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

    private static async Task<string?> ReadIndexBlobHashAsync(string repoRoot, string relativePath)
    {
        var indexPath = Path.Combine(repoRoot, ".glyphgit", "index");
        if (!File.Exists(indexPath))
        {
            return null;
        }

        var lines = await File.ReadAllLinesAsync(indexPath);
        var line = lines.FirstOrDefault(x => x.StartsWith(relativePath + "\t", StringComparison.Ordinal));
        if (line is null)
        {
            return null;
        }

        var parts = line.Split("\t");
        return parts.Length >= 2 ? parts[1] : null;
    }
}
