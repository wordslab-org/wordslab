namespace wordslab.manager.os
{
    // QEMU
    // A generic and open source machine emulator and virtualizer
    // https://www.qemu.org/
    public static class Qemu
    {
        public const string QEMUEXE = "qemu-system-x86_64";

        // 0. Check if Qemu is already installed

        public static bool IsAlreadyInstalled()
        {
            return GetInstalledVersion() != null;
        }

        public static Version GetInstalledVersion()
        { 
            string version = null;
            bool notFound = false;
            var outputParser = Command.Output.GetValue("QEMU emulator version (.+)", value => version = value);
            Command.Run(QEMUEXE, "--version", outputHandler: outputParser.Run, exitCodeHandler: c => notFound = (c != 0));

            if(notFound || version == null)
            {
                return null;
            }
            else
            {
                return new Version(version);
            }
        }

        // 1. Check requirements for running Qemu

        public static bool IsOsVersionOKForQemu()
        {
            if( OS.IsOSArchitectureX64())
            { 
                if(OS.IsLinux)
                {
                    return Linux.IsLinuxVersionUbuntu1804OrHigher();
                } 
                else if(OS.IsMacOS)
                {
                    return MacOS.IsMacOSVersionCatalinaOrHigher();
                }                
            }
            return false;
        }

        public static bool IsInstalledVersionSupported()
        {
            // Latest version : 6.2.0
            // Ubuntu 20.04  : 4.2.1
            // Ubuntu 18.04  : 2.11.1
            return GetInstalledVersion() >= new Version(2, 11, 1);
        }

        // 2. Install Qemu

        public static Version InstallQemu()
        {
            // On Linux, this will work only if wordslab is run as admin
            if (OS.IsLinux)
            {
                Command.Run("apt", "update -y", mustRunAsAdmin: true);
                Command.Run("apt", "install -y qemu qemu-utils qemu-kvm", mustRunAsAdmin: true);
            }
            // No need to be admin when using Howebrew on macOS
            else if(OS.IsMacOS)
            {
                Command.Run("brew", "install cdrtools");
                Command.Run("brew", "install qemu");
            }
            return GetInstalledVersion();
        }

        public static string GetQemuLinuxInstallCommand()
        {
            return "sudo apt update -y && sudo apt install -y qemu qemu-utils qemu-kvm";
        }
    }
}
