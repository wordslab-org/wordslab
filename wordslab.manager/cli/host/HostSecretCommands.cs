using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.os;

namespace wordslab.manager.cli.host
{
    public class SecretListCommand : Command<SecretListCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine("ERROR: host secret list command not yet implemented");
            return -1;
        }

        public class Settings : CommandSettings
        { }
    }
}
