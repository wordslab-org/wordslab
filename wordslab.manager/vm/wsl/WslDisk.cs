using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm.wsl
{
    public class WslDisk : VirtualDisk
    {
        public static VirtualDisk TryFindByName(string vmName, VirtualDiskFunction function, HostStorage storage)
        {
            var serviceName = GetServiceName(vmName, function);
            var wslDistribs = Wsl.list();
            if (wslDistribs.Any(d => d.Distribution == serviceName))
            {
                var storagePath = GetLocalStoragePath(vmName, function, storage);
                if (File.Exists(storagePath))
                {
                    return new WslDisk(vmName, function, storagePath, Storage.IsPathOnSSD(storagePath), storage);
                }
            }
            return null;
        }

        private static string GetLocalStoragePath(string vmName, VirtualDiskFunction function, HostStorage storage)
        {
            return VirtualDisk.GetHostStoragePathWithoutExtension(vmName, function, storage) + ".vhdx";
        }
                
        public static VirtualDisk CreateFromOSImage(string vmName, string osImagePath, /*int maxSizeGB,*/ HostStorage storage)
        {
            // See below:  public override void Resize(int maxSizeGB)
            // maxSizeGB = 256;

            var alreadyExistingDisk = TryFindByName(vmName, VirtualDiskFunction.OS, storage);
            if (alreadyExistingDisk != null) throw new ArgumentException($"A virtual OS disk already exists for virtual machine {vmName}");

            var serviceName = GetServiceName(vmName, VirtualDiskFunction.OS);
            var storageDirectory = GeHostStorageDirectory(VirtualDiskFunction.OS, storage);

            string cacheDirectory = storage.DownloadCacheDirectory;
            string diskInitScript = GetDiskInitScript(VirtualDiskFunction.OS);

            Wsl.import(serviceName, storageDirectory, osImagePath, 2);
            Wsl.execShell($"cp $(wslpath '{cacheDirectory}')/{diskInitScript} /root/{diskInitScript}", serviceName);
            Wsl.execShell($"chmod a+x /root/{diskInitScript}", serviceName);
            Wsl.execShell($"/root/{diskInitScript} '{cacheDirectory}' '{VirtualMachine.k3sExecutableFileName}' '{VirtualMachine.helmFileName}'", serviceName, ignoreError: "perl: warning");
            Wsl.terminate(serviceName);

            return TryFindByName(vmName, VirtualDiskFunction.OS, storage);
        }

        public bool InstallNvidiaContainerRuntimeOnOSImage(VirtualDisk clusterDisk)
        {
            if (Function == VirtualDiskFunction.OS)
            {
                clusterDisk.StartService();
                Wsl.execShell("nvidia-smi -L", ServiceName);
                Wsl.execShell($"/root/{vmGPUInitScript} '{hostStorage.DownloadCacheDirectory}' '{VirtualMachine.nvidiaContainerRuntimeVersion}'", ServiceName, ignoreError: "perl: warning");
                Wsl.terminate(ServiceName);
                clusterDisk.StopService();
                return true;
            }
            else
            {
                return false;
            }
        }

        public static VirtualDisk CreateBlank(string vmName, VirtualDiskFunction function, /*int maxSizeGB,*/ HostStorage storage)
        {
            // See below:  public override void Resize(int maxSizeGB)
            // maxSizeGB = 256;

            var alreadyExistingDisk = TryFindByName(vmName, function, storage);
            if (alreadyExistingDisk != null) throw new ArgumentException($"A virtual {function} disk already exists for virtual machine {vmName}");

            var serviceName = GetServiceName(vmName, function);
            var storageDirectory = GeHostStorageDirectory(function, storage);

            string cacheDirectory = storage.DownloadCacheDirectory;
            string diskInitScript = GetDiskInitScript(function);

            Wsl.import(serviceName, storageDirectory, Path.Combine(cacheDirectory, alpineFileName), 2);
            Wsl.execShell($"cp $(wslpath '{cacheDirectory}')/{diskInitScript} /root/{diskInitScript}", serviceName);
            Wsl.execShell($"chmod a+x /root/{diskInitScript}", serviceName);
            Wsl.execShell($"/root/{diskInitScript} '{cacheDirectory}'" + (function==VirtualDiskFunction.Cluster?$" '{VirtualMachine.k3sImagesFileName}'":""), serviceName);
            Wsl.terminate(serviceName);

            return TryFindByName(vmName, function, storage);
        }

        private HostStorage hostStorage;

        private WslDisk(string vmName, VirtualDiskFunction function, string storagePath, /*int maxSizeGB,*/ bool isSSD, HostStorage storage) :
            base(vmName, function, storagePath, 256, isSSD)
        {
            // See below:  public override void Resize(int maxSizeGB)
            // MaxSizeGB = 256;

            ServiceName = GetServiceName(vmName, function);
            hostStorage = storage;
        }

        public override void Resize(int maxSizeGB)
        {
            // TO DO : find a way to automate this
            // https://docs.microsoft.com/en-us/windows/wsl/vhd-size
            // https://docs.microsoft.com/en-us/powershell/module/storage/?view=windowsserver2022-ps
            throw new NotImplementedException();
        }

        public override void Delete()
        {
            if (IsServiceRunnig())
            {
                StopService();
            }
            Wsl.unregister(ServiceName);
        }

        public override bool IsServiceRequired()
        {
            return Function != VirtualDiskFunction.OS;
        }

        private string GetDiskStartupScript()
        {
            switch (Function)
            {
                case VirtualDiskFunction.Cluster:
                    return clusterDiskStartupScript;
                case VirtualDiskFunction.Data:
                    return dataDiskStartupScript;
            }
            return null;
        }

        public override void StartService()
        {
            var startupScript = GetDiskStartupScript();
            if (startupScript != null)
            {
                var startupScriptPath = $"/root/{startupScript}";            
                Wsl.execShell(startupScriptPath, ServiceName);
            }
        }

        public override bool IsServiceRunnig()
        {
            var wslDistribs = Wsl.list();
            return wslDistribs.Any(d => d.Distribution == ServiceName && d.IsRunning);
        }

        public override void StopService()
        {
            if(Function != VirtualDiskFunction.OS && IsServiceRunnig())
            {
                Wsl.terminate(ServiceName);
            }
        }

        // --- wordslab virtual machine software ---

        // Versions last updated : January 9 2022

        // Alpine mini root filesystem: https://alpinelinux.org/downloads/
        internal static readonly string alpineVersion   = "3.15.0";
        internal static readonly string alpineImageURL  = $"https://dl-cdn.alpinelinux.org/alpine/v{alpineVersion.Substring(0, 4)}/releases/x86_64/alpine-minirootfs-{alpineVersion}-x86_64.tar.gz";
        internal static readonly int    alpineImageSize = 5867520; // 2731445 compressed
        internal static readonly string alpineFileName  = $"alpine-{alpineVersion}.tar";

        // Ubuntu minimum images: https://partner-images.canonical.com/oci/
        internal static readonly string ubuntuRelease   = "focal";
        internal static readonly string ubuntuVersion   = "20220105";
        internal static readonly string ubuntuImageURL  = $"https://partner-images.canonical.com/oci/{ubuntuRelease}/{ubuntuVersion}/ubuntu-{ubuntuRelease}-oci-amd64-root.tar.gz";
        internal static readonly int    ubuntuImageSize = 78499840; // 27746207 compressed
        internal static readonly string ubuntuFileName  = $"ubuntu-{ubuntuRelease}-{ubuntuVersion}.tar";

        // --- wordslab virtual machine scripts ---

        internal static readonly string vmDiskInitScript      = "wordslab-vm-init.sh";
        internal static readonly string vmGPUInitScript       = "wordslab-gpu-init.sh";
        internal static readonly string clusterDiskInitScript = "wordslab-cluster-init.sh";
        internal static readonly string dataDiskInitScript    = "wordslab-data-init.shh";

        internal static readonly string vmStartupScript          = "wordslab-vm-start.sh";
        internal static readonly string clusterDiskStartupScript = "wordslab-cluster-start.sh";
        internal static readonly string dataDiskStartupScript    = "wordslab-data-start.sh";

        private static string GetDiskInitScript(VirtualDiskFunction function)
        {
            switch (function)
            {
                case VirtualDiskFunction.OS:
                    return vmDiskInitScript;
                case VirtualDiskFunction.Cluster:
                    return clusterDiskInitScript;
                case VirtualDiskFunction.Data:
                    return dataDiskInitScript;
            }
            return null;
        }
    }
}
