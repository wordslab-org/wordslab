using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;

namespace wordslab.manager.console
{    
    public class VersionCommand : CommandWithUI<VersionCommand.Settings>
    {
        public VersionCommand(ICommandsUI ui) : base(ui) 
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            UI.WriteLine($"wordslab manager version: {ConsoleApp.Version}");
            UI.WriteLine();
            UI.WriteLine("Documentation: https://www.wordslab.org/");
            UI.WriteLine($"Release notes: https://github.com/wordslab-org/wordslab/releases/tag/v{ConsoleApp.Version}");
            UI.WriteLine();
            return 0;
        }

        public class Settings : CommandSettings
        {
        }
    }
}
