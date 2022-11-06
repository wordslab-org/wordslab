using Microsoft.EntityFrameworkCore;
using wordslab.manager.os;

namespace wordslab.manager.config
{
    [Owned]
    public class ComputeSpec
    {
        public int Processors { get; set; }

        public int MemoryGB { get; set; }

        public override bool Equals(object? obj)
        {
            var spec = obj as ComputeSpec;
            if (spec == null) return false;

            var equals = true;
            equals = equals && spec.Processors == Processors;
            equals = equals && spec.MemoryGB == MemoryGB;
            return equals;
        }
    }

    [Owned]
    public class GPUSpec
    {
        public string? ModelName { get; set; }

        public int MemoryGB { get; set; }

        public int GPUCount { get; set; }

        public override bool Equals(object? obj)
        {
            var spec = obj as GPUSpec;
            if (spec == null) return false;

            var equals = true;            
            equals = equals && spec.GPUCount == GPUCount;
            equals = equals && spec.ModelName == ModelName;
            equals = equals && spec.MemoryGB == MemoryGB;
            return equals;
        }
    }

    [Owned]
    public class StorageSpec
    {
        public int ClusterDiskSizeGB { get; set; }
        public bool ClusterDiskIsSSD { get; set; }

        public int DataDiskSizeGB { get; set; }
        public bool DataDiskIsSSD { get; set; }

        public override bool Equals(object? obj)
        {
            var spec = obj as StorageSpec;
            if (spec == null) return false;

            var equals = true;
            equals = equals && spec.ClusterDiskSizeGB == ClusterDiskSizeGB;
            equals = equals && spec.ClusterDiskIsSSD == ClusterDiskIsSSD;
            equals = equals && spec.DataDiskSizeGB == DataDiskSizeGB;
            equals = equals && spec.DataDiskIsSSD == DataDiskIsSSD;
            return equals;
        }
    }

    [Owned]
    public class NetworkSpec
    {
        public int SSHPort { get; set; } = 21;

        public int KubernetesPort { get; set; } = 6443;

        public int HttpIngressPort { get; set; } = 80;

        public int HttpsIngressPort { get; set; } = 443;

        public override bool Equals(object? obj)
        {
            var spec = obj as NetworkSpec;
            if (spec == null) return false;

            var equals = true;            
            equals = equals && spec.SSHPort == SSHPort;
            equals = equals && spec.KubernetesPort == KubernetesPort;
            equals = equals && spec.HttpIngressPort == HttpIngressPort;
            equals = equals && spec.HttpsIngressPort == HttpsIngressPort;
            return equals;
        }
    }

    [Owned]
    public class VirtualMachineSpec
    {
        public ComputeSpec Compute { get; set; } = new ComputeSpec();

        public GPUSpec GPU { get; set; } = new GPUSpec();

        public StorageSpec Storage { get; set; } = new StorageSpec();

        public NetworkSpec Network { get; set; } = new NetworkSpec();

        // Comparison

        public override bool Equals(object? obj)
        {
            var spec = obj as VirtualMachineSpec;
            if (spec == null) return false;

            var equals = true;
            equals = equals && spec.Compute.Equals(Compute);
            equals = equals && spec.GPU.Equals(GPU);
            equals = equals && spec.Storage.Equals(Storage);
            equals = equals && spec.Network.Equals(Network);
            return equals;
        }
    }
}
