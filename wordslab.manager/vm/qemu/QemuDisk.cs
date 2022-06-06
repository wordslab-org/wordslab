using System.Text.RegularExpressions;
using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm.qemu
{
    public class QemuDisk : VirtualDisk
    {
        public static List<string> ListVMNamesFromOsDisks(HostStorage storage)
        {
            var vmNames = new List<string>();
            var storagePathTemplate = GetLocalStoragePath("*", VirtualDiskFunction.OS, storage);
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
            var diskStoragePath = GetLocalStoragePath(vmName, function, storage);
            if (File.Exists(diskStoragePath))
            {
                return new QemuDisk(vmName, function, diskStoragePath, Qemu.GetVirtualDiskSizeGB(diskStoragePath), Storage.IsPathOnSSD(diskStoragePath));
            }
            else
            {
                return null;
            }
        }

        private static string GetLocalStoragePath(string vmName, VirtualDiskFunction function, HostStorage storage)
        {
            return VirtualDisk.GetHostStoragePathWithoutExtension(vmName, function, storage) + ".img";
        }

        public static VirtualDisk CreateFromOSImage(string vmName, string osImagePath, int totalSizeGB, HostStorage storage)
        {
            var alreadyExistingDisk = TryFindByName(vmName, VirtualDiskFunction.OS, storage);
            if (alreadyExistingDisk != null) throw new ArgumentException($"A virtual OS disk already exists for virtual machine {vmName}");

            var diskStoragePath = GetLocalStoragePath(vmName, VirtualDiskFunction.OS, storage);
            var scriptsPath = GetLinuxScriptsPath(storage);
            Qemu.CreateVirtualDiskFromOsImageWithCloudInit(diskStoragePath, osImagePath, Path.Combine(scriptsPath, metadataFile), Path.Combine(scriptsPath, userdataFile));

            return TryFindByName(vmName, VirtualDiskFunction.OS, storage);
        }

        private static string GetLinuxScriptsPath(HostStorage storage)
        {
            return Path.Combine(storage.DownloadCacheDirectory, "scripts", "linux");
        }

        public static void InstallK3sOnVirtualMachine(VirtualMachineEndpoint vmEndpoint, HostStorage storage)
        {
            SshClient.ImportKnownHostOnLinuxClient(vmEndpoint.IPAddress, vmEndpoint.SSHPort);

            SshClient.CopyFileToRemoteMachine(Path.Combine(storage.DownloadCacheDirectory, VirtualMachine.k3sExecutableFileName), "ubuntu", vmEndpoint.IPAddress, vmEndpoint.SSHPort, $"~/{VirtualMachine.k3sExecutableFileName}");
            SshClient.CopyFileToRemoteMachine(Path.Combine(storage.DownloadCacheDirectory, VirtualMachine.k3sImagesFileName), "ubuntu", vmEndpoint.IPAddress, vmEndpoint.SSHPort, $"~/{VirtualMachine.k3sImagesFileName}");
            SshClient.CopyFileToRemoteMachine(Path.Combine(storage.DownloadCacheDirectory, VirtualMachine.helmFileName), "ubuntu", vmEndpoint.IPAddress, vmEndpoint.SSHPort, $"~/{VirtualMachine.helmFileName}");

            var scriptsPath = GetLinuxScriptsPath(storage);
            SshClient.CopyFileToRemoteMachine(Path.Combine(scriptsPath, k3sInstallScript), "ubuntu", vmEndpoint.IPAddress, vmEndpoint.SSHPort, $"~/{k3sInstallScript}");
            SshClient.ExecuteRemoteCommand("ubuntu", vmEndpoint.IPAddress, vmEndpoint.SSHPort, $"chmod a+x {k3sInstallScript}");
            SshClient.CopyFileToRemoteMachine(Path.Combine(scriptsPath, k3sStartupScript), "ubuntu", vmEndpoint.IPAddress, vmEndpoint.SSHPort, $"~/{k3sStartupScript}");
            SshClient.ExecuteRemoteCommand("ubuntu", vmEndpoint.IPAddress, vmEndpoint.SSHPort, $"chmod a+x {k3sStartupScript}");

            SshClient.ExecuteRemoteCommand("ubuntu", vmEndpoint.IPAddress, vmEndpoint.SSHPort, $"sudo ./{k3sInstallScript}");
        }

        public static VirtualDisk CreateBlank(string vmName, VirtualDiskFunction function, int totalSizeGB, HostStorage storage)
        {
            var alreadyExistingDisk = TryFindByName(vmName, function, storage);
            if (alreadyExistingDisk != null) throw new ArgumentException($"A virtual {function} disk already exists for virtual machine {vmName}");

            var diskStoragePath = GetLocalStoragePath(vmName, VirtualDiskFunction.OS, storage);
            Qemu.CreateVirtualDisk(diskStoragePath, totalSizeGB);

            return TryFindByName(vmName, function, storage);
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

        // Versions last updated : January 9 2022

        // Ubuntu cloud images: https://cloud-images.ubuntu.com/minimal/releases/
        internal static readonly string ubuntuRelease = "focal";
        internal static readonly string ubuntuReleaseNum = "20.04";
        internal static readonly string ubuntuVersion = "20220201";
        internal static readonly string ubuntuImageURL = $"https://cloud-images.ubuntu.com/minimal/releases/{ubuntuRelease}/release-{ubuntuVersion}/ubuntu-{ubuntuReleaseNum}-minimal-cloudimg-amd64.img";
        internal static readonly int ubuntuImageSize = 258473984;
        internal static readonly string ubuntuFileName = $"ubuntu-{ubuntuRelease}-{ubuntuVersion}-cloud.img";

        // --- wordslab virtual machine scripts ---

        internal static readonly string metadataFile = "meta-data.yaml";
        internal static readonly string userdataFile = "user-data.yaml";

        internal static readonly string k3sInstallScript = "wordslab-k3s-install.sh";
        internal static readonly string k3sStartupScript = "wordslab-k3s-start.sh";
    }
}
