using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.os;

namespace wordslab.manager.console.host
{
    public class SystemInfoCommand : Command<SystemInfoCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            /* TO DO
                 - MacOS : several disks have the same properties
                 - Linux : wireless network interface not detected
                 - WSL no disks !!!

                 - WSL L3 cache size very different from windows L3 cache size
            */

            AnsiConsole.WriteLine($"System information for the host machine: {OS.GetMachineName()}");
            AnsiConsole.WriteLine();

            OsInfoCommand.DisplayOSInfo();
            ComputeInfoCommand.DisplayComputeInfo();
            StorageInfoCommand.DisplayStorageInfo();
            NetworkInfoCommand.DisplayNetworkInfo();

            return 0;
        }        

        public class Settings : CommandSettings
        { }
    }

    public class SystemStatusCommand : Command<SystemStatusCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine($"System status for the host machine: {OS.GetMachineName()}");
            AnsiConsole.WriteLine();

            ComputeStatusCommand.DisplayComputeStatus();
            StorageStatusCommand.DisplayStorageStatus();
            NetworkStatusCommand.DisplayNetworkStatus();

            return 0;
        }        

        public class Settings : CommandSettings
        { }
    }
}
