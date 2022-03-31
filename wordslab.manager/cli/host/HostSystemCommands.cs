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
            /* TO DO
                 - MacOS : several disks have the same properties
                 - Linux : wireless network interface not detected
                 - WSL no disks !!!

                 - WSL L3 cache size very different from windows L3 cache size
                 - WSL nvidia-smi not available ?
            */

            AnsiConsole.WriteLine($"System information for the host machine: {OS.GetMachineName()}");
            AnsiConsole.WriteLine();

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
            AnsiConsole.WriteLine($"- native hypervisor : {(OS.IsNativeHypervisorAvailable()?"available":"not available")}");
            AnsiConsole.WriteLine();
                        
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
                    AnsiConsole.WriteLine($"- model name   : {gpu.Name}");
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
                AnsiConsole.WriteLine();
            }

            AnsiConsole.WriteLine($"Network info: IPv4 addresses");
            var addresses = Network.GetIPAddressesAvailable();
            foreach (var address in addresses.Values)
            {
                AnsiConsole.WriteLine($"- {address.Address}");                
                AnsiConsole.WriteLine($"  . network interface name : {address.NetworkInterfaceName}");
                if (address.IsLoopback)
                {
                AnsiConsole.WriteLine($"  . loopback               : {address.IsLoopback}");
                }
                if (address.IsWireless)
                {
                AnsiConsole.WriteLine($"  . wireless               : {address.IsWireless}");
                }
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
            AnsiConsole.WriteLine($"- used memory : {mem.UsedPhysicalMB} MB");
            AnsiConsole.WriteLine($"- free memory : {mem.FreePhysicalMB} MB");
            AnsiConsole.WriteLine();

            var gpus = Compute.GetNvidiaGPUsInfo();
            var gpuStats = Compute.GetNvidiaGPUsUsage();
            if (gpuStats.Count > 0)
            {
                foreach (var gpu in gpuStats)
                {
                    AnsiConsole.WriteLine($"GPU {gpu.Index} usage: {gpus[gpu.Index].Name}");
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

            var drives = Storage.GetDrivesInfo();
            foreach (var drive in drives.Values)
            {
                AnsiConsole.WriteLine($"Drive info: {drive.DrivePath} [{drive.VolumeName}]");
                AnsiConsole.WriteLine($"- used space   : {(drive.TotalSizeMB - drive.FreeSpaceMB) / 1000f:F1} GB");
                AnsiConsole.WriteLine($"- free space   : {drive.FreeSpaceMB / 1000f:F1} GB");
                AnsiConsole.WriteLine($"- percent used : {drive.PercentUsedSpace} %");
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
