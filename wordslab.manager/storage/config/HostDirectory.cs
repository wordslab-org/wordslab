using System.ComponentModel.DataAnnotations;

namespace wordslab.manager.storage.config
{    
    public class HostDirectory
    {
        public HostDirectory()
        { }

        public HostDirectory(StorageFunction function, string path)
        {
            Function = function;
            Path = path;
        }

        public enum StorageFunction
        {
            DownloadCache,
            VirtualMachineOS,
            VirtualMachineCluster,
            VirtualMachineData,
            Backup
        }

        [Key]
        public StorageFunction Function { get; private set; }

        public string Path { get; private set; }
    }
}
