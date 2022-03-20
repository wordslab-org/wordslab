using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.os;

namespace wordslab.manager.cli.host
{
    public class ComputeStatusCommand : Command<ComputeStatusCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine($"Compute status for the host machine: {OS.GetMachineName()}");
            
            return 0;
        }

        public class Settings : CommandSettings
        { }
    }
}
