using wordslab.manager.vm;

namespace wordslab.manager.storage.config
{
    public class VirtualMachineConfig
    {
        public VirtualMachineConfig()
        { }

        public VirtualMachineConfig(VirtualMachine vm)
        {
            Type = vm.Type;
            Name = vm.Name;

            Processors = vm.Processors;
            MemoryGB = vm.MemoryGB;

            GPUModel = vm.GPUModel;
            GPUMemoryGB = vm.GPUMemoryGB;

            VmDiskSizeGB = vm.OsDisk.MaxSizeGB;
            VmDiskIsSSD = vm.OsDisk.IsSSD;

            ClusterDiskSizeGB = vm.ClusterDisk.MaxSizeGB;
            ClusterDiskIsSSD = vm.ClusterDisk.IsSSD;

            DataDiskSizeGB = vm.DataDisk.MaxSizeGB;
            DataDiskIsSSD = vm.DataDisk.IsSSD;

            HostSSHPort = vm.Endpoint.SSHPort;
            HostKubernetesPort = vm.Endpoint.KubernetesPort;
            HostHttpIngressPort = vm.Endpoint.HttpIngressPort;
        }

        public VirtualMachineType Type { get; internal set; }

        public string Name { get; internal set; }

        public int Processors { get; internal set; }
        public int MemoryGB { get; internal set; }

        public string GPUModel { get; internal set; }
        public int GPUMemoryGB { get; internal set; }

        public int VmDiskSizeGB { get; internal set; }
        public bool VmDiskIsSSD { get; internal set; }

        public int ClusterDiskSizeGB { get; internal set; }
        public bool ClusterDiskIsSSD { get; internal set; }

        public int DataDiskSizeGB { get; internal set; }
        public bool DataDiskIsSSD { get; internal set; }

        public int HostSSHPort { get; internal set; }
        public int HostKubernetesPort { get; internal set; }
        public int HostHttpIngressPort { get; internal set; }
    }
}
