namespace wordslab.manager.storage
{
    public class LocalDirectory
    {
        public LocalDirectory() 
        { }

        public LocalDirectory(StorageFunction function, string path)
        {
            Function = function;
            Path = path;
        }

        public int Id { get; private set; }

        public StorageFunction Function { get; private set; }
        
        public string Path { get; private set; }
    }

    public enum StorageFunction
    {
        DownloadCache,
        VirtualMachineOS,
        VirtualMachineCluster,
        VirtualMachineData,
        LocalBackup
    }
}
