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
                return OS.GetOSVersion() >= targetVersion;
            }
            else { return false; }
        }
        
        public static bool IsHomebrewInstalled()
        {
            throw new NotImplementedException();
        }

        public static string GetHomebrewInstallMessage()
        {
            throw new NotImplementedException();
        }
    }
}
