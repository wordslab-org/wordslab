using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.os;

namespace wordslab.manager.console.host
{
    public class ComputeInfoCommand : Command<ComputeInfoCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine($"Compute info for the host machine: {OS.GetMachineName()}");
            AnsiConsole.WriteLine();

            DisplayComputeInfo();

            return 0;
        }

        internal static void DisplayComputeInfo()
        {
            AnsiConsole.WriteLine("CPU info:");
            var cpu = Compute.GetCPUInfo();
            AnsiConsole.WriteLine($"- manufacturer       : {cpu.Manufacturer}");
            AnsiConsole.WriteLine($"- model name         : {cpu.ModelName}");
            AnsiConsole.WriteLine($"- number of cores    : {cpu.NumberOfCores}");
            AnsiConsole.WriteLine($"- logical processors : {cpu.NumberOfLogicalProcessors}");
            AnsiConsole.WriteLine($"- max clock speed    : {cpu.MaxClockSpeedMhz} Mhz");
            AnsiConsole.WriteLine($"- L3 cache size      : {cpu.L3CacheSizeKB} KB");
            AnsiConsole.WriteLine($"- virtualization     : {(Compute.IsCPUVirtualizationAvailable(cpu) ? "available" : "not available")}");
            // AnsiConsole.WriteLine($"- feature flags      : {cpu.FeatureFlags}");
            AnsiConsole.WriteLine();

            AnsiConsole.WriteLine("Memory info:");
            var mem = Memory.GetMemoryInfo();
            AnsiConsole.WriteLine($"- total physical size : {Math.Round(mem.TotalPhysicalMB / 1024f)} GB");
            AnsiConsole.WriteLine();

            var gpus = Compute.GetNvidiaGPUsInfo();
            if (gpus.Count > 0)
            {
                foreach (var gpu in gpus)
                {
                    AnsiConsole.WriteLine($"GPU {gpu.Index} info:");
                    AnsiConsole.WriteLine($"- model name   : {gpu.ModelName}");
                    AnsiConsole.WriteLine($"- memory       : {Math.Round(gpu.MemoryMB / 1024f)} GB");
                    AnsiConsole.WriteLine($"- architecture : {gpu.Architecture}");
                    AnsiConsole.WriteLine();
                }
            }
            else
            {
                AnsiConsole.WriteLine($"GPU info: no NVIDIA GPU found on this system or driver not installed");
                AnsiConsole.WriteLine();
            }
        }

        public class Settings : CommandSettings
        { }
    }

    public class ComputeStatusCommand : Command<ComputeStatusCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine($"Compute status for the host machine: {OS.GetMachineName()}");
            AnsiConsole.WriteLine();

            DisplayComputeStatus();

            return 0;
        }

        internal static void DisplayComputeStatus()
        {
            var cpu = Compute.GetCPUInfo();
            AnsiConsole.WriteLine($"CPU usage: {cpu.ModelName}");
            var cpuUsage = Compute.GetPercentCPUTime();
            var mem = Memory.GetMemoryInfo();
            AnsiConsole.WriteLine($"- cpu load    : {cpuUsage} %");
            AnsiConsole.WriteLine($"- used memory : {mem.UsedPhysicalMB} MB");
            AnsiConsole.WriteLine($"- free memory : {mem.FreePhysicalMB} MB");
            AnsiConsole.WriteLine();

            var gpus = Compute.GetNvidiaGPUsInfo();
            var gpuStats = Compute.GetNvidiaGPUsUsage();
            if (gpuStats.Count > 0)
            {
                foreach (var gpu in gpuStats)
                {
                    AnsiConsole.WriteLine($"GPU {gpu.Index} usage: {gpus[gpu.Index].ModelName}");
                    AnsiConsole.WriteLine($"- gpu load    : {gpu.PercentGPUTime} %");
                    AnsiConsole.WriteLine($"- used memory : {gpu.MemoryUsedMB} MB");
                    AnsiConsole.WriteLine($"- free memory : {gpu.MemoryFreeMB} MB");
                    AnsiConsole.WriteLine();
                }
            }
            else
            {
                AnsiConsole.WriteLine($"GPU usage: no NVIDIA GPU found on this system or driver not installed");
                AnsiConsole.WriteLine();
            }
        }

        public class Settings : CommandSettings
        { }
    }
}
