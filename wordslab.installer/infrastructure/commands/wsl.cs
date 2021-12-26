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
            foreach(var gpuInfo in gpus)
            {
                if(gpuInfo.Architecture >= nvidia.GPUArchitectureInfo.Pascal) {  return true; }
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
            if(IsWSL2AlreadyInstalled())
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



        // WSL 2 commands

        public const string COMMAND = "wsl";

        // Execute Linux binary files

        public static bool exec(string commandLine, string distribution = null, string workingDirectory = null, string userName = null)
        {
            return true;
        }

        public static bool execShell(string commandLine, string workingDirectory = null)
        {
            return true;
        }

        // Manage Windows Subsystem for Linux

        public static bool install(string distributionName = "Ubuntu")
        {
            return true;
        }

        public static bool setDefaultVersion(int version)
        {
            return true;
        }

        public static bool shutdown()
        {
            return true;
        }

        public class StatusResult
        {
            public bool    IsInstalled = false;            
            public int     DefaultVersion = 0;
            public string  DefaultDistribution;
            public Version LinuxKernelVersion;
            public string  LastWSLUpdate;
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

                Command.Run("wsl", "--status", outputHandler: outputParser.Run, exitCodeHandler: c => result.IsInstalled=(c==0));

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

        public static bool update(bool rollback = false)
        {
            return true;
        }

        // Manage distributions in Windows Subsystem for Linux

        public static bool export(string distribution, string filename)
        {
            return true;
        }

        public static bool import(string distribution, string installPath, string filename, int version = 2)
        {
            return true;
        }

        public static bool list(bool all = false, bool quiet = false, bool verbose = false, bool online = false)
        {
            return true;
        }

        public static bool setDefaultDistribution(string distributionName)
        {
            return true;
        }

        public static bool setVersion(string distribution, int version)
        {
            return true;
        }

        public static bool terminate(string distribution)
        {
            return true;
        }

        public static bool unregister(string distribution)
        {
            return true;
        }

        /*
        // Executes : wsl -l -v
        // Returns  : 
        // -1 if WSL2 is not installed
        //  0 if WSL2 is ready but no distribution was installed
        //  1 if WSL2 is ready but the default distribution is set to run in WSL version 1
        //  2 if WSL2 is ready and the default distribution is set to run in WSL version 2
        public static int CheckWSLVersion()
        {
            try
            {
                string output;
                string error;
                int exitcode = Process.Run("wsl.exe", "-l -v", 5, out output, out error, true);
                if (exitcode == 0 && String.IsNullOrEmpty(error))
                {
                    if (String.IsNullOrEmpty(output))
                    {
                        return 0;
                    }
                    var lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length < 2)
                    {
                        return 0;
                    }
                    for (var i = 1; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        var cols = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (cols.Length == 4)
                        {
                            return Int32.Parse(cols[3]);
                        }
                    }
                    return 0;
                }
            }
            catch (Exception)
            { }
            return -1;
        }

        // Executes : wsl -- uname -r
        // Returns  :  
        // Version object if kernel version was correctly parsed
        // null otherwise
        public static Version CheckKernelVersion()
        {
            try
            {
                string output;
                string error;
                int exitcode = Process.Run("wsl.exe", "-- uname -r", 5, out output, out error);
                if (exitcode == 0 && String.IsNullOrEmpty(error) && !String.IsNullOrEmpty(output))
                {
                    int firstDot = output.IndexOf('.');
                    int secondDot = output.IndexOf('.', firstDot + 1);
                    if(firstDot > 0 && secondDot > firstDot)
                    {
                        var major = Int32.Parse(output.Substring(0, firstDot));
                        var minor = Int32.Parse(output.Substring(firstDot+1, secondDot-firstDot-1));
                        return new Version(major, minor);
                    }
                }
            }
            catch (Exception)
            { }
            return null;
        }

        // Executes : wsl -- cat /etc/*-release
        // Returns  :  
        // true if the default distribution launched by the wsl command is Ubuntu
        // false otherwise
        public static bool CheckUbuntuDistribution(out string distrib, out string version)
        {
            distrib = "unknown";
            version = "?";
            try
            {
                string output;
                string error;
                int exitcode = Process.Run("wsl.exe", "-- cat /etc/*-release", 5, out output, out error);
                if (exitcode == 0 && String.IsNullOrEmpty(error) && !String.IsNullOrEmpty(output))
                {
                    var lines = output.Split('\n');
                    foreach(var line in lines)
                    {
                        if (line.StartsWith("DISTRIB_ID="))
                        {
                            distrib = line.Substring(11);
                        }
                        else if (line.StartsWith("DISTRIB_RELEASE="))
                        {
                            version = line.Substring(16);
                        }
                    }
                    var major = Int32.Parse(version.Substring(0, 2));
                    if( String.Compare(distrib, "Ubuntu", StringComparison.InvariantCultureIgnoreCase) == 0 &&
                        major >= 18)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            { }
            return false;
        }
    }*/
    }
}
