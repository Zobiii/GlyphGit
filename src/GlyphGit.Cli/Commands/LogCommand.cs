using GlyphGit.Cli.Runtime;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GlyphGit.Cli.Commands;

public sealed class LogCommand : AsyncCommand<LogCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--oneline")]
        public bool Oneline { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var console = ConsoleHost.Console;
        var runtime = new CliFactory(console).Create(forInit: false);
        var result = await runtime.Log.ExecuteAsync();

        if (!result.IsSuccess || result.Payload is null)
        {
            console.Write(new Panel(result.Message).BorderColor(Color.Red));
            return (int)result.ExitCode;
        }

        if (settings.Oneline)
        {
            foreach (var item in result.Payload)
            {
                console.MarkupLine($"{item.Hash[..7]} {Markup.Escape(item.Commit.Message)}");
            }

            return 0;
        }

        var table = new Table().RoundedBorder();
        table.AddColumn("Hash");
        table.AddColumn("Author");
        table.AddColumn("Date (UTC)");
        table.AddColumn("Message");

        foreach (var item in result.Payload)
        {
            table.AddRow(
                item.Hash[..7],
                Markup.Escape(item.Commit.Author),
                item.Commit.TimestampUtc.ToString("u"),
                Markup.Escape(item.Commit.Message));
        }

        console.Write(table);
        return 0;
    }
}

