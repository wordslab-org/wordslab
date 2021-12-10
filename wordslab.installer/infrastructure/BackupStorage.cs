namespace wordslab.installer.infrastructure
{
    public class BackupStorage
    {
        public BackupProtocol Protocol { get; }
        public string Server { get; }
        public string StorageUnit { get; }
        public string RelativePath { get; }
        public object Credentials { get; }
        public int MaxCapacityMB { get; }
    }

    public enum BackupProtocol
    {
        FileSystem,
        S3
    }
}
