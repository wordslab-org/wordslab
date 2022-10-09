using Spectre.Console.Cli;
using Spectre.Console;
using System.Diagnostics.CodeAnalysis;

namespace wordslab.manager.console.host
{
    public class InstallCommand : Command<InstallCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine("This is install command");
            AnsiConsole.WriteLine();

            return 0;
        }

        public class Settings : CommandSettings
        { }
    }
}
