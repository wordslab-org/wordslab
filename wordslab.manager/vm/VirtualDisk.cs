using wordslab.manager.storage;

namespace wordslab.manager.vm
{
    public abstract class VirtualDisk 
    {
        protected static string GetLocalStoragePathWithoutExtension(string vmName, VirtualDiskFunction function, HostStorage storage)
        {
            switch (function)
            {
                case VirtualDiskFunction.OS:
                    return Path.Combine(storage.VirtualMachineOSDirectory, $"wordslab-{vmName}-os-disk");
                case VirtualDiskFunction.Cluster:
                    return Path.Combine(storage.VirtualMachineClusterDirectory, $"wordslab-{vmName}-cluster-disk");
                case VirtualDiskFunction.Data:
                    return Path.Combine(storage.VirtualMachineDataDirectory, $"wordslab-{vmName}-data-disk");
            }
            return null;
        }

        protected VirtualDisk(string vmName, VirtualDiskFunction function, string storagePath, int totalSizeGB, bool isSSD)
        {
            VMName = vmName;
            Function = function;
            StoragePath = storagePath;
            TotalSizeGB = totalSizeGB;
            IsSSD = isSSD;
        }

        public string VMName { get; protected set; }

        public VirtualDiskFunction Function { get; protected set; }

        public string StoragePath { get; protected set; }

        public int TotalSizeGB { get; protected set; }

        public bool IsSSD { get; protected set; }

        // Implement this in all subclasses :
        // public static VirtualDisk CreateWithOS(HostStorage storage, string osImagePath, int totalSizeMB)
        // public static VirtualDisk CreateBlank(HostStorage storage, VirtualDiskFunction function, int totalSizeMB)

        public abstract void Resize(int totalSizeGB);

        public abstract void Delete();

        public abstract bool IsServiceRequired();

        public abstract void StartService();

        public abstract bool IsServiceRunnig();

        public abstract void StopService();
    }
    
    public enum VirtualDiskFunction
    {
        OS,
        Cluster,
        Data
    }
}
