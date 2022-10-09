using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace wordslab.manager.config
{
    [Owned]
    public class ComputeQuotas
    {
        public int Processors { get; set; }

        public int MemoryGB { get; set; }
    }

    [Owned]
    public class DeviceTypeQuota
    {
        [Key]
        public string ModelName { get; set; }

        public int Count { get; set; }
    }

    [Owned]
    public class StorageQuotas
    {
        public int HDDDiskSizeGB { get; set; }

        public int SSDDiskSizeGB { get; set; }

        public int ObjectStorageSizeGB { get; set; }
    }

    [Owned]
    public class NetworkQuotas
    {
        public int StaticIPAdressCount { get; set; }

        public int EgressTraficPerMonthGB { get; set; }
    }

    [Owned]
    public class CloudAccountQuotas
    {
        public List<DeviceTypeQuota> VirtualMachines { get; set; }

        public ComputeQuotas Compute { get; set; }

        public List<DeviceTypeQuota> GPUs { get; set; }

        public StorageQuotas Storage { get; set; }

        public NetworkQuotas Network { get; set; }
    }
}
