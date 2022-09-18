using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.os;

namespace wordslab.manager.console.host
{
    public class StorageInfoCommand : Command<StorageInfoCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine($"Storage info for the host machine: {OS.GetMachineName()}");
            AnsiConsole.WriteLine();

            DisplayStorageInfo();

            return 0;
        }

        internal static void DisplayStorageInfo()
        {
            var drives = Storage.GetDrivesInfo();
            foreach (var drive in drives.Values)
            {
                AnsiConsole.WriteLine($"Drive info: {drive.DrivePath} [{drive.VolumeName}]");
                AnsiConsole.WriteLine($"- partition id : {drive.PartitionId}");
                AnsiConsole.WriteLine($"- drive size   : {drive.TotalSizeMB / 1000f:F1} GB");
                AnsiConsole.WriteLine($"- free space   : {drive.FreeSpaceMB / 1000f:F1} GB");
                AnsiConsole.WriteLine($"- disk id      : {drive.DiskId}");
                AnsiConsole.WriteLine($"- disk model   : {drive.DiskModel}");
                AnsiConsole.WriteLine($"- disk size    : {drive.DiskSizeMB / 1000} GB");
                AnsiConsole.WriteLine($"- is SSD       : {drive.IsSSD}");
                AnsiConsole.WriteLine();
            }
        }

        public class Settings : CommandSettings
        { }
    }

    public class StorageStatusCommand : Command<StorageStatusCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine($"Storage status for the host machine: {OS.GetMachineName()}");
            AnsiConsole.WriteLine();

            DisplayStorageStatus();

            return 0;
        }

        internal static void DisplayStorageStatus()
        {
            var drives = Storage.GetDrivesInfo();
            foreach (var drive in drives.Values)
            {
                AnsiConsole.WriteLine($"Drive info: {drive.DrivePath} [{drive.VolumeName}]");
                AnsiConsole.WriteLine($"- used space   : {(drive.TotalSizeMB - drive.FreeSpaceMB) / 1000f:F1} GB");
                AnsiConsole.WriteLine($"- free space   : {drive.FreeSpaceMB / 1000f:F1} GB");
                AnsiConsole.WriteLine($"- percent used : {drive.PercentUsedSpace} %");
                AnsiConsole.WriteLine();
            }
        }

        public class Settings : CommandSettings
        { }
    }
}
