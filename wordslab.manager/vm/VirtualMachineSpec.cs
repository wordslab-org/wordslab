using wordslab.manager.os;

namespace wordslab.manager.vm
{
    public class VirtualMachineSpec
    {
        public string Name;

        public int Cores;
        public int MemoryMB;

        public string GPUModel;
        public int    GPUMemoryMB;
        public bool   WithGPU { get { return GPUModel != "None"; } }

        public int  VmDiskSizeMB;
        public bool VmDiskIsSSD;

        public int  ClusterDiskSizeMB;
        public bool ClusterDiskIsSSD;

        public int  DataDiskSizeMB;
        public bool DataDiskIsSSD;

        public static VirtualMachineSpec Minimum
        {
            get
            {
                var vmSpec = new VirtualMachineSpec();

                vmSpec.Cores = 2;
                vmSpec.MemoryMB = 4000;

                if (OS.IsWindows)
                {
                    vmSpec.VmDiskSizeMB = 2000;
                }
                else
                {
                    vmSpec.VmDiskSizeMB = 2000;
                }
                vmSpec.ClusterDiskSizeMB = 5000;
                vmSpec.DataDiskSizeMB = 5000;

                return vmSpec;
            }
        }

        public static VirtualMachineSpec Recommended
        {
            get
            {
                var vmSpec = new VirtualMachineSpec();

                vmSpec.Cores = 8;
                vmSpec.MemoryMB = 12000;

                vmSpec.GPUModel = "NVIDIA GeForce GTX 1060";
                vmSpec.GPUMemoryMB = 6000;

                vmSpec.VmDiskSizeMB = 5000;
                vmSpec.ClusterDiskSizeMB = 10000;
                vmSpec.DataDiskSizeMB = 15000;

                return vmSpec;
            }
        }
    }
}
