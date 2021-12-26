namespace wordslab.installer.infrastructure.commands
{
    // Checking GPU requirements
    public class nvidia
    {        
        // nvidia-smi --query-gpu=driver_version --format=csv,noheader
        // > 496.76
        internal static Version GetDriverVersion()
        {
            return null;
        }

        public static bool IsNvidiaDriver20Sep21OrLater()
        {
            var driverVersion = GetDriverVersion();
            if(driverVersion != null)
            {
                var targetVersion = new Version(472, 12);
                return driverVersion >= targetVersion;
            }
            else { return false; }
        }

        public static bool IsNvidiaDriver16Nov21OrLater()
        {
            var driverVersion = GetDriverVersion();
            if (driverVersion != null)
            {
                var targetVersion = new Version(496, 76);
                return driverVersion >= targetVersion;
            }
            else { return false; }
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
        public static List<GPUInfo> GetNvidiaGPUs()
        {
            return null;
        }
    }
}
