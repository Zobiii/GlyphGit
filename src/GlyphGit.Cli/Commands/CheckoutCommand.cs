using GlyphGit.Cli.Runtime;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GlyphGit.Cli.Commands;

public sealed class CheckoutCommand : AsyncCommand<CheckoutCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<target-or-separator>")]
        public string TargetOrSeparator { get; init; } = string.Empty;

        [CommandArgument(1, "[value]")]
        public string? Value { get; init; }

        [CommandArgument(2, "[more]")]
        public string[] More { get; init; } = [];

        [CommandOption("-y|--yes")]
        public bool Yes { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var console = ConsoleHost.Console;
        var runtime = new CliFactory(console).Create(forInit: false);

        if (settings.TargetOrSeparator == "--")
        {
            var paths = new List<string>();
            if (!string.IsNullOrWhiteSpace(settings.Value))
            {
                paths.Add(settings.Value);
            }

            paths.AddRange(settings.More.Where(x => !string.IsNullOrWhiteSpace(x)));

            if (paths.Count == 0)
            {
                console.Write(new Panel("No paths provided after '--'.").BorderColor(Color.Red));
                return 2;
            }

            var restore = await runtime.Restore.ExecuteAsync(
                staged: false,
                worktree: true,
                paths: paths,
                cancellationToken: cancellationToken);

            if (!restore.IsSuccess)
            {
                console.Write(new Panel(restore.Message).BorderColor(Color.Red));
                return (int)restore.ExitCode;
            }

            console.MarkupLine($"[green]{Markup.Escape(restore.Message)}[/]");
            return 0;
        }

        var branchName = settings.TargetOrSeparator;
        if (string.IsNullOrWhiteSpace(settings.Value) && settings.More.Length == 0)
        {
            var fileExists = await runtime.Repository.WorkingTree.ExistsAsync(branchName, cancellationToken);
            if (fileExists)
            {
                var restoreSingle = await runtime.Restore.ExecuteAsync(
                    staged: false,
                    worktree: true,
                    paths: [branchName],
                    cancellationToken: cancellationToken);

                if (!restoreSingle.IsSuccess)
                {
                    console.Write(new Panel(restoreSingle.Message).BorderColor(Color.Red));
                    return (int)restoreSingle.ExitCode;
                }

                console.MarkupLine($"[green]{Markup.Escape(restoreSingle.Message)}[/]");
                return 0;
            }
        }

        if (!settings.Yes && console.Profile.Capabilities.Interactive)
        {
            if (!AnsiConsole.Confirm($"Checkout branch '{branchName}'? This may overwrite worktree files.", true))
            {
                return 0;
            }
        }

        var result = await runtime.Switch.ExecuteAsync(branchName, cancellationToken);
        if (!result.IsSuccess)
        {
            console.Write(new Panel(result.Message).BorderColor(Color.Red));
            return (int)result.ExitCode;
        }

        console.MarkupLine($"[green]{Markup.Escape(result.Message)}[/]");
        return 0;
    }
}
