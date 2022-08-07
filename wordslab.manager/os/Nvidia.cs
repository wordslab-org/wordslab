using System.Runtime.InteropServices;

namespace wordslab.manager.os
{
    // Checking GPU requirements
    public class Nvidia
    {        
        // nvidia-smi --query-gpu=driver_version --format=csv,noheader
        // > 496.76
        public static Version GetDriverVersion()
        {
            try
            {
                string version = "";
                Command.Run("nvidia-smi", "--query-gpu=driver_version --format=csv,noheader", outputHandler: s => version = s);
                return new Version(version);
            }
            catch (Exception)
            {
                // No Nvidia driver installed
                return null;
            }
        }

        public static bool IsNvidiaDriverForWindows20Sep21OrLater(Version driverVersion)
        {
            if(driverVersion != null)
            {
                var targetVersion = new Version(472, 12);
                return driverVersion >= targetVersion;
            }
            else { return false; }
        }

        public static bool IsNvidiaDriverForWindows16Nov21OrLater(Version driverVersion)
        {
            if (driverVersion != null)
            {
                var targetVersion = new Version(496, 76);
                return driverVersion >= targetVersion;
            }
            else { return false; }
        }
        
        public static bool IsNvidiaDriverForLinux13Dec21OrLater(Version driverVersion)
        {
            if (driverVersion != null)
            {
                var targetVersion = new Version(495, 46);
                return driverVersion >= targetVersion;
            }
            else { return false; }
        }

        public static void TryOpenNvidiaUpdateOnWindows()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Command.LaunchAndForget(@"C:\Program Files\NVIDIA Corporation\NVIDIA GeForce Experience\NVIDIA GeForce Experience.exe");
            }
        }        
    }
}
