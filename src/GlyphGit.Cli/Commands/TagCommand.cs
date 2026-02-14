using GlyphGit.Cli.Runtime;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GlyphGit.Cli.Commands;

public sealed class TagCommand : AsyncCommand<TagCommand.Settings>
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
            var list = await runtime.Tag.ListAsync(cancellationToken);
            if (!list.IsSuccess || list.Payload is null)
            {
                console.Write(new Panel(list.Message).BorderColor(Color.Red));
                return (int)list.ExitCode;
            }

            foreach (var tag in list.Payload.OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                var hash = string.IsNullOrWhiteSpace(tag.CommitHash) ? "-" : tag.CommitHash[..Math.Min(7, tag.CommitHash.Length)];
                console.MarkupLine($"{Markup.Escape(tag.Name)} [grey]{hash}[/]");
            }

            return 0;
        }

        var create = await runtime.Tag.CreateAsync(settings.Name, cancellationToken);
        if (!create.IsSuccess)
        {
            console.Write(new Panel(create.Message).BorderColor(Color.Red));
            return (int)create.ExitCode;
        }

        console.MarkupLine($"[green]{Markup.Escape(create.Message)}[/]");
        return 0;
    }
}
