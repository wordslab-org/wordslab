namespace wordslab.installer.infrastructure
{
    public class HostMachineResources
    {
        // CPU consumption is between 0 and 1000 x logical CPUs
        // Memory and storage properties are measured in MB

        public int HostMachineLogicalCPUs;
        public int HostMachineMemory;

        public int VMLogicalCPUs;
        public int VMMemory;

        public bool VMIsRunning = false;

        public int VMProcessCPU;
        public int VMProcessMemory;

        public int InstallerAppStorage;
        public int DownloadCacheStorage;

        public int VMOsStorage;
        public int VMClusterStorage;
        public int VMDataStorage;

        public int LocalBackupStorage;
    }
}
