namespace wordslab.installer.infrastructure.commands
{
    // Checking GPU requirements
    public class nvidia_smi
    {        
        // nvidia-smi --query-gpu=driver_version --format=csv,noheader
        // > 496.76
        public static bool IsNvidiaDriverVersionOKForWSL2WithGPU()
        {
            return true;
        }

        public class GPUInfo
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public int MemoryGB { get; set; }
            public GPUArchitectureInfo Architecture { get; set; }
        }

        // Pascal: GeForce GTX 10xx | TITAN X
        // Volta: TITAN V
        // Turing: GeForce GTX 16xx | GeForce RTX 20xx | TITAN RTX
        // Ampere: GeForce RTX 30xx
        public enum GPUArchitectureInfo
        {
            Unknown,
            Pascal,
            Volta,
            Turing,
            Ampere
        }

        // nvidia-smi --query-gpu=index,gpu_name,memory.total --format=csv,noheader
        // > 0, NVIDIA GeForce GTX 1050, 4096 MiB
        public static List<GPUArchitectureInfo> GetNvidiaGPUs()
        {
            return null;
        }
    }
}
