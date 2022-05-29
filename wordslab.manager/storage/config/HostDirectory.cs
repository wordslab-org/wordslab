using static wordslab.manager.storage.HostStorage;

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

        public int Id { get; private set; }

        public StorageFunction Function { get; private set; }

        public string Path { get; private set; }
    }
}
