using System.Diagnostics;
using wordslab.manager.storage;

namespace wordslab.manager.os
{
    // Reference documentation :
    // https://docs.microsoft.com/en-us/windows/wsl/basic-commands
    // https://github.com/agowa338/WSL-DistroLauncher-Alpine
    // C# calls to wslapi.dll : https://programmerall.com/article/2051677932/
    public static class Wsl
    {
        // 0. Check if WSL 2 is already installed
        
        public static bool IsWSL2AlreadyInstalled()
        {
            if (IsWindowsVersionOKForWSL2())
            {
                try
                {
                    var wslStatus = Wsl.status();
                    if (wslStatus != null && wslStatus.IsInstalled) { return true; }
                }
                catch (Exception) { /* ignore errors */ }
                return false;
            }
            else { return false; }
        }

        // 1. Check requirements for running WSL 2
        // https://docs.microsoft.com/en-us/windows/wsl/install-manual#step-2---check-requirements-for-running-wsl-2
        // 1.1 You must be running Windows 10. For x64 systems: Version 1903 or higher, with Build 18362 or higher.
        // 1.2 You must enable the Virtual Machine Platform optional feature. Your machine will require virtualization capabilities to use this feature.

        // Build 18917 : 
        // - wsl.exe --list --verbose, wsl.exe --list --quiet options
        // - wsl.exe --import --version options 
        // - wsl.exe --shutdown option        
        // Build 19555 :
        // - wsl.exe present when the Windows Subsystem for Linux optional component is not enabled to improve feature discoverability.
        // Build 20150 :
        // - WSL2 GPU compute
        // - wsl.exe --install command line option to easily set up WSL
        // - wsl.exe --update command line option to manage updates to the WSL2 kernel
        // Build 20175 :
        // - adjust default memory assignment of WSL2 VM to be 50% of host memory or 8GB, whichever is less
        // - enable nested virtualization for WSL2 by default on amd64
        // - wsl.exe --update demand start Microsoft Update
        // Build 20190 : 12 août 2020 - 21H1
        // - added support for installing the WSL2 kernel and distributions to wsl.exe --install
        // BACKPORT to 19H1 & 19H2 : 20 août 2020
        // - if your minor build number is 1049 or higher on Windows builds 18362 or 18363, then you have the backport and the ability to run WSL 2 distros
        // Build 20211 :
        // - wsl.exe --mount for mounting physical or virtual disks.
        // - WSL instances are now terminated when the user logs off
        // Build 21286 :
        // - wsl.exe --cd command to set current working directory of a command
        // - added an option to /etc/wsl.conf to enable start up commands: [boot] command=<string>
        // Build 21364 :
        // - GUI apps are now available

        public static bool IsWindowsVersionOKForWSL2()
        {
            return OS.IsOSArchitectureX64() && Windows.IsWindows10Version1903OrHigher();
        }

        // Additional requirements to enable NVIDIA CUDA on WSL 2
        // https://docs.microsoft.com/en-us/windows/ai/directml/gpu-cuda-in-wsl
        // 1.3 WSL 2 GPU acceleration will be available on Pascal (GTX 1050) and later GPU architecture on both GeForce and Quadro product SKUs in WDDM mode
        // 1.4 Windows 11 and Windows 10, version 21H2 (build 19044) support running existing ML tools, libraries, and popular frameworks that use NVIDIA CUDA for GPU hardware acceleration inside a WSL 2 instance.
        // 1.5 Download and install the NVIDIA CUDA-enabled driver for WSL to use with your existing CUDA ML workflow
        // - Windows 11 driver version >= v472.12 (20 sept 2021) : this new Game Ready Driver provides support for the official launch of Windows 11
        // - Windows 10 21H2 driver version >= 496.76 (16 nov 2021) : 
        // 1.6 Ensure you are on the latest WSL Kernel: we recommend 5.10.16.3 or later for better performance and functional fixes.

        public static string GetNvidiaGPUAvailableForWSL2()
        {
            var gpus = Compute.GetNvidiaGPUsInfo();
            foreach (var gpuInfo in gpus)
            {
                if (gpuInfo.Architecture >= Compute.GPUArchitectureInfo.Pascal) { return $"{gpuInfo.ModelName} ({gpuInfo.MemoryMB} MB)"; }
            }
            return null;
        }

        public static bool IsWindowsVersionOKForWSL2WithGPU()
        {
            return OS.IsOSArchitectureX64() && Windows.IsWindows10Version21H2OrHigher();
        }

