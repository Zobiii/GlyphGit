using Spectre.Console;

namespace GlyphGit.Cli;

internal static class ConsoleHost
{
    public static IAnsiConsole Console { get; set; } = AnsiConsole.Console;
}
