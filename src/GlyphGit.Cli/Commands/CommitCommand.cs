using GlyphGit.Cli.Runtime;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GlyphGit.Cli.Commands;

public sealed class CommitCommand : AsyncCommand<CommitCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-m|--message <MESSAGE>")]
        public string Message { get; init; } = string.Empty;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        return string.IsNullOrWhiteSpace(settings.Message)
            ? ValidationResult.Error("Missing commit message. Use -m \"...\"")
            : ValidationResult.Success();
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var console = ConsoleHost.Console;
        var runtime = new CliFactory(console).Create(forInit: false);

        var result = await console.Status()
            .Spinner(Spinner.Known.Arc)
            .StartAsync("Creating commit...", async _ => await runtime.Commit.ExecuteAsync(settings.Message));

        if (!result.IsSuccess)
        {
            console.Write(new Panel(result.Message).BorderColor(Color.Red));
            return (int)result.ExitCode;
        }

        console.MarkupLine($"[green]{Markup.Escape(result.Message)}[/]");
        return 0;
    }
}

