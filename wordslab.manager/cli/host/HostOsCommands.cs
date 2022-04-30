using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.os;

namespace wordslab.manager.cli.host
{
    public class OsInfoCommand : Command<OsInfoCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine($"Operating system info for the host machine: {OS.GetMachineName()}");
            AnsiConsole.WriteLine();

            DisplayOSInfo();

            return 0;
        }

        internal static void DisplayOSInfo()
        {
            AnsiConsole.WriteLine("Operating System info:");
            AnsiConsole.WriteLine($"- os name           : {OS.GetOSName()}");
            AnsiConsole.WriteLine($"- os version        : {OS.GetOSVersion()}");
            if (OS.IsLinux)
            {
                var distrib = OS.GetLinuxDistributionInfo();
                if (!String.IsNullOrEmpty(distrib.Name))
                {
                    AnsiConsole.WriteLine($"- distribution name : {distrib.Name}");
                }
                if (distrib.Version > new Version())
                {
                    AnsiConsole.WriteLine($"- distrib. version  : {distrib.Version}");
                }

            }
            AnsiConsole.WriteLine($"- x64 architecture  : {OS.IsOSArchitectureX64()}");
            AnsiConsole.WriteLine($"- native hypervisor : {(OS.IsNativeHypervisorAvailable() ? "available" : "not available")}");
            AnsiConsole.WriteLine();
        }

        public class Settings : CommandSettings
        { }
    }
}
