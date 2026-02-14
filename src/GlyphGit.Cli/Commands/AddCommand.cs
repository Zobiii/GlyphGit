using GlyphGit.Cli.Runtime;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GlyphGit.Cli.Commands;

public sealed class AddCommand : AsyncCommand<AddCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[paths]")]
        public string[] Paths { get; init; } = [];
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var console = ConsoleHost.Console;
        var runtime = new CliFactory(console).Create(forInit: false);

        var result = await console.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync("Staging files...", async _ => await runtime.Add.ExecuteAsync(settings.Paths));

        if (!result.IsSuccess)
        {
            console.Write(new Panel(result.Message).BorderColor(Color.Red));
            return (int)result.ExitCode;
        }

        console.MarkupLine($"[green]{Markup.Escape(result.Message)}[/]");
        return 0;
    }
}

