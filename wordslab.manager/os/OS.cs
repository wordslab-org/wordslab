using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace wordslab.manager.os
{
    public class OS
    {
        public static string GetMachineName()
        {
            return Dns.GetHostName();
        }

        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);    

        public static string GetOSName()
        {
            return RuntimeInformation.OSDescription;
        }

        /// <summary>
        /// Returns OS version on Windows and MacOS.
        /// Returns kernel version on Linux -> call OS.GetLinuxDistribution() if needed.
        /// </summary>
        public static Version GetOSVersion()
        {
            return Environment.OSVersion.Version;
        }

        public class DistributionInfo
        {
            public string Name = "Linux";
            public Version Version = new Version();
        }

        public static DistributionInfo GetLinuxDistributionInfo()
        {
            if (IsLinux)
            {
                DistributionInfo distrib = new DistributionInfo();
                if (File.Exists("/etc/os-release"))
                {
                    foreach (string line in File.ReadAllLines("/etc/os-release"))
                    {
                        if (line.StartsWith("ID=", StringComparison.Ordinal))
                        {
                            distrib.Name = line.Substring(3).Trim('"', '\'');
                        }
                        else if (line.StartsWith("VERSION_ID=", StringComparison.Ordinal))
                        {
                            var versionString = line.Substring(11).Trim('"', '\'');
                            try
                            {
                                if (versionString.IndexOf('.') != -1)
                                {
                                    distrib.Version = new Version(versionString);
                                }
                                else
                                {
                                    distrib.Version = new Version(int.Parse(versionString), 0);
                                }
                            }
                            catch (Exception)
                            {
                                // Failed to parse version string
                            }
                        }
                    }
                }
                return distrib;
            }
            else
            {
                throw new NotSupportedException("This method is only available for Linux");
            }
        }

        public static bool IsOSArchitectureX64()
        {
            return RuntimeInformation.OSArchitecture == Architecture.X64;
        }

        public static bool IsNativeHypervisorAvailable()
        {
            if (IsWindows)
            {
                // Checking HyperV requirements : 
                // powershell.exe Get-ComputerInfo -property "HyperV*"
                //  HyperVisorPresent                                 : True
                //  HyperVRequirementDataExecutionPreventionAvailable : True
                //  HyperVRequirementSecondLevelAddressTranslation    : True
                //  HyperVRequirementVirtualizationFirmwareEnabled    : True
                //  HyperVRequirementVMMonitorModeExtensions          : True
                string? hyperVisorPresent = null;
                string? hyperVRequirementDataExecutionPreventionAvailable = null;
                string? hyperVRequirementSecondLevelAddressTranslation = null;
                string? hyperVRequirementVirtualizationFirmwareEnabled = null;
                string? hyperVRequirementVMMonitorModeExtensions = null;
                var outputParser = Command.Output.GetValue(@"HyperVisorPresent=(?<isenabled>[a-zA-Z]+)", s => hyperVisorPresent = s).
                                                    GetValue(@"HyperVRequirementDataExecutionPreventionAvailable=(?<isenabled>[a-zA-Z]+)", s => hyperVRequirementDataExecutionPreventionAvailable = s).
                                                    GetValue(@"HyperVRequirementSecondLevelAddressTranslation=(?<isenabled>[a-zA-Z]+)", s => hyperVRequirementSecondLevelAddressTranslation = s).
                                                    GetValue(@"HyperVRequirementVirtualizationFirmwareEnabled=(?<isenabled>[a-zA-Z]+)", s => hyperVRequirementVirtualizationFirmwareEnabled = s).
                                                    GetValue(@"HyperVRequirementVMMonitorModeExtensions=(?<isenabled>[a-zA-Z]+)", s => hyperVRequirementVMMonitorModeExtensions = s);

                Command.Run("powershell.exe", "Get-ComputerInfo -Property \"HyperV*\" | Write-Host", outputHandler: outputParser.Run);

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
            else if(IsLinux)
            {
                // From the KVM FAQ
                // -> check that:
                // - the modules are correctly loaded
                //   lsmod| grep kvm
                //      kvm_intel             303104  6
                //      kvm                   864256  1 kvm_intel
                // - you don't have a "KVM: disabled by BIOS" line in the output of dmesg
                // - /dev/kvm exists and you have the correct rights to use it
                //   ls -l /dev/kvm
                //      crw-rw----+ 1 root kvm 10, 232 mars  27 12:09 /dev/kvm
                //      ls: cannot access '/dev/kvm2': No such file or directory

                try
                {
                    var modules = new List<object>();
                    var outputParser = Command.Output.GetList(null, @"(?<module>\w+)\s+\d+", dict => dict["module"], modules);
                    Command.Run("lsmod", outputHandler: outputParser.Run);
                    var kvmKernelModuleIsLoaded = modules.Cast<string>().Where(name => name.Equals("kvm") || name.Equals("kvm_intel") || name.Equals("kvm_amd")).Count() == 2;

                    var kvmErrorMessage = false;
                    var outputParser2 = Command.Output.GetValue("kvm: disabled by bios", _ => kvmErrorMessage = true);
                    Command.Run("dmesg", outputHandler: outputParser2.Run);

                    string accessRights = null;
                    var outputParser3 = Command.Output.GetValue(@"^([^\s]+).*/dev/kvm", value => accessRights = value);
                    Command.Run("ls", "-l /dev/kvm", outputHandler: outputParser3.Run);
                    var devKvmUserAccess = accessRights != null && accessRights.Length > 3 && accessRights.Substring(1, 2) == "rw";

                    return kvmKernelModuleIsLoaded && !kvmErrorMessage && devKvmUserAccess;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else if(IsMacOS)
            {
                // From https://developer.apple.com/documentation/hypervisor
                // At runtime, determine whether the Hypervisor APIs are available on a particular machine with the command
                //    sysctl kern.hv_support
                //          kern.hv_support: 0
                //          kern.hv_support: 1
                // Entitlements
                // All process must have the com.apple.security.hypervisor entitlement to use Hypervisor API.

                var hvSupport = false;
                var outputParser = Command.Output.GetValue(@"kern.hv_support: (\d)", value => hvSupport = (value == "1"));
                Command.Run("sysctl", "kern.hv_support", outputHandler: outputParser.Run);

                return hvSupport;
            }
            else
            {
                throw new NotSupportedException("This method is only available for Windows, Linux and MacOS");
            }
        }

        [DllImport("libc")]
        private static extern uint geteuid();

        public static bool IsRunningAsAdministrator()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            else
            {
                return geteuid() == 0;
            }
        }
    }
}
