using GlyphGit.Cli.Runtime;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GlyphGit.Cli.Commands;

public sealed class SwitchCommand : AsyncCommand<SwitchCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<name>")]
        public string Name { get; init; } = string.Empty;

        [CommandOption("-y|--yes")]
        public bool Yes { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var console = ConsoleHost.Console;
        var runtime = new CliFactory(console).Create(forInit: false);

        if (!settings.Yes && console.Profile.Capabilities.Interactive)
        {
            if (!AnsiConsole.Confirm($"Switch to branch '{settings.Name}'? This may overwrite worktree files.", true))
            {
                return 0;
            }
        }

        var result = await runtime.Switch.ExecuteAsync(settings.Name, cancellationToken);
        if (!result.IsSuccess)
        {
            console.Write(new Panel(result.Message).BorderColor(Color.Red));
            return (int)result.ExitCode;
        }

        console.MarkupLine($"[green]{Markup.Escape(result.Message)}[/]");
        return 0;
    }
}
