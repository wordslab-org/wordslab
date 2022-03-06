using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;

namespace wordslab.manager.cli
{
    public class VmListCommand : Command<VmListCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine("The vm list command is not yet implemented, sorry.");
            return 0;
        }

        public class Settings : CommandSettings
        {
            [CommandOption("-r|--running")]
            public bool? RunningOnly { get; set; }
        }
    }
}
