namespace wordslab.manager.vm
{
    public abstract class VirtualDisk 
    {
        public string Name { get; protected set; }

        public VirtualDiskFunction Function { get; protected set; }

        public string StoragePath { get; protected set; }

        public int TotalSizeMB { get; protected set; }

        public int AvailableSpaceMB { get; protected set; }

        public bool IsSSD { get; protected set; }
    }
    
    public enum VirtualDiskFunction
    {
        OS,
        Cluster,
        Data
    }
}