        public static bool IsNvidiaDriverVersionOKForWSL2WithGPU()
        {
            if (GetNvidiaGPUAvailableForWSL2() != null)
            {
                var driverVersion = Nvidia.GetDriverVersion();
                if (Windows.IsWindows11Version21HOrHigher())
                {
                    return Nvidia.IsNvidiaDriverForWindows20Sep21OrLater(driverVersion);
                }
                else if (Windows.IsWindows10Version21H2OrHigher())
                {
                    return Nvidia.IsNvidiaDriverForWindows16Nov21OrLater(driverVersion);
                }
            }
            return false;
        }

        public static bool IsLinuxKernelVersionOKForWSL2WithGPU()
        {
            if (IsWSL2AlreadyInstalled())
            {
                var wslStatus = Wsl.status();
                var targetVersion = new Version(5, 10, 16);
                return wslStatus.LinuxKernelVersion >= targetVersion;
            }
            return false;
        }

        // 2. Enable WSL2 dependencies
        // https://docs.microsoft.com/en-us/windows/wsl/install-manual#step-3---enable-virtual-machine-feature
        // 2.1 Enable Virtual Machine feature (/featurename:VirtualMachinePlatform) - as Administrator
        // 2.2 Enable the Windows Subsystem for Linux (featurename:Microsoft-Windows-Subsystem-Linux) - as Administrator
        // 2.3 Download and run the Linux kernel update package (https://wslstorestorage.blob.core.Windows.net/wslblob/wsl_update_x64.msi) - as Administrator
        // 2.4 Reboot the computer

        public static async Task LegacyDownloadAndInstallLinuxKernelUpdatePackage(HostStorage storageManager)
        {
            var kernelUpdate = await storageManager.DownloadFileWithCache("https://wslstorestorage.blob.core.Windows.net/wslblob/wsl_update_x64.msi", "wsl_update_x64.msi");
            Command.Run("msiexec.exe", "/i " + kernelUpdate.FullName, timeoutSec: 60);
        }
        
        /// <summary>
        /// A complete WSL restart will then be needed with wsl --shutdown
        /// </summary>
        public static void UpdateLinuxKernelVersion(string scriptsDirectory, string logsDirectory, bool rollback = false)
        {
            string options = "";
            if (rollback) options += "--rollback ";
            Command.ExecuteShellScript(Path.Combine(scriptsDirectory,"os","Wsl"), "update-wsl.ps1", options, logsDirectory, runAsAdmin: true, timeoutSec: 300);
        }

        // WSL 2 commands

        public const string WSLEXE = "wsl.exe";

        // Execute Linux binary files

        public static int exec(string commandLine, string distribution = null, string workingDirectory = null, string userName = null,
                               int timeoutSec = 10, bool unicodeEncoding = false,
                               Action<string> outputHandler = null, Action<string> errorHandler = null, Action<int> exitCodeHandler = null)
        {
            string args = "";
            if (distribution != null) args += $"--distribution {distribution} ";
            if (workingDirectory != null) args += $"--CD \"{workingDirectory}\" ";
            if (userName != null) args += $"--user {userName} ";
            return Command.Run(WSLEXE, args + $"--exec {commandLine}", timeoutSec: timeoutSec, unicodeEncoding: unicodeEncoding,
                                          outputHandler: outputHandler, errorHandler: errorHandler, exitCodeHandler: exitCodeHandler);
        }

        public static int execShell(string commandLine, string distribution = null, string workingDirectory = null, string userName = null,
                                    int timeoutSec = 10, bool unicodeEncoding = false, string ignoreError = null,
                                    Action<string> outputHandler = null, Action<string> errorHandler = null, Action<int> exitCodeHandler = null)
        {
            string error = null;
            if(errorHandler==null && !String.IsNullOrEmpty(ignoreError))
            {
                errorHandler = e => error = e;
            }

            string args = "";
            if (distribution != null) args += $"--distribution {distribution} ";
            if (workingDirectory != null) args += $"--CD \"{workingDirectory}\" ";
            if (userName != null) args += $"--user {userName} ";
            int exitCode = Command.Run(WSLEXE, args + $"-- {commandLine}", timeoutSec: timeoutSec, unicodeEncoding: unicodeEncoding,
                                          outputHandler: outputHandler, errorHandler: errorHandler, exitCodeHandler: exitCodeHandler);

            if (!String.IsNullOrEmpty(error))
            {
                if (!error.Contains(ignoreError)) { throw new InvalidOperationException($"Error while executing command {commandLine} : \"{error}\""); }
            }
            return exitCode;
        }

