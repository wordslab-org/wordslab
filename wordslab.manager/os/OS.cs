using System.Net;
using System.Runtime.InteropServices;

namespace wordslab.manager.os
{
    public class OS
    {
        public static string GetMachineName()
        {
            return Dns.GetHostName();
        }

        public static string GetOSName()
        {
            return RuntimeInformation.OSDescription;
        }

        public static Version GetOSVersion()
        {
            return Environment.OSVersion.Version;
        }

        public static bool IsOSArchitectureX64()
        {
            return RuntimeInformation.OSArchitecture == Architecture.X64;
        }

        public static bool IsVirtualizationEnabled()
        {
            return false;
        }

        public static bool IsKernelBasedHypervisorAvailable()
        {
            return false;
        }
    }
}
