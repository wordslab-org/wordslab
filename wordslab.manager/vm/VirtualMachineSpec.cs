using wordslab.manager.os;

namespace wordslab.manager.vm
{
    public class VirtualMachineSpec
    {
        public string Name;

        public int LogicalProcessors;
        public int MemoryGB;

        public string GPUModel;
        public int    GPUMemoryGB;
        public bool   WithGPU { get { return GPUModel != "None"; } }

        public int  VmDiskSizeGB;
        public bool VmDiskIsSSD;

        public int  ClusterDiskSizeGB;
        public bool ClusterDiskIsSSD;

        public int  DataDiskSizeGB;
        public bool DataDiskIsSSD;

        public static VirtualMachineSpec Minimum
        {
            get
            {
                var vmSpec = new VirtualMachineSpec();

                vmSpec.LogicalProcessors = 2;
                vmSpec.MemoryGB = 4;

                if (OS.IsWindows)
                {
                    vmSpec.VmDiskSizeGB = 2;
                }
                else
                {
                    vmSpec.VmDiskSizeGB = 2;
                }
                vmSpec.ClusterDiskSizeGB = 5;
                vmSpec.DataDiskSizeGB = 5;

                return vmSpec;
            }
        }

        public static VirtualMachineSpec Recommended
        {
            get
            {
                var vmSpec = new VirtualMachineSpec();

                vmSpec.LogicalProcessors = 8;
                vmSpec.MemoryGB = 12;

                vmSpec.GPUModel = "NVIDIA GeForce GTX 1060";
                vmSpec.GPUMemoryGB = 6;

                vmSpec.VmDiskSizeGB = 5;
                vmSpec.ClusterDiskSizeGB = 10;
                vmSpec.DataDiskSizeGB = 15;

                return vmSpec;
            }
        }
    }
}
