using System.Runtime.InteropServices;

namespace wordslab.installer.infrastructure.commands
{
    public class windows10
    {
        public static bool IsOSArchitectureX64()
        {
            return RuntimeInformation.OSArchitecture == Architecture.X64;
        }

        public static Version GetOSVersion()
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

        public static void OpenWindowsUpdate()
        {
            Command.LaunchAndForget("control.exe", "update");
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
            string? hyperVisorPresent = null;
            string? hyperVRequirementDataExecutionPreventionAvailable = null;
            string? hyperVRequirementSecondLevelAddressTranslation = null;
            string? hyperVRequirementVirtualizationFirmwareEnabled = null;
            string? hyperVRequirementVMMonitorModeExtensions = null;
            var outputParser = Command.Output.GetValue(@"HyperVisorPresent\s+:\s+(?<isenabled>[a-zA-Z]+)\s*$", s => hyperVisorPresent = s).
                                                GetValue(@"HyperVRequirementDataExecutionPreventionAvailable\s+:\s+(?<isenabled>[a-zA-Z]+)\s*$", s => hyperVRequirementDataExecutionPreventionAvailable = s).
                                                GetValue(@"HyperVRequirementSecondLevelAddressTranslation\s+:\s+(?<isenabled>[a-zA-Z]+)\s*$", s => hyperVRequirementSecondLevelAddressTranslation = s).
                                                GetValue(@"HyperVRequirementVirtualizationFirmwareEnabled\s+:\s+(?<isenabled>[a-zA-Z]+)\s*$", s => hyperVRequirementVirtualizationFirmwareEnabled = s).
                                                GetValue(@"HyperVRequirementVMMonitorModeExtensions\s+:\s+(?<isenabled>[a-zA-Z]+)\s*$", s => hyperVRequirementVMMonitorModeExtensions = s);

            Command.Run("powershell.exe", "Get-ComputerInfo -Property \"HyperV*\"", outputHandler: outputParser.Run);

            bool isEnabled = String.Equals(hyperVisorPresent, "true", StringComparison.InvariantCultureIgnoreCase);
            if (!isEnabled)
            {
                isEnabled = String.Equals(hyperVRequirementDataExecutionPreventionAvailable, "true", StringComparison.InvariantCultureIgnoreCase) &&
                            String.Equals(hyperVRequirementSecondLevelAddressTranslation, "true", StringComparison.InvariantCultureIgnoreCase) &&
                            String.Equals(hyperVRequirementVirtualizationFirmwareEnabled, "true", StringComparison.InvariantCultureIgnoreCase) &&
                            String.Equals(hyperVRequirementVMMonitorModeExtensions, "true", StringComparison.InvariantCultureIgnoreCase);
            }
            return isEnabled;
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

        private static readonly string SCRIPT_PATH;
        private static readonly string LOGS_PATH;

        static windows10() 
        {
            SCRIPT_PATH = @"c:\tmp";
            LOGS_PATH = @"c:\tmp";
        }

        public static bool IsWindowsSubsystemForLinuxEnabled()
        {
            bool isEnabled = true;
            Command.ExecuteShellScriptAsAdmin(Path.Combine(SCRIPT_PATH,"check-wsl.ps1"), "", LOGS_PATH, exitCodeHandler: c => isEnabled = (c == 0));
            return isEnabled;
        }

        public static bool EnableWindowsSubsystemForLinux()
        {
            int exitCode = -1;
            Command.ExecuteShellScriptAsAdmin(Path.Combine(SCRIPT_PATH, "enable-wsl.ps1"), "", LOGS_PATH, exitCodeHandler: c => exitCode = c);

            bool restartNeeded = false;
            if(exitCode == 0)       { restartNeeded = false; }
            else if (exitCode == 1) { restartNeeded = true; }
            else                    { throw new InvalidOperationException($"Failed to enable Windows Subsystem for Linux : see script log file in {LOGS_PATH}"); }
            return restartNeeded;
        }

        public static bool DisableWindowsSubsystemForLinux()
        {
            int exitCode = -1;
            Command.ExecuteShellScriptAsAdmin(Path.Combine(SCRIPT_PATH, "disable-wsl.ps1"), "", LOGS_PATH, exitCodeHandler: c => exitCode = c);

            bool restartNeeded = false;
            if (exitCode == 0) { restartNeeded = false; }
            else if (exitCode == 1) { restartNeeded = true; }
            else { throw new InvalidOperationException($"Failed to disable Windows Subsystem for Linux : see script log file in {LOGS_PATH}"); }
            return restartNeeded;
        }

        public static bool IsVirtualMachinePlatformEnabled()
        {
            bool isEnabled = true;
            Command.ExecuteShellScriptAsAdmin(Path.Combine(SCRIPT_PATH, "check-vmd.ps1"), "", LOGS_PATH, exitCodeHandler: c => isEnabled = (c == 0));
            return isEnabled;
        }

        public static bool EnableVirtualMachinePlatform()
        {
            int exitCode = -1;
            Command.ExecuteShellScriptAsAdmin(Path.Combine(SCRIPT_PATH, "enable-vmd.ps1"), "", LOGS_PATH, exitCodeHandler: c => exitCode = c);

            bool restartNeeded = false;
            if (exitCode == 0) { restartNeeded = false; }
            else if (exitCode == 1) { restartNeeded = true; }
            else { throw new InvalidOperationException($"Failed to enable Windows Virtual Machine Platform : see script log file in {LOGS_PATH}"); }
            return restartNeeded;
        }

        public static bool DisableVirtualMachinePlatform()
        {
            int exitCode = -1;
            Command.ExecuteShellScriptAsAdmin(Path.Combine(SCRIPT_PATH, "disable-vmd.ps1"), "", LOGS_PATH, exitCodeHandler: c => exitCode = c);

            bool restartNeeded = false;
            if (exitCode == 0) { restartNeeded = false; }
            else if (exitCode == 1) { restartNeeded = true; }
            else { throw new InvalidOperationException($"Failed to disable Windows Virtual Machine Platform : see script log file in {LOGS_PATH}"); }
            return restartNeeded;
        }

        public static void ShutdownAndRestart()
        {
            Command.Run("shutdown.exe", "-r -t 0");
        }
    }
}
