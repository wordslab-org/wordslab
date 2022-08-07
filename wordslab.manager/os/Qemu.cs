using System.Diagnostics;
using System.Text.RegularExpressions;

namespace wordslab.manager.os
{
    // QEMU
    // A generic and open source machine emulator and virtualizer
    // https://www.qemu.org/
    public static class Qemu
    {
        public const string QEMUEXE = "qemu-system-x86_64";
        public const string QEMUIMG = "qemu-img";

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

        public static void CreateVirtualDisk(string diskFilePath, int maxSizeGB)
        {
            Command.Run(QEMUIMG, $"create -f qcow2 {diskFilePath} {maxSizeGB}G");
        }
        
        public static void CreateVirtualDiskFromOsImageWithCloudInit(string osDiskFilePath, string osImagePath, string metadataFilePath, string userdataTemplatePath)
        {
            string osDiskFileDir = Directory.GetParent(osDiskFilePath).FullName;

            string tmpDir = null;
            Command.Run("mktemp", "-d /tmp/wordslab.XXXXXX", outputHandler: output => tmpDir = output);
            
            string sshPublicKey = SshClient.GetPublicKeyForCurrentUser();

            var userdataTemplate = File.ReadAllText(userdataTemplatePath);
            var userdataContent = userdataTemplate.Replace("$(cat ~/.ssh/id_rsa.pub)", sshPublicKey);
            File.WriteAllText(Path.Combine(tmpDir, "user-data"), userdataContent);

            Command.Run(GetCDRomToolExe(), $"-output {osDiskFileDir}/cloudinit.img -volid cidata -joliet -rock {metadataFilePath} {tmpDir}/user-data");

            Command.Run("rm", $"-Rf {tmpDir}");

            Command.Run(QEMUIMG, $"create -F qcow2 -b {osImagePath} -f qcow2 {osDiskFilePath}");
        }

        public static int GetVirtualDiskSizeGB(string diskFilePath)
        {
            string sizeStr = null;
            var outputParser = Command.Output.GetValue("virtual size:.*\\((\\d+)\\sbytes\\)", value => sizeStr = value);
            Command.Run(QEMUIMG, $"info {diskFilePath}", outputHandler: outputParser.Run);

            if (String.IsNullOrEmpty(sizeStr))
            {
                throw new Exception($"Failed to get virtual disk size for file: {diskFilePath}");
            }
            else
            {
                return (int)(long.Parse(sizeStr)/(1024*1024*1024));
            }
        }

        // 4. Run Qemu virtual machine

        public static int StartVirtualMachine(int processors, int memoryGB,
            string osDiskFilePath, string clusterDiskFilePath, string dataDiskFilePath,
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

            string osDiskFileDir = Directory.GetParent(osDiskFilePath).FullName;

            var pid = Command.LaunchAndForget(QEMUEXE, $"-machine accel={accelParam},type=q35 -cpu host -smp {processors} -m {memoryGB}G -nographic " +
                        $"-device virtio-net-pci,netdev=net0 -netdev user,id=net0,hostfwd=tcp::{sshForwardPort}-:22,hostfwd=tcp::{httpForwardPort}-:80,hostfwd=tcp::{kubernetesForwardPort}-:6443 " +
                        $"-drive if=virtio,format=qcow2,file={osDiskFilePath} -drive if=virtio,format=raw,file={osDiskFileDir}/cloudinit.img " +
                        $"-drive if=virtio,format=qcow2,file={clusterDiskFilePath} " +
                        $"-drive if=virtio,format=qcow2,file={dataDiskFilePath}", showWindow: false);
            return pid;
        }

        public class QemuProcessProperties
        {
            public int PID;
            public int Processors;
            public int MemoryGB;
            public string OsDiskFilePath;
            public string ClusterDiskFilePath;
            public string DataDiskFilePath;
            public int sshForwardPort;
            public int httpForwardPort;
            public int kubernetesForwardPort;
        }

        public static QemuProcessProperties TryFindVirtualMachineProcess(string osDiskFilePath)
        {
            string strPID = null;
            string strCMD = null;
            Command.Output.GetValue($"\\s*(\\d+)\\s+R\\s+.*{osDiskFilePath}.*", value => strPID = value);
            Command.Output.GetValue($"\\s*\\d+\\s+R\\s+(.*{osDiskFilePath}.*)", value => strCMD = value);   
            Command.Run("ps", $"-C {QEMUEXE} -o pid,state,command --no-headers -w -w", outputHandler: Command.Output.Run);

            if (!String.IsNullOrEmpty(strPID))
            {
                var process = new QemuProcessProperties();
                process.PID = Int32.Parse(strPID);

                var qemuCommandRegex = new Regex("-smp\\s(\\d+).*-m\\s(\\d+)G.*tcp::(\\d+)-:22.*tcp::(\\d+)-:80.*tcp::(\\d+)-:6443.*-drive if=virtio,format=qcow2,file=([^\\s]+) -drive if=virtio,format=raw,file=[^\\s]+/cloudinit.img -drive if=virtio,format=qcow2,file=([^\\s]+) -drive if=virtio,format=qcow2,file=([^\\s]+)");
                var match = qemuCommandRegex.Match(strCMD);
                if (match.Success)
                {
                    process.Processors = Int32.Parse(match.Groups[1].Value);
                    process.MemoryGB = Int32.Parse(match.Groups[2].Value);
                    process.sshForwardPort = Int32.Parse(match.Groups[3].Value);
                    process.httpForwardPort = Int32.Parse(match.Groups[4].Value);
                    process.kubernetesForwardPort = Int32.Parse(match.Groups[5].Value);
                    process.OsDiskFilePath = match.Groups[6].Value;
                    process.ClusterDiskFilePath = match.Groups[7].Value;
                    process.DataDiskFilePath = match.Groups[8].Value;
                    return process;
                }
            }
            return null;
        }

        public static bool StopVirtualMachine(int pid)
        {
            var vmProcess = Process.GetProcessById(pid);

            // https://stackoverflow.com/questions/283128/how-do-i-send-ctrlc-to-a-process-in-c
            // Send Ctrl-C
            vmProcess.StandardInput.WriteLine("\x3");
            vmProcess.StandardOutput.ReadToEnd();
            return vmProcess.HasExited;
        }
    }
}
