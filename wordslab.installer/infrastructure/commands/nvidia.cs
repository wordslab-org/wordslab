using System.Runtime.InteropServices;

namespace wordslab.installer.infrastructure.commands
{
    // Checking GPU requirements
    public class nvidia
    {        
        // nvidia-smi --query-gpu=driver_version --format=csv,noheader
        // > 496.76
        public static Version GetDriverVersion()
        {
            try
            {
                string version = "";
                Command.Run("nvidia-smi", "--query-gpu=driver_version --format=csv,noheader", outputHandler: s => version = s);
                return new Version(version);
            }
            catch (Exception)
            {
                // No Nvidia driver installed
                return null;
            }
        }

        public static bool IsNvidiaDriver20Sep21OrLater(Version driverVersion)
        {
            if(driverVersion != null)
            {
                var targetVersion = new Version(472, 12);
                return driverVersion >= targetVersion;
            }
            else { return false; }
        }

        public static bool IsNvidiaDriver16Nov21OrLater(Version driverVersion)
        {
            if (driverVersion != null)
            {
                var targetVersion = new Version(496, 76);
                return driverVersion >= targetVersion;
            }
            else { return false; }
        }

        public static void TryOpenNvidiaUpdate()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Command.LaunchAndForget(@"C:\Program Files\NVIDIA Corporation\NVIDIA GeForce Experience\NVIDIA GeForce Experience.exe");
            }
        }

        public class GPUInfo
        {
            public GPUInfo(int index, string name, int memory)
            {
                Index = index;
                Name = name;
                MemoryMB = memory;
            }

            public int Index { get; set; }
            public string Name { get; set; }
            public int MemoryMB { get; set; }
            public GPUArchitectureInfo Architecture 
            {
                get
                {
                    if (Name.Contains("GeForce GTX 10") || Name.Contains("TITAN X"))
                    {
                        return GPUArchitectureInfo.Pascal;
                    }
                    else if (Name.Contains("TITAN V"))
                    {
                        return GPUArchitectureInfo.Volta;
                    }
                    else if (Name.Contains("GeForce GTX 16") || Name.Contains("GeForce RTX 20") || Name.Contains("TITAN RTX"))
                    {
                        return GPUArchitectureInfo.Turing;
                    }
                    else if (Name.Contains("GeForce GTX 30"))
                    {
                        return GPUArchitectureInfo.Ampere;
                    }
                    else
                    {
                        return GPUArchitectureInfo.Unknown;
                    }
                }
            }
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
            var gpus = new List<object>();
            var outputParser = Command.Output.GetList(null, 
                @"(?<index>\d+),\s*(?<name>[^,]+),\s*(?<memory>\d+)",
                dict => new GPUInfo(Int32.Parse(dict["index"]), dict["name"], Int32.Parse(dict["memory"])),
                gpus);

            try
            {
                Command.Run("nvidia-smi", "--query-gpu=index,gpu_name,memory.total --format=csv,noheader", outputHandler: outputParser.Run);
            }
            catch (Exception) 
            { /* no Nvidia GPU available */ }

            return gpus.Cast<GPUInfo>().ToList();
        }
    }
}
