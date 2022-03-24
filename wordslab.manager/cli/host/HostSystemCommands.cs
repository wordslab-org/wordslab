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

            AnsiConsole.WriteLine("Operating System info:");
            AnsiConsole.WriteLine($"- os name           : {OS.GetOSName()}");
            AnsiConsole.WriteLine($"- os version        : {OS.GetOSVersion()}");
            AnsiConsole.WriteLine($"- x64 architecture  : {OS.IsOSArchitectureX64()}");
            AnsiConsole.WriteLine($"- virtualization    : {(OS.IsVirtualizationEnabled()?"enabled":"disabled")}");
            AnsiConsole.WriteLine($"- kernel hypervisor : {(OS.IsKernelBasedHypervisorAvailable()?"available":"not available")}");
            AnsiConsole.WriteLine();
                        
            AnsiConsole.WriteLine("CPU info:");
            var cpu = Compute.GetCPUInfo();
            AnsiConsole.WriteLine($"- manufacturer       : {cpu.Manufacturer}");
            AnsiConsole.WriteLine($"- model name         : {cpu.ModelName}");
            AnsiConsole.WriteLine($"- number of cores    : {cpu.NumberOfCores}");
            AnsiConsole.WriteLine($"- logical processors : {cpu.NumberOfLogicalProcessors}");
            AnsiConsole.WriteLine($"- max clock speed    : {cpu.MaxClockSpeedMhz} Mhz");
            AnsiConsole.WriteLine($"- L3 cache size      : {cpu.L3CacheSizeKB} KB");
            AnsiConsole.WriteLine($"- feature flags      : {cpu.FeatureFlags}");
            AnsiConsole.WriteLine();
                        
            AnsiConsole.WriteLine("Memory info:");
            var mem = Memory.GetMemoryInfo();
            AnsiConsole.WriteLine($"- total physical size : {mem.TotalPhysicalMB} MB");
            AnsiConsole.WriteLine();

            var gpus = Compute.GetNvidiaGPUsInfo();
            if (gpus.Count > 0)
            {
                foreach (var gpu in gpus) 
                {
                    AnsiConsole.WriteLine($"GPU {gpu.Index} info:");
                    AnsiConsole.WriteLine($"- model name   : {gpu.Name}");
                    AnsiConsole.WriteLine($"- memory       : {gpu.MemoryMB} MB");
                    AnsiConsole.WriteLine($"- architecture : {gpu.Architecture}");
                    AnsiConsole.WriteLine();
                }
            }
            else
            {
                AnsiConsole.WriteLine($"GPU info: no NVIDIA GPU found on this system or driver not installed");
                AnsiConsole.WriteLine();
            }

            var disks = Storage.GetDrivesStatus();
            foreach (var disk in disks.Values)
            {
                AnsiConsole.WriteLine($"Disk info: {disk.Name} [{disk.Label}]");
                AnsiConsole.WriteLine($"- total size     : {disk.TotalSizeMB / 1024} GB");
                AnsiConsole.WriteLine($"- root directory : {disk.RootDirectory}");
                if (disk.IsNetworkDrive)
                {
                    AnsiConsole.WriteLine($"- network drive  : {disk.IsNetworkDrive}");
                }
                AnsiConsole.WriteLine();
            }
                        
            AnsiConsole.WriteLine($"Network info: IPv4 addresses");
            var addresses = Network.GetIPAddressesAvailable();
            foreach (var address in addresses.Values)
            {
                AnsiConsole.WriteLine($"- {address.Address}");
                if(address.IsLoopback)
                {
                AnsiConsole.WriteLine($"  . loopback               : {address.IsLoopback}");
                }
                if (address.IsWireless)
                {
                AnsiConsole.WriteLine($"  . wireless               : {address.IsWireless}");
                }
                AnsiConsole.WriteLine($"  . network interface name : {address.NetworkInterfaceName}");
            }
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

            var cpu = Compute.GetCPUInfo();
            AnsiConsole.WriteLine($"CPU usage: {cpu.ModelName}");            
            var cpuUsage = Compute.GetPercentCPUTime();
            var mem = Memory.GetMemoryInfo();
            AnsiConsole.WriteLine($"- cpu load    : {cpuUsage} %");
            AnsiConsole.WriteLine($"- free memory : {mem.FreePhysicalMB} MB");
            AnsiConsole.WriteLine($"- used memory : {mem.UsedPhysicalMB} MB");
            AnsiConsole.WriteLine();

            var gpus = Compute.GetNvidiaGPUsInfo();
            var gpuStats = Compute.GetNvidiaGPUsUsage();
            if (gpuStats.Count > 0)
            {
                foreach (var gpu in gpuStats)
                {
                    AnsiConsole.WriteLine($"GPU {gpu.Index} usage: {gpus[gpu.Index].Name}");
                    AnsiConsole.WriteLine($"- gpu load    : {gpu.PercentGPUTime} %");
                    AnsiConsole.WriteLine($"- free memory : {gpu.MemoryFreeMB} MB");
                    AnsiConsole.WriteLine($"- used memory : {gpu.MemoryUsedMB} MB");
                    AnsiConsole.WriteLine();
                }
            }
            else
            {
                AnsiConsole.WriteLine($"GPU usage: no NVIDIA GPU found on this system or driver not installed");
                AnsiConsole.WriteLine();
            }

            var disks = Storage.GetDrivesStatus();
            foreach (var disk in disks.Values)
            {
                AnsiConsole.WriteLine($"Disk usage: {disk.Name} [{disk.Label}]");
                AnsiConsole.WriteLine($"- free space   : {disk.FreeSpaceMB / 1024} GB");
                AnsiConsole.WriteLine($"- used space   : {(disk.TotalSizeMB - disk.FreeSpaceMB) / 1024} GB");
                AnsiConsole.WriteLine($"- percent used : {disk.PercentUsedSpace} %");
                AnsiConsole.WriteLine();
            }
                        
            AnsiConsole.WriteLine($"Network info: TCP ports in use");
            var portsSets = Network.GetTcpPortsInUse();
            foreach (var ip in portsSets.Keys)
            {
                AnsiConsole.Write($"- {ip} : ");
                foreach(var port in portsSets[ip])
                {
                    AnsiConsole.Write(port);
                    AnsiConsole.Write(' ');
                }
                AnsiConsole.WriteLine();
            }
            AnsiConsole.WriteLine();

            return 0;
        }

        public class Settings : CommandSettings
        { }
    }
}
