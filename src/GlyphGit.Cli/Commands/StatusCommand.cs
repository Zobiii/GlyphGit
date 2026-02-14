using GlyphGit.Cli.Runtime;
using GlyphGit.Domain.Status;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GlyphGit.Cli.Commands;

public sealed class StatusCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var console = ConsoleHost.Console;
        var runtime = new CliFactory(console).Create(forInit: false);

        var result = await console.Status()
            .Spinner(Spinner.Known.BouncingBar)
            .StartAsync("Scanning repository state...", async _ => await runtime.Status.ExecuteAsync());

        if (!result.IsSuccess || result.Payload is null)
        {
            console.Write(new Panel(result.Message).BorderColor(Color.Red));
            return (int)result.ExitCode;
        }

        var table = new Table().RoundedBorder();
        table.AddColumn("Area");
        table.AddColumn("State");
        table.AddColumn("Path");

        foreach (var item in result.Payload.OrderBy(x => x.Area).ThenBy(x => x.Path, StringComparer.Ordinal))
        {
            var area = item.Area.ToString();
            var state = item.State switch
            {
                FileState.Added => "[green]added[/]",
                FileState.Modified => "[yellow]modified[/]",
                FileState.Deleted => "[red]deleted[/]",
                _ => "?"
            };

            table.AddRow(area, state, Markup.Escape(item.Path));
        }

        if (result.Payload.Count == 0)
        {
            console.MarkupLine("[green]Working tree clean.[/]");
            return 0;
        }

        console.Write(table);
        return 0;
    }
}

