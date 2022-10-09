using Spectre.Console.Cli;
using Spectre.Console;
using System.Diagnostics.CodeAnalysis;

namespace wordslab.manager.console.host
{
    public class ConfigInfoCommand : Command<ConfigInfoCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine("This is config info command");
            AnsiConsole.WriteLine();

            return 0;
        }

        public class Settings : CommandSettings
        { }
    }

    public class ConfigUpdateCommand : Command<ConfigUpdateCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine("This is config update command");
            AnsiConsole.WriteLine();

            return 0;
        }

        public class Settings : CommandSettings
        { }
    }
}
