namespace wordslab.installer.infrastructure
{
    public class PersistentDisk
    {
        public string Name { get; }
        public int CapacityMB { get; }
        public int MaxCapacityMB { get; }
        public bool IsSSD { get; }
        public bool IsVHD { get; }
        public string VHDPath { get; }
    }
}
