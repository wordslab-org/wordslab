using System.Text.RegularExpressions;
using wordslab.manager.config;
using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm.qemu
{
    public class QemuDisk : VirtualDisk
    {
        public static List<string> ListVMNamesFromClusterDisks(HostStorage storage)
        {
            var vmNames = new List<string>();
            var storagePathTemplate = GetHostStorageFile("*", VirtualDiskFunction.Cluster, storage);
            var filenames = Directory.GetFiles(Path.GetDirectoryName(storagePathTemplate), Path.GetFileName(storagePathTemplate));
            var vmNameRegex = new Regex(storagePathTemplate.Replace("\\", "\\\\").Replace("*", "(.+)"));
            foreach (var filename in filenames)
            {
                vmNames.Add(vmNameRegex.Match(filename).Groups[1].Value);
            }
            return vmNames;
        }

        public static VirtualDisk TryFindByName(string vmName, VirtualDiskFunction function, HostStorage storage)
        {
            var diskStoragePath = GetHostStorageFile(vmName, function, storage);
            if (File.Exists(diskStoragePath))
            {
                return new QemuDisk(vmName, function, diskStoragePath, Qemu.GetVirtualDiskSizeGB(diskStoragePath), Storage.IsPathOnSSD(diskStoragePath));
            }
            else
            {
                return null;
            }
        }

        private static string GetHostStorageFile(string vmName, VirtualDiskFunction function, HostStorage storage)
        {
            return VirtualDisk.GetHostStorageDirectory(vmName, function, storage) + ".img";
        }

        public static VirtualDisk CreateFromOSImage(string vmName, string osImagePath, int totalSizeGB, HostStorage storage)
        {
            var alreadyExistingDisk = TryFindByName(vmName, VirtualDiskFunction.Cluster, storage);
            if (alreadyExistingDisk != null) throw new ArgumentException($"A virtual cluster disk already exists for virtual machine {vmName}");

            var diskStoragePath = GetHostStorageFile(vmName, VirtualDiskFunction.Cluster, storage);
            var scriptsPath = GetLinuxScriptsPath(storage);
            Qemu.CreateVirtualDiskFromOsImageWithCloudInit(diskStoragePath, osImagePath, Path.Combine(scriptsPath, metadataFile), Path.Combine(scriptsPath, userdataFile));

            return TryFindByName(vmName, VirtualDiskFunction.Cluster, storage);
        }

        private static string GetLinuxScriptsPath(HostStorage storage)
        {
            return Path.Combine(storage.DownloadCacheDirectory, "scripts", "linux");
        }

        public static void InstallK3sOnVirtualMachine(VirtualMachineInstance vmInstance, HostStorage storage)
        {
            var ip = vmInstance.VmIPAddress;
            var sshPort = vmInstance.Config.HostSSHPort;

            SshClient.ImportKnownHostOnClient(ip, sshPort);

            SshClient.CopyFileToRemoteMachine(Path.Combine(storage.DownloadCacheDirectory, VirtualMachine.k3sExecutableFileName), "ubuntu", ip, sshPort, $"~/{VirtualMachine.k3sExecutableFileName}");
            SshClient.CopyFileToRemoteMachine(Path.Combine(storage.DownloadCacheDirectory, VirtualMachine.k3sImagesFileName), "ubuntu", ip, sshPort, $"~/{VirtualMachine.k3sImagesFileName}");
            SshClient.CopyFileToRemoteMachine(Path.Combine(storage.DownloadCacheDirectory, VirtualMachine.helmFileName), "ubuntu", ip, sshPort, $"~/{VirtualMachine.helmFileName}");

            var scriptsPath = GetLinuxScriptsPath(storage);
            SshClient.CopyFileToRemoteMachine(Path.Combine(scriptsPath, k3sInstallScript), "ubuntu", ip, sshPort, $"~/{k3sInstallScript}");
            SshClient.ExecuteRemoteCommand("ubuntu", ip, sshPort, $"chmod a+x {k3sInstallScript}");
            SshClient.CopyFileToRemoteMachine(Path.Combine(scriptsPath, k3sStartupScript), "ubuntu", ip, sshPort, $"~/{k3sStartupScript}");
            SshClient.ExecuteRemoteCommand("ubuntu", ip, sshPort, $"chmod a+x {k3sStartupScript}");

            SshClient.ExecuteRemoteCommand("ubuntu", ip, sshPort, $"sudo ./{k3sInstallScript}");
        }

        public static VirtualDisk CreateBlank(string vmName, int totalSizeGB, HostStorage storage)
        {
            var alreadyExistingDisk = TryFindByName(vmName, VirtualDiskFunction.Data, storage);
            if (alreadyExistingDisk != null) throw new ArgumentException($"A virtual data disk already exists for virtual machine {vmName}");

            var diskStoragePath = GetHostStorageFile(vmName, VirtualDiskFunction.Data, storage);
            Qemu.CreateVirtualDisk(diskStoragePath, totalSizeGB);

            return TryFindByName(vmName, VirtualDiskFunction.Data, storage);
        }

        private QemuDisk(string vmName, VirtualDiskFunction function, string storagePath, int totalSizeGB, bool isSSD) :
            base(vmName, function, storagePath, totalSizeGB, isSSD)
        { }

        public override void Resize(int totalSizeGB)
        {
            throw new NotImplementedException();
        }

        public override void Delete()
        {
            if (File.Exists(StoragePath))
            {
                File.Delete(StoragePath);
            }
        }

        public override bool IsServiceRequired()
        {
            return false;
        }

        public override void StartService()
        { }

        public override bool IsServiceRunnig()
        {
            return true;
        }

        public override void StopService()
        { }

        // --- wordslab virtual machine software ---

        // Versions last updated : August 16 2022

        // Ubuntu cloud images: https://cloud-images.ubuntu.com/minimal/releases/
        internal static readonly string ubuntuRelease = "focal";
        internal static readonly string ubuntuReleaseNum = "20.04";
        internal static readonly string ubuntuVersion = "20220810";
        internal static readonly string ubuntuImageURL = $"https://cloud-images.ubuntu.com/minimal/releases/{ubuntuRelease}/release-{ubuntuVersion}/ubuntu-{ubuntuReleaseNum}-minimal-cloudimg-amd64.img";
        internal static readonly int ubuntuImageSize = 264962048;
        internal static readonly string ubuntuFileName = $"ubuntu-{ubuntuRelease}-{ubuntuVersion}-cloud.img";

        // --- wordslab virtual machine scripts ---

        internal static readonly string metadataFile = "meta-data.yaml";
        internal static readonly string userdataFile = "user-data.yaml";

        internal static readonly string k3sInstallScript = "wordslab-k3s-install.sh";
        internal static readonly string k3sStartupScript = "wordslab-k3s-start.sh";
    }
}
