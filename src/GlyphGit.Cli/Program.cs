using GlyphGit.Cli.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GlyphGit.Cli;

public static class Program
{
    public static Task<int> Main(string[] args) => RunAsync(args, AnsiConsole.Console);

    public static Task<int> RunAsync(string[] args, IAnsiConsole console)
    {
        ConsoleHost.Console = console;

        if (args.Length > 0 && !args.Contains("--help", StringComparer.Ordinal))
        {
            console.Write(new FigletText("GlyphGit").Color(Color.CadetBlue));
        }

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("glyphgit");
            config.AddCommand<InitCommand>("init");
            config.AddCommand<AddCommand>("add");
            config.AddCommand<CommitCommand>("commit");
            config.AddCommand<StatusCommand>("status");
            config.AddCommand<LogCommand>("log");
            config.AddCommand<DiffCommand>("diff");
            config.AddCommand<RestoreCommand>("restore");
            config.AddCommand<BranchCommand>("branch");
            config.AddCommand<SwitchCommand>("switch");
        });

        return app.RunAsync(args);
    }
}