        public static bool CheckRunningDistribution(string wslDistribution, out string linuxDistributionName, out string linuxDistributionVersion)
        {
            string ldistribution = "unknown";
            string lversion = "?";
            int exitcode = -1;
            try
            {
                var outputParser = Command.Output.GetValue(@"DISTRIB_ID=\s*(?<distrib>[^\s]*)\s*$", s => ldistribution = s).
                                                  GetValue(@"DISTRIB_RELEASE=\s*(?<distrib>[^\s]*)\s*$", s => lversion = s);

                Wsl.execShell("cat /etc/*-release", wslDistribution, outputHandler: outputParser.Run, exitCodeHandler: c => exitcode = c);
            }
            catch (Exception) { }

            linuxDistributionName = ldistribution;
            linuxDistributionVersion = lversion;
            return exitcode == 0;
        }

        // Manage Windows Subsystem for Linux

        public static void install(string distributionName = "Ubuntu")
        {
            Command.Run(WSLEXE, $"--install --distribution {distributionName}", timeoutSec: 300, unicodeEncoding: true);
        }

        public static void setDefaultVersion(int version)
        {
            if (Wsl.status().DefaultVersion != version)
            {
                Command.Run(WSLEXE, $"--set-default-version {version}", unicodeEncoding: true);
            }
        }

        public static void shutdown()
        {
            Command.Run(WSLEXE, "--shutdown", unicodeEncoding: true);
        }

        public class StatusResult
        {
            public bool IsInstalled = false;
            public int DefaultVersion = 0;
            public string DefaultDistribution;
            public Version LinuxKernelVersion;
            public string LastWSLUpdate;
        }

        public static StatusResult status()
        {
            var result = new StatusResult();
            try
            {
                string? distrib = null;
                string? wslver = null;
                string? wsldate = null;
                string? linuxver = null;
                var outputParser = Command.Output.GetValue(@":\s+(?<distrib>[a-zA-Z]+[^\s]*)\s*$", s => distrib = s).
                                                  GetValue(@":\s+(?<wslver>[\d])\s*$", s => wslver = s).
                                                  GetValue(@"\s+(?<wsldate>\d+(?:/\d+)+)\s*$", s => wsldate = s).
                                                  GetValue(@":\s+(?<linuxver>(?:\d+\.)+\d+)\s*$", s => linuxver = s);

                Command.Run("wsl", "--status", unicodeEncoding: true, outputHandler: outputParser.Run, exitCodeHandler: c => result.IsInstalled = (c == 0));

                if (!String.IsNullOrEmpty(wslver)) result.DefaultVersion = Int32.Parse(wslver);
                if (!String.IsNullOrEmpty(distrib)) result.DefaultDistribution = distrib;
                if (!String.IsNullOrEmpty(linuxver)) result.LinuxKernelVersion = new Version(linuxver);
                if (!String.IsNullOrEmpty(wsldate)) result.LastWSLUpdate = wsldate;
            }
            catch (Exception ex)
            {
                // This method is used to check if wsl is installed => do nothing in case of exception
            }
            return result;
        }

        // https://docs.microsoft.com/en-us/windows/wsl/wsl-config#wslconfig
        public class WslConfig
        {
            // An absolute Windows path to a custom Linux kernel.
            // default: The Microsoft built kernel provided inbox
            public string kernel;

            // How much memory to assign to the WSL 2 VM.
            // default: 50% of total memory on Windows or 8GB, whichever is less; on builds before 20175: 80% of your total memory on Windows
            public int? memoryMB;

            // How many processors to assign to the WSL 2 VM.
            // default: The same number of processors on Windows
            public int? processors;

            // Boolean specifying if ports bound to wildcard or localhost in the WSL 2 VM should be connectable from the host via localhost:port.
            // default: true
            public bool? localhostForwarding;

            // Additional kernel command line arguments.
            // default: Blank
            public string kernelCommandLine;

            // How much swap space to add to the WSL 2 VM, 0 for no swap file. Swap storage is disk-based RAM used when memory demand exceeds limit on hardware device.
            // default: 25% of memory size on Windows rounded up to the nearest GB
            public int? swap;

            // An absolute Windows path to the swap virtual hard disk.
            // default: %USERPROFILE%\AppData\Local\Temp\swap.vhdx
            public string swapFile;

            // Default true setting enables Windows to reclaim unused memory allocated to WSL 2 virtual machine.
            // default: true
            public bool? pageReporting;

            // Boolean to turn on or off support for GUI applications(WSLg) in WSL. Only available for Windows 11.
            // default: true
            public bool? guiApplications;

            // Boolean to turn on an output console Window that shows the contents of dmesg upon start of a WSL 2 distro instance. Only available for Windows 11.
            // default: false
            public bool? debugConsole;

