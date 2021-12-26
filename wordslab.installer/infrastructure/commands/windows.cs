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
        public static bool IsOSArchitectureX64()
        {
            return RuntimeInformation.OSArchitecture == Architecture.X64;
        }

        internal static Version GetOSVersion()
        {
            return Environment.OSVersion.Version;
        }

        public static bool IsWindows10Version1903OrHigher()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var targetVersion = new Version(10, 0, 18362);
                return GetOSVersion() >= targetVersion;
            }
            else { return false; }
        }

        public static bool IsWindows10Version21H2OrHigher()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var targetVersion = new Version(10, 0, 19044);
                return GetOSVersion() >= targetVersion;
            }
            else { return false; }
        }

        public static bool IsWindows11Version21HOrHigher()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var targetVersion = new Version(11, 0, 22000);
                return Environment.OSVersion.Version >= targetVersion;
            }
            else { return false; }
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
