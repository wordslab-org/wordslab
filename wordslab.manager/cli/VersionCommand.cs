using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;

namespace wordslab.manager.cli
{    
    public class VersionCommand : Command<VersionCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.MarkupLine($"wordslab manager version: [bold yellow]{ConsoleApp.Version}[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("Documentation: https://www.wordslab.org/");
            AnsiConsole.WriteLine($"Release notes: https://github.com/wordslab-org/wordslab/releases/tag/v{ConsoleApp.Version}");
            AnsiConsole.WriteLine();
            return 0;
        }

        public class Settings : CommandSettings
        {
        }
    }
}
