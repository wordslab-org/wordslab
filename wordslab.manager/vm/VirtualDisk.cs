using wordslab.manager.storage;

namespace wordslab.manager.vm
{
    public abstract class VirtualDisk 
    {
        protected static string GetServiceName(string vmName, VirtualDiskFunction function)
        {
            switch (function)
            {
                case VirtualDiskFunction.OS:
                    return $"wordslab-{vmName}-vm";
                case VirtualDiskFunction.Cluster:
                    return $"wordslab-{vmName}-cluster-disk";
                case VirtualDiskFunction.Data:
                    return $"wordslab-{vmName}-data-disk";
            }
            return null;
        }

        protected static string GeHostStorageDirectory(VirtualDiskFunction function, HostStorage storage)
        {
            switch (function)
            {
                case VirtualDiskFunction.OS:
                    return storage.VirtualMachineOSDirectory;
                case VirtualDiskFunction.Cluster:
                    return storage.VirtualMachineClusterDirectory;
                case VirtualDiskFunction.Data:
                    return storage.VirtualMachineDataDirectory;
            }
            return null;
        }

        protected static string GetHostStoragePathWithoutExtension(string vmName, VirtualDiskFunction function, HostStorage storage)
        {
            var serviceName = GetServiceName(vmName, function);
            var hostStorageDirectory = GeHostStorageDirectory(function, storage);
            return Path.Combine(hostStorageDirectory, serviceName);
        }

        protected VirtualDisk(string vmName, VirtualDiskFunction function, string storagePath, int maxSizeGB, bool isSSD)
        {
            VMName = vmName;
            Function = function;
            StoragePath = storagePath;
            MaxSizeGB = maxSizeGB;
            IsSSD = isSSD;
        }

        public string VMName { get; private set; }

        public VirtualDiskFunction Function { get; protected set; }

        public string StoragePath { get; protected set; }

        public int MaxSizeGB { get; protected set; }

        public bool IsSSD { get; protected set; }

        // Implement this in all subclasses :
        // public static VirtualDisk TryFindByName(string vmName, VirtualDiskFunction function, HostStorage storage)
        // public static VirtualDisk CreateFromOSImage(string vmName, string osImagePath, int maxSizeGB, HostStorage storage)
        // public static VirtualDisk CreateBlank(string vmName, VirtualDiskFunction function, int maxSizeGB, HostStorage storage)
   
        public abstract void Resize(int maxSizeGB);

        public abstract void Delete();

        public virtual bool IsServiceRequired() {  return false; }

        public string ServiceName { get; protected set; }

        public virtual void StartService() { }

        public virtual bool IsServiceRunnig() { return false; }

        public virtual void StopService() { }
    }
    
    public enum VirtualDiskFunction
    {
        OS,
        Cluster,
        Data
    }
}
