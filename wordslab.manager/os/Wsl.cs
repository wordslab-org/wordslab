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
            return Windows.IsOSArchitectureX64() && Windows.IsWindows10Version1903OrHigher();
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
                if (gpuInfo.Architecture >= Compute.GPUArchitectureInfo.Pascal) { return $"{gpuInfo.Name} ({gpuInfo.MemoryMB} MB)"; }
            }
            return null;
        }

        public static bool IsWindowsVersionOKForWSL2WithGPU()
        {
            return Windows.IsOSArchitectureX64() && Windows.IsWindows10Version21H2OrHigher();
        }

        public static bool IsNvidiaDriverVersionOKForWSL2WithGPU()
        {
            if (GetNvidiaGPUAvailableForWSL2() != null)
            {
                var driverVersion = Nvidia.GetDriverVersion();
                if (Windows.IsWindows11Version21HOrHigher())
                {
                    return Nvidia.IsNvidiaDriver20Sep21OrLater(driverVersion);
                }
                else if (Windows.IsWindows10Version21H2OrHigher())
                {
                    return Nvidia.IsNvidiaDriver16Nov21OrLater(driverVersion);
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

        public static async Task LegacyDownloadAndInstallLinuxKernelUpdatePackage(StorageManager storageManager)
        {
            var kernelUpdate = await storageManager.DownloadFileWithCache("https://wslstorestorage.blob.core.Windows.net/wslblob/wsl_update_x64.msi", "wsl_update_x64.msi");
            Command.Run("msiexec.exe", "/i " + kernelUpdate.FullName, timeoutSec: 60);
        }
        
        public static void UpdateLinuxKernelVersion(bool rollback = false)
        {
            string options = "";
            if (rollback) options += "--rollback ";
            Command.Run("wsl.exe", $"--update {options}", timeoutSec: 30, unicodeEncoding: true);
        }

        // WSL 2 commands

        public const string COMMAND = "wsl";

        // Execute Linux binary files

        public static int exec(string commandLine, string distribution = null, string workingDirectory = null, string userName = null,
                               int timeoutSec = 10, bool unicodeEncoding = false,
                               Action<string> outputHandler = null, Action<string> errorHandler = null, Action<int> exitCodeHandler = null)
        {
            string args = "";
            if (distribution != null) args += $"--distribution {distribution} ";
            if (workingDirectory != null) args += $"--CD \"{workingDirectory}\" ";
            if (userName != null) args += $"--user {userName} ";
            return Command.Run("wsl.exe", args + $"--exec {commandLine}", timeoutSec: timeoutSec, unicodeEncoding: unicodeEncoding,
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
            int exitCode = Command.Run("wsl.exe", args + $"-- {commandLine}", timeoutSec: timeoutSec, unicodeEncoding: unicodeEncoding,
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
                var outputParser = Command.Output.GetValue(@"DISTRIB_ID=\s*(?<distrib>.*)\s*$", s => ldistribution = s).
                                                  GetValue(@"DISTRIB_RELEASE=\s*(?<distrib>.*)\s*$", s => lversion = s);

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
            Command.Run("wsl.exe", $"--install --distribution {distributionName}", timeoutSec: 300, unicodeEncoding: true);
        }

        public static void setDefaultVersion(int version)
        {
            if (Wsl.status().DefaultVersion != version)
            {
                Command.Run("wsl.exe", $"--set-default-version {version}", unicodeEncoding: true);
            }
        }

        public static void shutdown()
        {
            Command.Run("wsl.exe", "--shutdown", unicodeEncoding: true);
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

       

        // Manage distributions in Windows Subsystem for Linux

        public static void export(string distribution, string filename)
        {
            Command.Run("wsl.exe", $"--export {distribution} \"{filename}\"", timeoutSec: 60, unicodeEncoding: true);
        }

        public static void import(string distribution, string installPath, string filename, int? version = null)
        {
            string options = "";
            if (version.HasValue) options += $"--version {version.Value} ";
            Command.Run("wsl.exe", $"--import {distribution} \"{installPath}\" \"{filename}\" {options}", timeoutSec: 60, unicodeEncoding: true);
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

                Command.Run("wsl.exe", "--list --verbose", unicodeEncoding: true, outputHandler: outputParser.Run);
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

                Command.Run("wsl.exe", "--list --online", unicodeEncoding: true, outputHandler: outputParser.Run);
            }
            return result.Cast<DistributionInfo>().ToList();
        }

        public static void setDefaultDistribution(string distribution)
        {
            Command.Run("wsl.exe", $"--set-default {distribution}", unicodeEncoding: true);
        }

        public static void setVersion(string distribution, int version)
        {
            Command.Run("wsl.exe", $"--set-version {distribution} {version}", unicodeEncoding: true);
        }

        public static void terminate(string distribution)
        {
            Command.Run("wsl.exe", $"--terminate {distribution}", unicodeEncoding: true);
        }

        public static void unregister(string distribution)
        {
            Command.Run("wsl.exe", $"--unregister {distribution}", timeoutSec: 30, unicodeEncoding: true);
        }
    }
}
