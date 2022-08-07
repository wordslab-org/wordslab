using System.Runtime.InteropServices;

namespace wordslab.manager.os
{
    public class MacOS
    {
        public static bool IsMacOSVersionCatalinaOrHigher()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var targetVersion = new Version(10,15);
                return OS.IsOSArchitectureX64() && OS.GetOSVersion() >= targetVersion;
            }
            else { return false; }
        }
        
        public static bool IsHomebrewPackageManagerAvailable()
        {
            bool isAvailable = false;
            Command.Run("brew", "--version", exitCodeHandler: code => isAvailable = code == 0);
            return isAvailable;
        }

        // This script requires admin privileges and user interaction
        // - interaction 1 : type sudo password
        // - interaction 2 : press Enter to validate the list of changes which will be applied
        public static string GetHomebrewInstallCommand()
        {
            return "/bin/bash -c \"$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\"";
        }
    }
}
