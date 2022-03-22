using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.os;

namespace wordslab.manager.cli.host
{
    public class SystemInfoCommand : Command<SystemInfoCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine($"System information for the host machine: {OS.GetMachineName()}");
            AnsiConsole.WriteLine();

            var cpu = Compute.GetCPUInfo();
            AnsiConsole.WriteLine("CPU info:");
            AnsiConsole.WriteLine($"- manufacturer       : {cpu.Manufacturer}");
            AnsiConsole.WriteLine($"- model name         : {cpu.ModelName}");
            AnsiConsole.WriteLine($"- number of cores    : {cpu.NumberOfCores}");
            AnsiConsole.WriteLine($"- logical processors : {cpu.NumberOfLogicalProcessors}");
            AnsiConsole.WriteLine($"- max clock speed    : {cpu.MaxClockSpeedMhz} Mhz");
            AnsiConsole.WriteLine($"- L3 cache size      : {cpu.L3CacheSizeKB} KB");
            AnsiConsole.WriteLine($"- feature flags      : {cpu.FeatureFlags}");
            AnsiConsole.WriteLine();

            return 0;
        }

        public class Settings : CommandSettings
        { }
    }

    public class SystemUsageCommand : Command<SystemUsageCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine($"System usage for the host machine: {OS.GetMachineName()}");
            AnsiConsole.WriteLine();

            var cpuUsage = Compute.GetPercentCPUTime();
            AnsiConsole.WriteLine($"CPU usage: {cpuUsage} %");
            AnsiConsole.WriteLine();

            return 0;
        }

        public class Settings : CommandSettings
        { }
    }
}
