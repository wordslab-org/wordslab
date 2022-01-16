using wordslab.installer.localstorage;

namespace wordslab.installer.infrastructure.commands
{
    // Reference documentation :
    // https://docs.microsoft.com/en-us/windows/wsl/basic-commands
    // https://github.com/agowa338/WSL-DistroLauncher-Alpine
    // C# calls to wslapi.dll : https://programmerall.com/article/2051677932/
    public static class wsl
    {
        // 0. Check if WSL 2 is already installed

        public static bool IsWSL2AlreadyInstalled()
        {
            if (IsWindowsVersionOKForWSL2())
            {
                try
                {
                    var wslStatus = wsl.status();
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

        public static bool IsWindowsVersionOKForWSL2()
        {
            return windows.IsOSArchitectureX64() && windows.IsWindows10Version1903OrHigher();
        }

        public static bool IsVirtualizationEnabled()
        {
            return windows.IsVirtualMachinePlatformEnabled() || windows.IsVirtualizationEnabled();
        }

        // Additional requirements to enable NVIDIA CUDA on WSL 2
        // https://docs.microsoft.com/en-us/windows/ai/directml/gpu-cuda-in-wsl
        // 1.3 WSL 2 GPU acceleration will be available on Pascal (GTX 1050) and later GPU architecture on both GeForce and Quadro product SKUs in WDDM mode
        // 1.4 Windows 11 and Windows 10, version 21H2 (build 19044) support running existing ML tools, libraries, and popular frameworks that use NVIDIA CUDA for GPU hardware acceleration inside a WSL 2 instance.
        // 1.5 Download and install the NVIDIA CUDA-enabled driver for WSL to use with your existing CUDA ML workflow
        // - Windows 11 driver version >= v472.12 (20 sept 2021) : this new Game Ready Driver provides support for the official launch of Windows 11
        // - Windows 10 21H2 driver version >= 496.76 (16 nov 2021) : 
        // 1.6 Ensure you are on the latest WSL Kernel: we recommend 5.10.16.3 or later for better performance and functional fixes.

        public static bool IsNvidiaGPUAvailableForWSL2()
        {
            var gpus = nvidia.GetNvidiaGPUs();
            foreach (var gpuInfo in gpus)
            {
                if (gpuInfo.Architecture >= nvidia.GPUArchitectureInfo.Pascal) { return true; }
            }
            return false;
        }

        public static bool IsWindowsVersionOKForWSL2WithGPU()
        {
            return windows.IsOSArchitectureX64() && windows.IsWindows10Version21H2OrHigher();
        }

        public static bool IsNvidiaDriverVersionOKForWSL2WithGPU()
        {
            if (IsNvidiaGPUAvailableForWSL2())
            {
                if (windows.IsWindows11Version21HOrHigher())
                {
                    return nvidia.IsNvidiaDriver20Sep21OrLater();
                }
                else if (windows.IsWindows10Version21H2OrHigher())
                {
                    return nvidia.IsNvidiaDriver16Nov21OrLater();
                }
            }
            return false;
        }

        public static bool IsLinuxKernelVersionOKForWSL2WithGPU()
        {
            if (IsWSL2AlreadyInstalled())
            {
                var wslStatus = wsl.status();
                var targetVersion = new Version(5, 10, 16);
                return wslStatus.LinuxKernelVersion >= targetVersion;
            }
            return false;
        }

        // 2. Enable WSL2 dependencies
        // https://docs.microsoft.com/en-us/windows/wsl/install-manual#step-3---enable-virtual-machine-feature
        // 2.1 Enable Virtual Machine feature (/featurename:VirtualMachinePlatform) - as Administrator
        // 2.2 Enable the Windows Subsystem for Linux (featurename:Microsoft-Windows-Subsystem-Linux) - as Administrator
        // 2.3 Download and run the Linux kernel update package (https://wslstorestorage.blob.core.windows.net/wslblob/wsl_update_x64.msi) - as Administrator
        // 2.4 Reboot the computer

        public static void DownloadAndInstallLinuxKernelUpdatePackage(LocalStorageManager localStorage)
        {
            var kernelUpdate = localStorage.DownloadFileWithCache("https://wslstorestorage.blob.core.windows.net/wslblob/wsl_update_x64.msi", "wsl_update_x64.msi");
            Command.Run(kernelUpdate.FullName, runAsAdmin: true);
        }

        // WSL 2 commands

        public const string COMMAND = "wsl";

        // Execute Linux binary files

        public static int exec(string commandLine, string distribution = null, string workingDirectory = null, string userName = null,
                               Action<string> outputHandler = null, Action<string> errorHandler = null, Action<int> exitCodeHandler = null)
        {
            string args = "";
            if (distribution != null) args += $"--distribution {distribution} ";
            if (workingDirectory != null) args += $"--CD \"{workingDirectory}\" ";
            if (userName != null) args += $"--user {userName}";
            return Command.Run("wsl.exe", args + $"--exec \"{commandLine}\"");
        }

        public static int execShell(string commandLine, string distribution = null, string workingDirectory = null, string userName = null,
                                    Action<string> outputHandler = null, Action<string> errorHandler = null, Action<int> exitCodeHandler = null)
        {
            string args = "";
            if (distribution != null) args += $"--distribution {distribution} ";
            if (workingDirectory != null) args += $"--CD \"{workingDirectory}\" ";
            if (userName != null) args += $"--user {userName}";
            return Command.Run("wsl.exe", args + $"-- {commandLine}", outputHandler: outputHandler, errorHandler: errorHandler, exitCodeHandler: exitCodeHandler);
        }

        public static bool CheckRunningDistribution(out string distribution, out string version)
        {
            string ldistribution = "unknown";
            string lversion = "?";
            int exitcode = -1;
            try
            {
                var outputParser = Command.Output.GetValue(@"DISTRIB_ID=\s*(?<distrib>.*)\s*$", s => ldistribution = s).
                                                  GetValue(@"DISTRIB_RELEASE=\s*(?<distrib>.*)\s*$", s => lversion = s);

                Command.Run("wsl.exe", "-- cat /etc/*-release", outputHandler: outputParser.Run, exitCodeHandler: c => exitcode = c);
            }
            catch (Exception) { }

            distribution = ldistribution;
            version = lversion;
            return exitcode == 0;
        }

        // Manage Windows Subsystem for Linux

        public static void install(string distributionName = "Ubuntu")
        {
            string args = "";
            if (distributionName != null) args += $"--distribution {distributionName} ";
            Command.Run("wsl.exe", args + "--install");
        }

        public static void setDefaultVersion(int version)
        {
            Command.Run("wsl.exe", $"--set-default-version {version}");
        }

        public static void shutdown()
        {
            Command.Run("wsl.exe", "--shutdown");
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

                Command.Run("wsl", "--status", outputHandler: outputParser.Run, exitCodeHandler: c => result.IsInstalled = (c == 0));

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

        public static void update(bool rollback = false)
        {
            string args = "";
            if (rollback) args += "--rollback ";
            Command.Run("wsl.exe", args + "--update");
        }

        // Manage distributions in Windows Subsystem for Linux

        public static void export(string distribution, string filename)
        {
            Command.Run("wsl.exe", $"--export {distribution} \"{filename}\"");
        }

        public static void import(string distribution, string installPath, string filename, int? version = null)
        {
            string args = "";
            if (version.HasValue) args += $"--version {version.Value} ";
            Command.Run("wsl.exe", args + $"--import {distribution} \"{installPath}\" \"{filename}\"");
        }

        public class ListResult
        {
            public string Distribution;
            public string OnlineFriendlyName;
            public bool IsDefault = false;
            public bool IsRunning = false;
            public int WslVersion = 0;
        }

        public static List<ListResult> list(bool online = false)
        {
            var result = new List<ListResult>();
            if(!online)
            {
                var outputParser = Command.Output.GetList(
                    @"NAME\s*STATE\s*VERSION",
                    @"(?<default>\*?)\s*(?<name>[^\s]*)\s*(?<state>[^\s]*)\s*(?<version>\d)$",
                    dict => new ListResult() { IsDefault = dict["default"] == "*",
                                               Distribution = dict["name"],
                                               IsRunning = dict["state"] != "Stopped",
                                               WslVersion = Int32.Parse(dict["version"]) },
                    (IList<object>)result);

                Command.Run("wsl.exe", "--list --verbose", outputHandler: outputParser.Run);
            }
            else
            {
                var outputParser = Command.Output.GetList(
                    @"NAME\s*FRIENDLY NAME",
                    @"(?<name>[^\s]+)\s*(?<friendlyname>.*)$",
                    dict => new ListResult()
                    {
                        Distribution = dict["name"],
                        OnlineFriendlyName = dict["friendlyname"]
                    },
                    (IList<object>)result);

                Command.Run("wsl.exe", "--list --online", outputHandler: outputParser.Run);
            }
            return result;
        }

        public static void setDefaultDistribution(string distribution)
        {
            Command.Run("wsl.exe", $"--set-default {distribution}");
        }

        public static void setVersion(string distribution, int version)
        {
            Command.Run("wsl.exe", $"--set-version {distribution} {version}");
        }

        public static void terminate(string distribution)
        {
            Command.Run("wsl.exe", $"--terminate {distribution}");
        }

        public static void unregister(string distribution)
        {
            Command.Run("wsl.exe", $"--unregister {distribution}");
        }
    }
}
