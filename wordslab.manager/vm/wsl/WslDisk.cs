using System.Text.RegularExpressions;
using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm.wsl
{
    public class WslDisk : VirtualDisk
    {
        public static List<string> ListVMNamesFromClusterDisks(HostStorage storage)
        {
            var vmNames = new List<string>();
            var storagePathTemplate = GetHostStorageDirectory("*", VirtualDiskFunction.Cluster, storage);
            var servicenames = Directory.GetFileSystemEntries(Path.GetDirectoryName(storagePathTemplate), Path.GetFileName(storagePathTemplate));
            var vmNameRegex = new Regex(storagePathTemplate.Replace("\\","\\\\").Replace("*", "(.+)"));
            foreach(var servicename in servicenames)
            {
                vmNames.Add(vmNameRegex.Match(servicename).Groups[1].Value);
            }
            return vmNames;
        }

        public static VirtualDisk TryFindByName(string vmName, VirtualDiskFunction function, HostStorage storage)
        {
            var serviceName = GetServiceName(vmName, function);
            var wslDistribs = Wsl.list();
            if (wslDistribs.Any(d => d.Distribution == serviceName))
            {
                var storagePath = GetHostStorageFile(vmName, function, storage);
                if (File.Exists(storagePath))
                {
                    return new WslDisk(vmName, function, storagePath, Storage.IsPathOnSSD(storagePath), storage);
                }
            }
            return null;
        }

        private static string GetHostStorageFile(string vmName, VirtualDiskFunction function, HostStorage storage)
        {
            return Path.Combine(VirtualDisk.GetHostStorageDirectory(vmName, function, storage), "ext4.vhdx");
        }
                
        public static VirtualDisk CreateFromOSImage(string vmName, string osImagePath, /*int maxSizeGB,*/ HostStorage storage)
        {
            // See below:  public override void Resize(int maxSizeGB)
            // maxSizeGB = 256;

            var alreadyExistingDisk = TryFindByName(vmName, VirtualDiskFunction.Cluster, storage);
            if (alreadyExistingDisk != null) throw new ArgumentException($"A virtual cluster disk already exists for virtual machine {vmName}");

            var serviceName = GetServiceName(vmName, VirtualDiskFunction.Cluster);
            var storageDirectory = GetHostStorageDirectory(vmName, VirtualDiskFunction.Cluster, storage);

            string cacheDirectory = storage.DownloadCacheDirectory;
            string scriptsDirectory = GetScriptsDirectory(storage);
            var diskInitAndStartScripts = GetDiskInitAndStartScripts(VirtualDiskFunction.Cluster);

            Wsl.import(serviceName, storageDirectory, osImagePath, 2);
            foreach (var diskScript in diskInitAndStartScripts)
            {
                Wsl.execShell($"cp $(wslpath '{scriptsDirectory}')/{diskScript} /root/{diskScript}", serviceName);
                Wsl.execShell($"chmod a+x /root/{diskScript}", serviceName);
            }
            Wsl.execShell($"/root/{diskInitAndStartScripts[0]} '{cacheDirectory}' '{VirtualMachine.k3sExecutableFileName}' '{VirtualMachine.k3sImagesFileName}' '{VirtualMachine.helmFileName}'", serviceName, timeoutSec: 60, ignoreError: "perl: warning");
            Wsl.terminate(serviceName);

            return TryFindByName(vmName, VirtualDiskFunction.Cluster, storage);
        }

        public bool InstallNvidiaContainerRuntimeOnOSImage(HostStorage storage)
        {
            if (Function == VirtualDiskFunction.Cluster)
            {
                string scriptsDirectory = GetScriptsDirectory(storage);

                try
                {
                    Wsl.execShell("nvidia-smi -L", ServiceName);
                }
                catch(Exception e)
                {
                    throw new InvalidOperationException($"Could not find a Nvidia GPU inside the virtual machine {VMName}, or nvidia-smi is not available");
                }
                
                // Temporary reopening of the Windows host interop to get the GPU config file for k3s
                Wsl.execShell("echo -e \"[automount]\\nenabled=true\\n[interop]\\nenabled=true\\nappendWindowsPath=true\" > /etc/wsl.conf", ServiceName);
                Wsl.terminate(ServiceName);

                Wsl.execShell($"/root/{clusterGPUInitScript} '{scriptsDirectory}' '{VirtualMachine.nvidiaContainerRuntimeVersion}'", ServiceName, timeoutSec: 60, ignoreError: "perl: warning");
                Wsl.terminate(ServiceName);
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
            var storageDirectory = GetHostStorageDirectory(vmName, function, storage);

            string cacheDirectory = storage.DownloadCacheDirectory;
            string scriptsDirectory = GetScriptsDirectory(storage);
            var diskInitAndStartScripts = GetDiskInitAndStartScripts(function);

            Wsl.import(serviceName, storageDirectory, Path.Combine(cacheDirectory, alpineFileName), 2);
            foreach (var diskScript in diskInitAndStartScripts)
            {
                Wsl.execShell($"cp $(wslpath '{scriptsDirectory}')/{diskScript} /root/{diskScript}", serviceName);
                Wsl.execShell($"chmod a+x /root/{diskScript}", serviceName);
            }
            Wsl.execShell($"/root/{diskInitAndStartScripts[0]}", serviceName, timeoutSec: 120);
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
            var dir = new FileInfo(StoragePath).Directory;
            if (IsServiceRunnig())
            {
                StopService();
            }
            Wsl.unregister(ServiceName);
            dir.Delete();
        }

        public override bool IsServiceRequired()
        {
            return Function != VirtualDiskFunction.Cluster;
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
                var startupScriptPath = $"/root/{startupScript} {ServiceName}";            
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
            if(Function != VirtualDiskFunction.Cluster && IsServiceRunnig())
            {
                Wsl.terminate(ServiceName);
            }
        }

        // --- wordslab virtual machine software ---

        // Versions last updated : August 16 2022

        // Alpine mini root filesystem: https://alpinelinux.org/downloads/
        internal static readonly string alpineVersion   = "3.16.0";
        internal static readonly string alpineImageURL  = $"https://dl-cdn.alpinelinux.org/alpine/v{alpineVersion.Substring(0, 4)}/releases/x86_64/alpine-minirootfs-{alpineVersion}-x86_64.tar.gz";
        internal static readonly int    alpineImageDownloadSize = 2712602;
        internal static readonly int    alpineImageDiskSize = 5816320;
        internal static readonly string alpineFileName  = $"alpine-{alpineVersion}.tar";

        // Ubuntu minimum images: https://partner-images.canonical.com/oci/
        internal static readonly string ubuntuRelease   = "focal";
        internal static readonly string ubuntuVersion   = "20220815";
        internal static readonly string ubuntuImageURL  = $"https://partner-images.canonical.com/oci/{ubuntuRelease}/{ubuntuVersion}/ubuntu-{ubuntuRelease}-oci-amd64-root.tar.gz";
        internal static readonly int    ubuntuImageDownloadSize = 27761025;
        internal static readonly int    ubuntuImageDiskSize = 78499840;
        internal static readonly string ubuntuFileName  = $"ubuntu-{ubuntuRelease}-{ubuntuVersion}.tar";

        // --- wordslab virtual machine scripts ---

        internal static string GetScriptsDirectory(HostStorage storage) { return Path.Combine(storage.ScriptsDirectory, "vm", "wsl"); }

        internal static readonly string clusterDiskInitScript = "wordslab-cluster-init.sh";
        internal static readonly string clusterGPUInitScript  = "wordslab-gpu-init.sh";
        internal static readonly string dataDiskInitScript    = "wordslab-data-init.sh";

        internal static readonly string clusterDiskStartupScript = "wordslab-cluster-start.sh";
        internal static readonly string dataDiskStartupScript    = "wordslab-data-start.sh";

        private static string[] GetDiskInitAndStartScripts(VirtualDiskFunction function)
        {
            switch (function)
            {
                case VirtualDiskFunction.Cluster:
                    return new string[] { clusterDiskInitScript, clusterGPUInitScript, clusterDiskStartupScript };
                case VirtualDiskFunction.Data:
                    return new string[] { dataDiskInitScript, dataDiskStartupScript };
            }
            return null;
        }
    }
}
