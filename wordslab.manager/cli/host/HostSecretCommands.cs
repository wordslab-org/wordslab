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
            AnsiConsole.WriteLine($"List of secrets stored on the host machine: {OS.GetMachineName()}");

            return 0;
        }

        public class Settings : CommandSettings
        { }
    }
}
