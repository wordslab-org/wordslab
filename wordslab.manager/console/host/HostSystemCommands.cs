using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.os;

namespace wordslab.manager.console.host
{
    public class SystemInfoCommand : CommandWithUI<SystemInfoCommand.Settings>
    {
        public SystemInfoCommand(ICommandsUI ui) : base(ui) 
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            /* TO DO
                 - MacOS : several disks have the same properties
                 - Linux : wireless network interface not detected
                 - WSL no disks !!!

                 - WSL L3 cache size very different from windows L3 cache size
            */

            UI.WriteLine($"System information for the host machine: {OS.GetMachineName()}");
            UI.WriteLine();

            DisplayOSInfo(UI);
            DisplayComputeInfo(UI);
            DisplayStorageInfo(UI);
            DisplayNetworkInfo(UI);

            return 0;
        }        

        public class Settings : CommandSettings
        { }

        internal static void DisplayOSInfo(ICommandsUI ui)
        {
            ui.WriteLine("Operating System info:");
            ui.WriteLine($"- os name           : {OS.GetOSName()}");
            ui.WriteLine($"- os version        : {OS.GetOSVersion()}");
            if (OS.IsLinux)
            {
                var distrib = OS.GetLinuxDistributionInfo();
                if (!String.IsNullOrEmpty(distrib.Name))
                {
                    ui.WriteLine($"- distribution name : {distrib.Name}");
                }
                if (distrib.Version > new Version())
                {
                    ui.WriteLine($"- distrib. version  : {distrib.Version}");
                }

            }
            ui.WriteLine($"- x64 architecture  : {OS.IsOSArchitectureX64()}");
            ui.WriteLine($"- native hypervisor : {(OS.IsNativeHypervisorAvailable() ? "available" : "not available")}");
            ui.WriteLine();
        }

        internal static void DisplayComputeInfo(ICommandsUI ui)
        {
            ui.WriteLine("CPU info:");
            var cpu = Compute.GetCPUInfo();
            ui.WriteLine($"- manufacturer       : {cpu.Manufacturer}");
            ui.WriteLine($"- model name         : {cpu.ModelName}");
            ui.WriteLine($"- number of cores    : {cpu.NumberOfCores}");
            ui.WriteLine($"- logical processors : {cpu.NumberOfLogicalProcessors}");
            ui.WriteLine($"- max clock speed    : {cpu.MaxClockSpeedMhz} Mhz");
            ui.WriteLine($"- L3 cache size      : {cpu.L3CacheSizeKB} KB");
            ui.WriteLine($"- virtualization     : {(Compute.IsCPUVirtualizationAvailable(cpu) ? "available" : "not available")}");
            // ui.WriteLine($"- feature flags      : {cpu.FeatureFlags}");
            ui.WriteLine();

            ui.WriteLine("Memory info:");
            var mem = Memory.GetMemoryInfo();
            ui.WriteLine($"- total physical size : {Math.Round(mem.TotalPhysicalMB / 1024f)} GB");
            ui.WriteLine();

            var gpus = Compute.GetNvidiaGPUsInfo();
            if (gpus.Count > 0)
            {
                foreach (var gpu in gpus)
                {
                    ui.WriteLine($"GPU {gpu.Index} info:");
                    ui.WriteLine($"- model name   : {gpu.ModelName}");
                    ui.WriteLine($"- memory       : {Math.Round(gpu.MemoryMB / 1024f)} GB");
                    ui.WriteLine($"- architecture : {gpu.Architecture}");
                    ui.WriteLine();
                }
            }
            else
            {
                ui.WriteLine($"GPU info: no NVIDIA GPU found on this system or driver not installed");
                ui.WriteLine();
            }
        }

        internal static void DisplayStorageInfo(ICommandsUI ui)
        {
            var drives = Storage.GetDrivesInfo();
            foreach (var drive in drives.Values)
            {
                ui.WriteLine($"Drive info: {drive.DrivePath} [{drive.VolumeName}]");
                ui.WriteLine($"- partition id : {drive.PartitionId}");
                ui.WriteLine($"- drive size   : {drive.TotalSizeMB / 1000f:F1} GB");
                ui.WriteLine($"- free space   : {drive.FreeSpaceMB / 1000f:F1} GB");
                ui.WriteLine($"- disk id      : {drive.DiskId}");
                ui.WriteLine($"- disk model   : {drive.DiskModel}");
                ui.WriteLine($"- disk size    : {drive.DiskSizeMB / 1000} GB");
                ui.WriteLine($"- is SSD       : {drive.IsSSD}");
                ui.WriteLine();
            }
        }