            // Boolean to turn on or off nested virtualization, enabling other nested VMs to run inside WSL 2. Only available for Windows 11.
            // default: true
            public bool? nestedVirtualization;

            // The number of milliseconds that a VM is idle, before it is shut down. Only available for Windows 11.
            // default: 60000
            public int? vmIdleTimeout;

            public void SetDefaultValues()
            {
                var memoryInfo = Memory.GetMemoryInfo();
                if(memoryInfo.TotalPhysicalMB >= 16000)
                {
                    memoryMB = 8 * 1024;
                }
                else
                {
                    memoryMB = (int)Math.Round(memoryInfo.TotalPhysicalMB / 2000f) * 1024;
                }
                var cpuInfo = Compute.GetCPUInfo();
                processors = (int)cpuInfo.NumberOfLogicalProcessors;
                localhostForwarding = true;
            }

            // true only if the values were loaded from the wslconfig file
            public bool LoadedFromFile = false;

            public WslConfig Clone()
            {
                return (WslConfig)this.MemberwiseClone();
            }
        }

        private static readonly string wslconfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wslconfig");

        public static WslConfig Read_wslconfig()
        {    
            var config = new WslConfig();
            // Try to read the config file
            if (File.Exists(wslconfigPath))
            {
                try
                {
                    using(StreamReader sr = new StreamReader(wslconfigPath))
                    {
                        string line = null;
                        while((line=sr.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (line.Length == 0) continue;
                            if (line.StartsWith('#') || line.StartsWith('[')) continue;
                            int indexEquals = line.IndexOf('=');
                            if(indexEquals > 0)
                            {
                                string key = line.Substring(0, indexEquals).Trim();
                                string value = line.Substring(indexEquals + 1).Trim();
                                switch (key)
                                {
                                    case "kernel":
                                        config.kernel = value;
                                        break;
                                    case "memory":
                                        config.memoryMB = ParseSizeMB(value);
                                        break;
                                    case "processors":
                                        config.processors = int.Parse(value);
                                        break;
                                    case "localhostForwarding":
                                        config.localhostForwarding = ParseBoolean(value);
                                        break;
                                    case "kernelCommandLine":
                                        config.kernelCommandLine = value;
                                        break;
                                    case "swap":
                                        config.swap = ParseSizeMB(value);
                                        break;
                                    case "swapFile":
                                        config.swapFile = value.Replace("\\\\", "\\");
                                        break;
                                    case "pageReporting":
                                        config.pageReporting = ParseBoolean(value);
                                        break;
                                    case "guiApplications":
                                        config.guiApplications = ParseBoolean(value);
                                        break;
                                    case "debugConsole":
                                        config.debugConsole = ParseBoolean(value);
                                        break;
                                    case "nestedVirtualization":
                                        config.nestedVirtualization = ParseBoolean(value);
                                        break;
                                    case "vmIdleTimeout":
                                        config.vmIdleTimeout = int.Parse(value);
                                        break;
                                }
                            }
                        }
                    }
                    config.LoadedFromFile = true;
                    return config;
                } catch { }
            }
            // If it didn't work, return the default values
            config.SetDefaultValues();
            return config;
        }

        private static bool ParseBoolean(string value)
        {
            return value == "true";
        }

        private static int? ParseSizeMB(string value)
        {
            if(value.EndsWith("MB"))
            {
                return int.Parse(value.Substring(0, value.Length - 2));
            } 
            else if (value.EndsWith("GB"))
            {
                return int.Parse(value.Substring(0, value.Length - 2)) * 1024;
            }
            return null;
        }

        // WARNING: overrides the existing MACHINE-WIDE configuration
        public static void Write_wslconfig(WslConfig config)
        {
            using(StreamWriter sw = new StreamWriter(wslconfigPath))
            {
                sw.WriteLine("[wsl2]");
                if(config.kernel != null)
                {
                    sw.WriteLine($"kernel = {config.kernel}");
                }
                if (config.memoryMB.HasValue)
                {
                    WriteSize(sw, "memory", config.memoryMB.Value);
                }
                if (config.processors.HasValue)
                {
                    sw.WriteLine($"processors = {config.processors.Value}");
                }
                if(config.localhostForwarding.HasValue)
                {
                    WriteBoolean(sw, "localhostForwarding", config.localhostForwarding.Value);
                }
                if(config.kernelCommandLine != null)
                {
                    sw.WriteLine($"kernelCommandLine = {config.kernelCommandLine}");
                }
                if(config.swap.HasValue)
                {
                    WriteSize(sw, "swap", config.swap.Value);
                }
                if(config.swapFile != null)
                {
                    sw.WriteLine($"swapFile = {config.swapFile.Replace("\\","\\\\")}");
                }
                if(config.pageReporting.HasValue)
                {
                    WriteBoolean(sw, "pageReporting", config.pageReporting.Value);
                }
                if (config.guiApplications.HasValue)
                {
                    WriteBoolean(sw, "guiApplications", config.guiApplications.Value);
                }
                if(config.debugConsole.HasValue)
                {
                    WriteBoolean(sw, "debugConsole", config.debugConsole.Value);
                }
                if(config.nestedVirtualization.HasValue)
                {
                    WriteBoolean(sw, "nestedVirtualization", config.nestedVirtualization.Value);
                }
                if (config.vmIdleTimeout.HasValue)
                {
                    sw.WriteLine($"vmIdleTimeout = {config.vmIdleTimeout.Value}");
                }
                config.LoadedFromFile = true;
            }
        }

        private static void WriteBoolean(StreamWriter sw, string key, bool value)
        {
            if(value)
            {
                sw.WriteLine($"{key} = true");
            }
            else
            {
                sw.WriteLine($"{key} = false");
            }
        }

        private static void WriteSize(StreamWriter sw, string key, int size)
        {
            if (size >= 1024)
            {
                sw.WriteLine($"{key} = {size/1024}GB");
            }
            else
            {
                sw.WriteLine($"{key} = {size}MB");
            }
        }

        // Manage distributions in Windows Subsystem for Linux

        public static void export(string distribution, string filename)
        {
            Command.Run(WSLEXE, $"--export {distribution} \"{filename}\"", timeoutSec: 60, unicodeEncoding: true);
        }

        public static void import(string distribution, string installPath, string filename, int? version = null)
        {
            string options = "";
            if (version.HasValue) options += $"--version {version.Value} ";
            Command.Run(WSLEXE, $"--import {distribution} \"{installPath}\" \"{filename}\" {options}", timeoutSec: 60, unicodeEncoding: true);
        }

        public class DistributionInfo
        {
            public string Distribution;
            public string OnlineFriendlyName;
            public bool IsDefault = false;
            public bool IsRunning = false;
            public int WslVersion = 0;
        }

        public static List<DistributionInfo> list(bool online = false)
        {
            var result = new List<object>();
            if(!online)
            {
                var outputParser = Command.Output.GetList(
                    @"NAME\s*STATE\s*VERSION",
                    @"(?<default>\*?)\s*(?<name>[^\s]*)\s*(?<state>[^\s]*)\s*(?<version>\d)$",
                    dict => new DistributionInfo() { IsDefault = dict["default"] == "*",
                                               Distribution = dict["name"],
                                               IsRunning = dict["state"] != "Stopped",
                                               WslVersion = Int32.Parse(dict["version"]) },
                    result);

                Command.Run(WSLEXE, "--list --verbose", unicodeEncoding: true, outputHandler: outputParser.Run);
            }
            else
            {
                var outputParser = Command.Output.GetList(
                    @"NAME\s*FRIENDLY NAME",
                    @"(?<name>[^\s]+)\s*(?<friendlyname>.*)$",
                    dict => new DistributionInfo()
                    {
                        Distribution = dict["name"],
                        OnlineFriendlyName = dict["friendlyname"]
                    },
                    result);

                Command.Run(WSLEXE, "--list --online", unicodeEncoding: true, outputHandler: outputParser.Run);
            }
            return result.Cast<DistributionInfo>().ToList();
        }

        public static void setDefaultDistribution(string distribution)
        {            
            Command.Run(WSLEXE, $"--set-default {distribution}", unicodeEncoding: true);
        }

        public static bool setVersion(string distribution, int version)
        {
            int exitCode = 0;
            string output = null;
            Command.Run(WSLEXE, $"--set-version {distribution} {version}", unicodeEncoding: true, outputHandler: o => output = o, exitCodeHandler: c => exitCode = c);
            bool conversionInProgress = exitCode != -1;
            return conversionInProgress;
        }

        public static void terminate(string distribution)
        {
            Command.Run(WSLEXE, $"--terminate {distribution}", unicodeEncoding: true);
        }

        public static void unregister(string distribution)
        {
            Command.Run(WSLEXE, $"--unregister {distribution}", timeoutSec: 30, unicodeEncoding: true);
        }

        // Get running process properties

        private static readonly int MEGA = 1024 * 1024;

        public static uint GetVirtualMachineWorkingSetMB()
        {
            var processes = Process.GetProcessesByName("vmmem");
            if(processes.Length == 0)
            {
                return 0;
            }
            else
            {
                return (uint)(processes[0].WorkingSet64 / MEGA);
            }
        }
    }
}
