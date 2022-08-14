using System.Runtime.InteropServices;

namespace wordslab.manager.os
{
    public class Windows
    {
        public static bool IsWindows10Version1903OrHigher()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var targetVersion = new Version(10, 0, 18362);
                return OS.GetOSVersion() >= targetVersion;
            }
            else { return false; }
        }

        public static bool IsWindows10Version21H2OrHigher()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var targetVersion = new Version(10, 0, 19044);
                return OS.GetOSVersion() >= targetVersion;
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

        // Managing Windows Optional features : several alternatives 
        // dism.exe /online /disable-feature /featurename:VirtualMachinePlatform  /norestart
        // https://docs.microsoft.com/en-us/windows-hardware/manufacture/desktop/dism/dism-api-functions?redirectedfrom=MSDN&view=windows-11 
        // ManagedDsim : https://github.com/jeffkl/ManagedDism
        // PowerShell: get-windowsoptionalfeature -online -featurename Microsoft-Windows-Subsystem-Linux
        // => choosing the PowerShell alternative :
        // https://docs.microsoft.com/en-us/powershell/module/dism/get-windowsoptionalfeature?view=windowsserver2022-ps
        // https://docs.microsoft.com/en-us/powershell/module/dism/enable-windowsoptionalfeature?view=windowsserver2022-ps
        // https://docs.microsoft.com/en-us/powershell/module/dism/disable-windowsoptionalfeature?view=windowsserver2022-ps 

        public static string IsWindowsSubsystemForLinuxEnabled_script(string scriptsDirectory)
        {
            return Command.GetScriptContent(Path.Combine(scriptsDirectory, "os", "Windows"), "check-wsl.ps1");
        }

        public static bool IsWindowsSubsystemForLinuxEnabled(string scriptsDirectory, string logsDirectory)
        {
            bool isEnabled = true;
            Command.ExecuteShellScript(Path.Combine(scriptsDirectory, "os", "Windows"), "check-wsl.ps1", "", logsDirectory, runAsAdmin: true, exitCodeHandler: c => isEnabled = (c == 0));
            return isEnabled;
        }

        public static string EnableWindowsSubsystemForLinux_script(string scriptsDirectory)
        {
            return Command.GetScriptContent(Path.Combine(scriptsDirectory, "os", "Windows"), "enable-wsl.ps1");
        }

        public static bool EnableWindowsSubsystemForLinux(string scriptsDirectory, string logsDirectory)
        {
            int exitCode = -1;
            Command.ExecuteShellScript(Path.Combine(scriptsDirectory, "os", "Windows"), "enable-wsl.ps1", "", logsDirectory, runAsAdmin: true, exitCodeHandler: c => exitCode = c);

            bool restartNeeded = false;
            if(exitCode == 0)       { restartNeeded = false; }
            else if (exitCode == 1) { restartNeeded = true; }
            else                    { throw new InvalidOperationException($"Failed to enable Windows Subsystem for Linux : see script log file in {logsDirectory}"); }
            return restartNeeded;
        }

        public static string DisableWindowsSubsystemForLinux_script(string scriptsDirectory)
        {
            return Command.GetScriptContent(Path.Combine(scriptsDirectory, "os", "Windows"), "disable-wsl.ps1");
        }

        public static bool DisableWindowsSubsystemForLinux(string scriptsDirectory, string logsDirectory)
        {
            int exitCode = -1;
            Command.ExecuteShellScript(Path.Combine(scriptsDirectory, "os", "Windows"), "disable-wsl.ps1", "", logsDirectory, runAsAdmin: true, exitCodeHandler: c => exitCode = c);

            bool restartNeeded = false;
            if (exitCode == 0) { restartNeeded = false; }
            else if (exitCode == 1) { restartNeeded = true; }
            else { throw new InvalidOperationException($"Failed to disable Windows Subsystem for Linux : see script log file in {logsDirectory}"); }
            return restartNeeded;
        }

        public static void ShutdownAndRestart()
        {
            Command.Run("shutdown.exe", "-r -t 0");
        }
    }
}