        internal static void DisplayNetworkInfo(ICommandsUI ui)
        {
            ui.WriteLine($"Network info: IPv4 addresses");
            var addresses = Network.GetIPAddressesAvailable();
            foreach (var address in addresses.Values)
            {
                ui.WriteLine($"- {address.Address}");
                ui.WriteLine($"  . network interface name : {address.NetworkInterfaceName}");
                if (address.IsLoopback)
                {
                    ui.WriteLine($"  . loopback               : {address.IsLoopback}");
                }
                if (address.IsWireless)
                {
                    ui.WriteLine($"  . wireless               : {address.IsWireless}");
                }
            }
            ui.WriteLine();
        }
    }

    public class SystemStatusCommand : CommandWithUI<SystemStatusCommand.Settings>
    {
        public SystemStatusCommand(ICommandsUI ui) : base(ui) 
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            UI.WriteLine($"System status for the host machine: {OS.GetMachineName()}");
            UI.WriteLine();

            DisplayComputeStatus(UI);
            DisplayStorageStatus(UI);
            DisplayNetworkStatus(UI);

            return 0;
        }        

        public class Settings : CommandSettings
        { }

        internal static void DisplayComputeStatus(ICommandsUI ui)
        {
            var cpu = Compute.GetCPUInfo();
            ui.WriteLine($"CPU usage: {cpu.ModelName}");
            var cpuUsage = Compute.GetPercentCPUTime();
            var mem = Memory.GetMemoryInfo();
            ui.WriteLine($"- cpu load    : {cpuUsage} %");
            ui.WriteLine($"- used memory : {mem.UsedPhysicalMB} MB");
            ui.WriteLine($"- free memory : {mem.FreePhysicalMB} MB");
            ui.WriteLine();

            var gpus = Compute.GetNvidiaGPUsInfo();
            var gpuStats = Compute.GetNvidiaGPUsUsage();
            if (gpuStats.Count > 0)
            {
                foreach (var gpu in gpuStats)
                {
                    ui.WriteLine($"GPU {gpu.Index} usage: {gpus[gpu.Index].ModelName}");
                    ui.WriteLine($"- gpu load    : {gpu.PercentGPUTime} %");
                    ui.WriteLine($"- used memory : {gpu.MemoryUsedMB} MB");
                    ui.WriteLine($"- free memory : {gpu.MemoryFreeMB} MB");
                    ui.WriteLine();
                }
            }
            else
            {
                ui.WriteLine($"GPU usage: no NVIDIA GPU found on this system or driver not installed");
                ui.WriteLine();
            }
        }

        internal static void DisplayStorageStatus(ICommandsUI ui)
        {
            var drives = Storage.GetDrivesInfo();
            foreach (var drive in drives.Values)
            {
                ui.WriteLine($"Drive info: {drive.DrivePath} [{drive.VolumeName}]");
                ui.WriteLine($"- used space   : {(drive.TotalSizeMB - drive.FreeSpaceMB) / 1000f:F1} GB");
                ui.WriteLine($"- free space   : {drive.FreeSpaceMB / 1000f:F1} GB");
                ui.WriteLine($"- percent used : {drive.PercentUsedSpace} %");
                ui.WriteLine();
            }
        }

        internal static void DisplayNetworkStatus(ICommandsUI ui)
        {
            ui.WriteLine($"Network info: TCP ports in use");
            var portsSets = Network.GetTcpPortsInUsePerIPAddress();
            foreach (var ip in portsSets.Keys)
            {
                var line = $"- {ip} : ";
                foreach (var port in portsSets[ip])
                {
                    line += $"{port} ";
                }
                ui.WriteLine(line);
            }
            ui.WriteLine();
        }
    }
}
