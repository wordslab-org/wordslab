using System.Net;

namespace wordslab.installer.infrastructure
{
    public class VirtualMachine
    {
        public const string DEFAULT_NAME = "wordslab-vm";

        public string Name { get; }

        public IPAddress IPAddress { get; }
        public bool IsLocal { get; }

        public int CPUCount { get; }
        public CPUInfo CPUInfo { get; }
        public int MemoryMB { get; }
        public bool SharedResources { get; }

        public GPUSupportStatus GPUSupport { get; }
        public int GPUCount { get; }
        public List<GPUInfo> GPUInfo { get; }

        public string OSName { get; }
        public string OSVersion { get; }
        public PersistentDisk OSDiskInfo { get; }

        public string K3sVersion { get; }
        public string K3sMountPath { get; }
        public PersistentDisk K3sDiskInfo { get; }

        public string LocalStorageMountPath { get; }
        public PersistentDisk LocalStorageDiskInfo { get; }
    }

    public class CPUInfo
    {
        // cat /proc/cpuinfo

        public string Vendor { get; }
        public int Model { get; }
        public string ModelName { get; }
        public int MaxFrequencyMHz { get; }
        public int CacheSizeKB { get; }
        public HashSet<string> Flags { get; }
    }

    public enum GPUSupportStatus
    {
        None,
        AutoDetect,
        OnDemand
    }

    public class GPUInfo
    {
        // nvidia-smi -q

        public string ProductName { get; }
        public string ProductArchitecture { get; }
        public int MemoryMB { get; }

        public string DriverVersion { get; }
        public string CUDAVersion { get; }
    }
}
