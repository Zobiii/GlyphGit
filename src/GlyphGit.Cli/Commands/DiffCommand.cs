using GlyphGit.Cli.Runtime;
using GlyphGit.Domain.Diff;
using GlyphGit.Domain.Status;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GlyphGit.Cli.Commands;

public sealed class DiffCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var console = ConsoleHost.Console;
        var runtime = new CliFactory(console).Create(forInit: false);
        var result = await runtime.Diff.ExecuteAsync();

        if (!result.IsSuccess || result.Payload is null)
        {
            console.Write(new Panel(result.Message).BorderColor(Color.Red));
            return (int)result.ExitCode;
        }

        var table = new Table().RoundedBorder();
        table.AddColumn("Scope");
        table.AddColumn("State");
        table.AddColumn("Path");
        table.AddColumn("Old");
        table.AddColumn("New");

        foreach (var d in result.Payload.OrderBy(x => x.Scope).ThenBy(x => x.Path, StringComparer.Ordinal))
        {
            var scope = d.Scope == DiffScope.IndexToHead ? "index<->HEAD" : "worktree<->index";
            var state = d.State switch
            {
                FileState.Added => "[green]added[/]",
                FileState.Modified => "[yellow]modified[/]",
                FileState.Deleted => "[red]deleted[/]",
                _ => "?"
            };

            table.AddRow(scope, state, Markup.Escape(d.Path), d.OldHash?[..Math.Min(7, d.OldHash.Length)] ?? "-", d.NewHash?[..Math.Min(7, d.NewHash.Length)] ?? "-");
        }

        if (result.Payload.Count == 0)
        {
            console.MarkupLine("[green]No differences.[/]");
            return 0;
        }

        console.Write(table);
        return 0;
    }
}

