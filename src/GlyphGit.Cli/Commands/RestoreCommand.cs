using GlyphGit.Cli.Runtime;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GlyphGit.Cli.Commands;

public sealed class RestoreCommand : AsyncCommand<RestoreCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--staged")]
        public bool Staged { get; init; }

        [CommandOption("--worktree")]
        public bool Worktree { get; init; }

        [CommandArgument(0, "[paths]")]
        public string[] Paths { get; init; } = [];

        [CommandOption("-y|--yes")]
        public bool Yes { get; init; }
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        return (!settings.Staged && !settings.Worktree)
            ? ValidationResult.Error("Use --staged and/or --worktree.")
            : ValidationResult.Success();
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var console = ConsoleHost.Console;
        var runtime = new CliFactory(console).Create(forInit: false);

        if (!settings.Yes && console.Profile.Capabilities.Interactive)
        {
            if (!AnsiConsole.Confirm("Restore selected paths?", true))
            {
                return 0;
            }
        }

        var result = await runtime.Restore.ExecuteAsync(settings.Staged, settings.Worktree, settings.Paths);

        if (!result.IsSuccess)
        {
            console.Write(new Panel(result.Message).BorderColor(Color.Red));
            return (int)result.ExitCode;
        }

        console.MarkupLine($"[green]{Markup.Escape(result.Message)}[/]");
        return 0;
    }
}

