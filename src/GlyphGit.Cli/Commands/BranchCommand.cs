using System.Data;
using System.Runtime.CompilerServices;
using GlyphGit.Cli.Runtime;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GlyphGit.Cli.Commands;

public sealed class BranchCommand : AsyncCommand<BranchCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[name]")]
        public string? Name { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var console = ConsoleHost.Console;
        var runtime = new CliFactory(console).Create(forInit: false);

        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            var result = await runtime.Branch.ListAsync(cancellationToken);
            if (!result.IsSuccess || result.Payload is null)
            {
                console.Write(new Panel(result.Message).BorderColor(Color.Red));
                return (int)result.ExitCode;
            }

            foreach (var branch in result.Payload.OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                var marker = branch.isCurrent ? "*" : " ";
                var hash = string.IsNullOrWhiteSpace(branch.CommitHash) ? "-" : branch.CommitHash[..Math.Min(7, branch.CommitHash.Length)];
                console.MarkupLine($"{marker} {Markup.Escape(branch.Name)} [grey]{hash}[/]");
            }

            return 0;
        }

        var create = await runtime.Branch.CreateAsync(settings.Name, cancellationToken);
        if (!create.IsSuccess)
        {
            console.Write(new Panel(create.Message).BorderColor(Color.Red));
            return (int)create.ExitCode;
        }

        console.MarkupLine($"[green]{Markup.Escape(create.Message)}[/]");
        return 0;
    }
}