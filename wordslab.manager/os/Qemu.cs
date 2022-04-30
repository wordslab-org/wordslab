namespace wordslab.manager.os
{
    // QEMU
    // A generic and open source machine emulator and virtualizer
    // https://www.qemu.org/
    public static class Qemu
    {
        public const string QEMUEXE = "qemu-system-x86_64";

        // 0. Check if Qemu is already installed

        public static bool IsInstalled()
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

        private static string GetCDRomToolExe()
        {
            string cdRomTool = null;
            if (OS.IsLinux)
            {
                cdRomTool = "genisoimage";
            }
            else if (OS.IsMacOS)
            {
                cdRomTool = "mkisofs";
            }
            return cdRomTool;
        }

        public static bool IsCDRomToolInstalled()
        {
            var cdRomTool = GetCDRomToolExe();

            bool isInstalled = false;            
            Command.Run(cdRomTool, "-version", exitCodeHandler: exitCode => isInstalled = exitCode == 0);
            return isInstalled;
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

        public static Version Install()
        {
            // On Linux, this will work only if wordslab is run as admin
            if (OS.IsLinux)
            {
                Command.Run("apt", "update -y", mustRunAsAdmin: true);
                Command.Run("apt", "install -y genisoimage qemu qemu-utils qemu-kvm", mustRunAsAdmin: true);
            }
            // No need to be admin when using Howebrew on macOS
            else if(OS.IsMacOS)
            {
                Command.Run("brew", "install cdrtools");
                Command.Run("brew", "install qemu");
            }
            return GetInstalledVersion();
        }

        public static void InstallCDRomTool()
        {
            // On Linux, this will work only if wordslab is run as admin
            if (OS.IsLinux)
            {
                Command.Run("apt", "update -y", mustRunAsAdmin: true);
                Command.Run("apt", "install -y genisoimage", mustRunAsAdmin: true);
            }
            // No need to be admin when using Howebrew on macOS
            else if (OS.IsMacOS)
            {
                Command.Run("brew", "install cdrtools");
            }
        }

        public static string GetLinuxInstallCommand()
        {
            return "sudo apt update -y && sudo apt install -y genisoimage qemu qemu-utils qemu-kvm";
        }

        // 3. Create virtual disks

        public static void CreateVirtualDisk(string diskFilePathWithoutExt, int maxSizeGB)
        {
            Command.Run("qemu-img", $"create -f qcow2 {diskFilePathWithoutExt}.img {maxSizeGB}G");
        }
        
        public static void CreateVirtualDiskFromOsImageWithCloudInit(string osDiskFilePathWithoutExt, string osImagePath, string metadataFilePath, string userdataTemplatePath)
        {
            string osDiskFileDir = Directory.GetParent(osDiskFilePathWithoutExt).FullName;

            string tmpDir = null;
            Command.Run("mktemp", "-d /tmp/wordslab.XXXXXX", outputHandler: output => tmpDir = output);
            
            string sshPublicKey = SshClient.GetPublicKeyForCurrentUser();

            var userdataTemplate = File.ReadAllText(userdataTemplatePath);
            var userdataContent = userdataTemplate.Replace("$(cat ~/.ssh/id_rsa.pub)", sshPublicKey);
            File.WriteAllText(Path.Combine(tmpDir, "user-data"), userdataContent);

            Command.Run(GetCDRomToolExe(), $"-output {osDiskFileDir}/cloudinit.img -volid cidata -joliet -rock {metadataFilePath} {tmpDir}/user-data");

            Command.Run("rm", $"-Rf {tmpDir}");

            Command.Run($"qemu-img", $"create -F qcow2 -b {osImagePath} -f qcow2 {osDiskFilePathWithoutExt}.img");
        }

        // 4. Run Qemu virtual machine

        public static void StartVirtualMachine(int processors, int memoryGB,
            string osDiskFilePathWithoutExt, string clusterDiskFilePathWithoutExt, string dataDiskFilePathWithoutExt,
            int sshForwardPort=3022, int httpForwardPort=3080, int kubernetesForwardPort=3443)
        {
            string accelParam = null;
            if(OS.IsLinux)
            {
                accelParam = "kvm";
            }
            else if(OS.IsMacOS)
            {
                accelParam = "hvf";
            }

            string osDiskFileDir = Directory.GetParent(osDiskFilePathWithoutExt).FullName;

            Command.Run(QEMUEXE, $"-machine accel={accelParam},type=q35 -cpu host -smp {processors} -m {memoryGB}G -nographic " +
                                 $"-device virtio-net-pci,netdev=net0 -netdev user,id=net0,hostfwd=tcp::{sshForwardPort}-:22,hostfwd=tcp::{httpForwardPort}-:80,hostfwd=tcp::{kubernetesForwardPort}-:6443 " +
                                 $"-drive if=virtio,format=qcow2,file={osDiskFilePathWithoutExt}.img -drive if=virtio,format=raw,file={osDiskFileDir}/cloudinit.img " +
                                 $"-drive if=virtio,format=qcow2,file={clusterDiskFilePathWithoutExt}.img " +
                                 $"-drive if=virtio,format=qcow2,file={dataDiskFilePathWithoutExt}.img");  
        }
    }
}
