using GlyphGit.Cli.Runtime;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GlyphGit.Cli.Commands;

public sealed class InitCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var console = ConsoleHost.Console;
        var factory = new CliFactory(console);
        var runtime = factory.Create(forInit: true);

        if (Directory.Exists(runtime.Repository.Paths.MetaPath))
        {
            var confirm = AnsiConsole.Confirm(".glyphgit exists already. Re-initialize?", false);
            if (!confirm)
            {
                return 0;
            }
        }

        var result = await console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Initializing repository...", async _ => await runtime.Init.ExecuteAsync());

        if (!result.IsSuccess)
        {
            console.Write(new Panel(result.Message).BorderColor(Color.Red));
            return (int)result.ExitCode;
        }

        console.MarkupLine($"[green]{Markup.Escape(result.Message)}[/]");
        return 0;
    }
}

