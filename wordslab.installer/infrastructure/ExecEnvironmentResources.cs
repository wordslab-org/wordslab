namespace wordslab.installer.infrastructure
{
    public class ExecEnvironmentResources
    {
        public string EnvName;

        // CPU consumption is between 0 and 1000 x logical CPUs
        // Memory and storage properties are measured in MB

        public int MaxCPU;
        public int MaxMemory;

        public int TotalCPU;
        public int TotalMemory;

        public List<ApplicationResources> AppResources;
    }

    public class ApplicationResources
    {
        public string AppName;

        public int MaxCPU;
        public int MaxMemory;

        public int TotalCPU;
        public int TotalMemory;

        public int ContainerTempStorage;

        public List<PodResources> PodResources;

        public int VolumeStorage;

        public List<VolumeResources> VolumeResources;
    }

    public class PodResources
    {
        public string PodName;

        public int ReplicaCount;

        public int MaxCPU;
        public int MaxMemory;

        public int TotalCPU;
        public int TotalMemory;

        public int ContainerTempStorage;
    }

    public class VolumeResources
    {
        public string VolumeName;

        public int VolumeStorage;
    }
}
