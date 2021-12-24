using System.Runtime.InteropServices;

namespace wordslab.installer.infrastructure.commands
{
    /*
    https://docs.microsoft.com/en-us/windows/wsl/install-manual
     
    # Uninstall (as admin)
    dism.exe /online /disable-feature /featurename:VirtualMachinePlatform  /norestart
      > L’opération a réussi.
    dism.exe /online /disable-feature /featurename:Microsoft-Windows-Subsystem-Linux /norestart
      > L’opération a réussi.

    # Check status
    PowerShell: get-windowsoptionalfeature -online -featurename Microsoft-Windows-Subsystem-Linux
      > State            : Disabled
    wsl --status
      > %ERRORLEVEL% = 1

    # Install (as admin)
    wsl --install
      > Installation en cours : Plateforme de machine virtuelle
      > Installation en cours : Sous-système Windows pour Linux
      > Téléchargement en cours : Ubuntu
      > Les modifications ne seront pas effectives avant que le système ne soit réamorcé.
      > %ERRORLEVEL% = 0

    # Files at this point
    C:\Users\laure\AppData\Local\Temp\Ubuntu.2020.424.0_x64.appx => 442 381 ko
    C:\Users\laure\AppData\Local\Packages => nothing

    # REBOOT 
    -> execute appx distribution launcher

    # Files at this point
    C:\Users\laure\AppData\Local\Temp\Ubuntu.2020.424.0_x64.appx => DELETED
    C:\Users\laure\AppData\Local\Packages\CanonicalGroupLimited.UbuntuonWindows_79rhkp1fndgsc\LocalState\ext4.vhdx => 1 145 856 Ko

    # Launch distribution for the first time
    Enter new UNIX username: / New password:
    passwd: password updated successfully

    # Files at this point
    C:\Users\laure\AppData\Local\Temp => swap.vhdx : 64 152 Ko

    # Check status
    wsl --status
      > %ERRORLEVEL% = 0
     */
    public class windows
    {
        // 1. Check requirements for running WSL 2
        // https://docs.microsoft.com/en-us/windows/wsl/install-manual#step-2---check-requirements-for-running-wsl-2
        // 1.1 You must be running Windows 10. For x64 systems: Version 1903 or higher, with Build 18362 or higher.
        // 1.2 You must enable the Virtual Machine Platform optional feature. Your machine will require virtualization capabilities to use this feature.

        // Additional requirements to enable NVIDIA CUDA on WSL 2
        // https://docs.microsoft.com/en-us/windows/ai/directml/gpu-cuda-in-wsl
        // 1.3 Windows 11 and Windows 10, version 21H2 (build 19044) support running existing ML tools, libraries, and popular frameworks that use NVIDIA CUDA for GPU hardware acceleration inside a WSL 2 instance.
        // 1.4 Download and install the NVIDIA CUDA-enabled driver for WSL to use with your existing CUDA ML workflow
        // - Windows 11 driver version >= v472.12 (20 sept 2021) : this new Game Ready Driver provides support for the official launch of Windows 11
        // - Windows 10 21H2 driver version >= 496.76 (16 nov 2021) : 
        // 1.5 WSL 2 GPU acceleration will be available on Pascal (GTX 1050) and later GPU architecture on both GeForce and Quadro product SKUs in WDDM mode
        // 1.6 Ensure you are on the latest WSL Kernel: we recommend 5.10.16.3 or later for better performance and functional fixes.

        // 2. Enable WSL2 dependencies
        // https://docs.microsoft.com/en-us/windows/wsl/install-manual#step-3---enable-virtual-machine-feature
        // 2.1 Enable Virtual Machine feature (/featurename:VirtualMachinePlatform) - as Administrator
        // 2.2 Enable the Windows Subsystem for Linux (featurename:Microsoft-Windows-Subsystem-Linux) - as Administrator
        // 2.3 Download and run the Linux kernel update package (https://wslstorestorage.blob.core.windows.net/wslblob/wsl_update_x64.msi) - as Administrator
        // 2.4 Reboot the computer

        internal static bool IsOSArchitectureX64()
        {
            return RuntimeInformation.OSArchitecture == Architecture.X64;
        }

        internal static bool IsWindows10Version1903OrHigher()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var targetVersion = new Version(10, 0, 18362);
                return Environment.OSVersion.Version >= targetVersion;
            }
            else { return false; }
        }

        internal static bool IsWindows10Version21H2OrHigher()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var targetVersion = new Version(10, 0, 19044);
                return Environment.OSVersion.Version >= targetVersion;
            }
            else { return false; }
        }

        public static bool IsWindowsVersionOKForWSL2()
        {
            return IsOSArchitectureX64() && IsWindows10Version1903OrHigher();
        }

        public static bool IsWindowsVersionOKForWSL2WithGPU()
        {
            return IsOSArchitectureX64() && IsWindows10Version21H2OrHigher();
        }

        // Checking HyperV requirements : several alernatives
        // https://docs.microsoft.com/fr-fr/windows-server/administration/windows-commands/systeminfo // https://www.technorms.com/8208/check-if-processor-supports-virtualization
        // https://docs.microsoft.com/en-us/virtualization/api/hypervisor-platform/funcs/whvgetcapability // HypervisorSharp : https://git.9net.org/-/snippets/5
        // https://stackoverflow.com/questions/49005791/how-to-check-if-intel-virtualization-is-enabled-without-going-to-bios-in-windows
        // => choosing the PowerShell alternative :
        // powershell.exe Get-ComputerInfo -property "HyperV*"
        //  HyperVisorPresent                                 : True
        //  HyperVRequirementDataExecutionPreventionAvailable : True
        //  HyperVRequirementSecondLevelAddressTranslation    : True
        //  HyperVRequirementVirtualizationFirmwareEnabled    : True
        //  HyperVRequirementVMMonitorModeExtensions          : True

        public static bool IsVirtualizationEnabled()
        {
            return true;
        }

        // Managing Windows Optional features : several alternatives 
        // dism.exe /online /disable-feature /featurename:VirtualMachinePlatform  /norestart
        // https://docs.microsoft.com/en-us/windows-hardware/manufacture/desktop/dism/dism-api-functions?redirectedfrom=MSDN&view=windows-11 
        // ManagedDsim : https://github.com/jeffkl/ManagedDism
        // PowerShell: get-windowsoptionalfeature -online -featurename Microsoft-Windows-Subsystem-Linux
        // => choosing the PowerShell alternative :
        // https://docs.microsoft.com/en-us/powershell/module/dism/get-windowsoptionalfeature?view=windowsserver2022-ps
        // https://docs.microsoft.com/en-us/powershell/module/dism/enable-windowsoptionalfeature?view=windowsserver2022-ps
        // https://docs.microsoft.com/en-us/powershell/module/dism/disable-windowsoptionalfeature?view=windowsserver2022-ps 

        public static bool IsWindowsSubsystemForLinuxEnabled()
        {
            return true;
        }

        public static bool EnableWindowsSubsystemForLinux()
        {
            return true;
        }

        public static bool DisableWindowsSubsystemForLinux()
        {
            return true;
        }

        public static bool IsVirtualMachinePlatformEnabled()
        {
            return true;
        }

        public static bool EnableVirtualMachinePlatform()
        {
            return true;
        }

        public static bool DisableVirtualMachinePlatform()
        {
            return true;
        }
    }
}
