using wordslab.manager.config;
using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm
{
    public static class VMRequirements
    {
        // Recommended Virtual Machine specs

        public const int MIN_HOST_RESERVED_PROCESSORS = 2;
        public const int MIN_HOST_RESERVED_MEMORY_GB = 2;
        public const int MIN_HOST_RESERVED_STORAGE_GB = 2;

        public const int MIN_HOST_DOWNLOADDIR_GB = 1;
        public const int MIN_HOST_BACKUPDIR_GB = 2;

        public const int MIN_VM_PROCESSORS = 2;
        public const int MIN_VM_MEMORY_GB = 4;
        public const string MIN_VM_GPUMODEL = "NVIDIA GeForce GTX 1050";
        public const int MIN_VM_GPUMEMORY_GB = 4;
        public const int MIN_VM_CLUSTERDISK_GB = 5;
        public const int MIN_VM_DATADISK_GB = 1;

        public const int REC_VM_PROCESSORS = 6;
        public const int REC_VM_MEMORY_GB = 12;
        public const string REC_VM_GPUMODEL = "NVIDIA GeForce RTX 2060";
        public const int REC_VM_GPUMEMORY_GB = 6;
        public const int REC_VM_CLUSTERDISK_GB = 10;
        public const int REC_VM_DATADISK_GB = 25;

        public const int DEFAULT_HOST_SSH_PORT = 3022;
        public const int DEFAULT_HOST_Kubernetes_PORT = 6443;
        public const int DEFAULT_HOST_HttpIngress_PORT = 80;
        public const int DEFAULT_HOST_HttpsIngress_PORT = 443;

        public static bool CheckCPURequirements(VirtualMachineSpec vmSpec, Compute.CPUInfo cpuInfo, out string cpuErrorMessage)
        {
            var cpuSpecOK = cpuInfo.NumberOfLogicalProcessors >= (MIN_HOST_RESERVED_PROCESSORS + vmSpec.Compute.Processors);
            if (cpuSpecOK)
            {
                cpuErrorMessage = null;
            }
            else
            {
                cpuErrorMessage = $"The CPU is not powerful enough: {MIN_HOST_RESERVED_PROCESSORS + vmSpec.Compute.Processors} logical processors are required, {cpuInfo.NumberOfLogicalProcessors} are available.";
            }
            return cpuSpecOK;
        }

        public static bool CheckMemoryRequirements(VirtualMachineSpec vmSpec, Memory.MemoryInfo memInfo, out string memoryErrorMessage)
        {
            var memorySpecOK = memInfo.TotalPhysicalMB >= (ulong)((MIN_HOST_RESERVED_MEMORY_GB + vmSpec.Compute.MemoryGB) * 1000);
            if (memorySpecOK)
            {
                memoryErrorMessage = null;
            }
            else
            {
                memoryErrorMessage = $"Not enough physical memory: {MIN_HOST_RESERVED_MEMORY_GB + vmSpec.Compute.MemoryGB} GBs are required, {memInfo.TotalPhysicalMB/1024f:F1} GB are available.";
            }
            return memorySpecOK;
        }

        public static bool CheckStorageRequirements(VirtualMachineSpec vmSpec, Dictionary<string, os.DriveInfo> drivesInfo, out string storageErrorMessage)
        {
            // Disk space requirements
            var ssdStorageReqs = 0;
            var anyStorageReqs = MIN_HOST_BACKUPDIR_GB;
            if (vmSpec.Storage.ClusterDiskIsSSD)
            {
                ssdStorageReqs += vmSpec.Storage.ClusterDiskSizeGB + MIN_HOST_DOWNLOADDIR_GB;
            }
            else
            {
                anyStorageReqs += vmSpec.Storage.ClusterDiskSizeGB + MIN_HOST_DOWNLOADDIR_GB;
            }
            if(vmSpec.Storage.DataDiskIsSSD)
            {
                ssdStorageReqs += vmSpec.Storage.DataDiskSizeGB;
            }
            else
            {
                anyStorageReqs += vmSpec.Storage.DataDiskSizeGB;
            }

            // Available disk space
            var ssdStorageSpace = drivesInfo.Values.Where(di => di.IsSSD).Sum(di => di.FreeSpaceMB / 1000f);
            var anyStorageSpace = drivesInfo.Values.Where(di => !di.IsSSD).Sum(di => di.FreeSpaceMB / 1000f);
            var osdirInfo = Storage.GetDriveInfoForUserProfile();
            if (osdirInfo.IsSSD)
            {
                ssdStorageSpace -= MIN_HOST_RESERVED_STORAGE_GB;
            }
            else
            {
                anyStorageSpace -= MIN_HOST_RESERVED_STORAGE_GB;
            }

            // Compare
            bool storageSpecOK;
            if (ssdStorageSpace >= (ssdStorageReqs + anyStorageReqs) ||
                (ssdStorageSpace >= ssdStorageReqs && anyStorageSpace >= anyStorageReqs))
            {
                storageSpecOK = true;
                storageErrorMessage = null;
            }
            else
            { 
                storageSpecOK = false;
                storageErrorMessage = $"Not enough free storage space. Storage required: {ssdStorageReqs} GB of SSD and {anyStorageReqs} GB of any kind. Free storage space available: {ssdStorageSpace:F1} GB of SSD and {anyStorageSpace:F1} of other storage";
            }
            return storageSpecOK;
        }

        public static bool CheckGPURequirements(VirtualMachineSpec vmSpec, List<Compute.GPUInfo> gpusInfo, out string gpuErrorMessage)
        {
            bool gpuSpecOK = true;
            gpuErrorMessage = null;
            if (vmSpec.GPU != null)
            {
                var gpuArchitecture = Compute.GPUInfo.GetArchitecture(vmSpec.GPU.ModelName);
                var availableGPU = gpusInfo.Where(gpu => gpu.ModelName == vmSpec.GPU.ModelName).FirstOrDefault();
                if(availableGPU == null)
                {
                    availableGPU = gpusInfo.Where(gpu => Compute.GPUInfo.GetArchitecture(gpu.ModelName) == gpuArchitecture).OrderByDescending(gpu => gpu.MemoryMB).FirstOrDefault();
                    if (availableGPU != null)
                    {
                        gpuErrorMessage = $"Could not find the required Nvidia GPU (\"{vmSpec.GPU.ModelName}\"), but found a GPU of the same architecture or more recent (\"{availableGPU.ModelName}\").";
                    }
                }
                if (availableGPU != null)
                {
                    gpuSpecOK = availableGPU.MemoryMB >= (vmSpec.GPU.MemoryGB * 1000);
                }
                else
                {
                    gpuSpecOK = false;
                }
                if (!gpuSpecOK)
                {
                    gpuErrorMessage = $"Could not find the required Nvidia GPU (\"{vmSpec.GPU.ModelName}\" with {vmSpec.GPU.MemoryGB} GB memory) on the host machine.";
                    if (gpusInfo.Count == 0)
                    {
                        gpuErrorMessage += " If the card is physically installed in the machine, you can try to update your Nvidia drivers and to install nvidia-smi.";
                    }
                }
            }
            return gpuSpecOK;
        }

        public static VirtualMachineSpec GetMinimumVMSpec()
        {
            var minVMSpec = new VirtualMachineSpec();
            minVMSpec.Compute.Processors = MIN_VM_PROCESSORS;
            minVMSpec.Compute.MemoryGB = MIN_VM_MEMORY_GB;
            minVMSpec.GPU = new GPUSpec() { ModelName = MIN_VM_GPUMODEL, MemoryGB = MIN_VM_GPUMEMORY_GB, GPUCount = 1 };
            minVMSpec.Storage.ClusterDiskSizeGB = MIN_VM_CLUSTERDISK_GB;
            minVMSpec.Storage.ClusterDiskIsSSD = false;
            minVMSpec.Storage.DataDiskSizeGB = MIN_VM_DATADISK_GB;
            minVMSpec.Storage.DataDiskIsSSD = true;
            return minVMSpec;
        }

        public static RecommendedVMSpecs GetRecommendedVMSpecs()
        {
            var cpuInfo = Compute.GetCPUInfo();
            var memInfo = Memory.GetMemoryInfo();
            var gpusInfo = Compute.GetNvidiaGPUsInfo();
            var drivesInfo = Storage.GetDrivesInfo();

            var minVMSpec = GetMinimumVMSpec();

            string minCPUErrorMessage = null;
            string minMemoryErrorMessage = null;
            string minStorageErrorMessage = null;
            string minGPUErrorMessage = null;
            var minRequirementsOK = CheckCPURequirements(minVMSpec, cpuInfo, out minCPUErrorMessage);
            minRequirementsOK = CheckMemoryRequirements(minVMSpec, memInfo, out minMemoryErrorMessage) && minRequirementsOK;
            minRequirementsOK = CheckStorageRequirements(minVMSpec, drivesInfo, out minStorageErrorMessage) && minRequirementsOK;
            minRequirementsOK = CheckGPURequirements(minVMSpec, gpusInfo, out minGPUErrorMessage) && minRequirementsOK;
            string minVMSpecErrorMessage = null;
            if (!minRequirementsOK)
            {
                minVMSpecErrorMessage = $"{minCPUErrorMessage} {minMemoryErrorMessage} {minStorageErrorMessage} {minGPUErrorMessage}";
            }

            var recVMSpec = new VirtualMachineSpec();
            recVMSpec.Compute.Processors = REC_VM_PROCESSORS;
            recVMSpec.Compute.MemoryGB = REC_VM_MEMORY_GB;
            recVMSpec.GPU = new GPUSpec() { ModelName = REC_VM_GPUMODEL, MemoryGB = REC_VM_GPUMEMORY_GB, GPUCount = 1 };
            recVMSpec.Storage.ClusterDiskSizeGB = REC_VM_CLUSTERDISK_GB;
            recVMSpec.Storage.ClusterDiskIsSSD = true;
            recVMSpec.Storage.DataDiskSizeGB = REC_VM_DATADISK_GB;
            recVMSpec.Storage.DataDiskIsSSD = true;

            string recCPUErrorMessage = null;
            string recMemoryErrorMessage = null;
            string recStorageErrorMessage = null;
            string recGPUErrorMessage = null;
            var recRequirementsOK = CheckCPURequirements(recVMSpec, cpuInfo, out recCPUErrorMessage);
            recRequirementsOK = CheckMemoryRequirements(recVMSpec, memInfo, out recMemoryErrorMessage) && recRequirementsOK;
            recRequirementsOK = CheckStorageRequirements(recVMSpec, drivesInfo, out recStorageErrorMessage) && recRequirementsOK;
            recRequirementsOK = CheckGPURequirements(recVMSpec, gpusInfo, out recGPUErrorMessage) && recRequirementsOK;
            string recVMSpecErrorMessage = null;
            if (!recRequirementsOK)
            {
                recVMSpecErrorMessage = $"{recCPUErrorMessage} {recMemoryErrorMessage} {recStorageErrorMessage} {recGPUErrorMessage}";
            }

            var maxVMSpec = new VirtualMachineSpec();
            maxVMSpec.Compute.Processors = (int)cpuInfo.NumberOfLogicalProcessors - MIN_HOST_RESERVED_PROCESSORS;
            maxVMSpec.Compute.MemoryGB = (int)(memInfo.TotalPhysicalMB/1000) - MIN_HOST_RESERVED_MEMORY_GB;
            var bestGPU = gpusInfo.OrderByDescending(gpu => (int)gpu.Architecture).ThenByDescending(gpu => gpu.MemoryMB).FirstOrDefault();
            if (bestGPU != null)
            {
                maxVMSpec.GPU = new GPUSpec() { ModelName = bestGPU.ModelName, MemoryGB = bestGPU.MemoryMB / 1024, GPUCount = 1 };
            }
            // Max disk size = max available storage space
            var maxSSDStorageSpace = 0;
            if (drivesInfo.Values.Any(di => di.IsSSD))
            {
                maxSSDStorageSpace = (int)drivesInfo.Values.Where(di => di.IsSSD).Max(di => di.FreeSpaceMB / 1000f);
            }
            var maxAnyStorageSpace = 0;
            if (drivesInfo.Values.Any(di => !di.IsSSD))
            {
                maxAnyStorageSpace = (int)drivesInfo.Values.Where(di => !di.IsSSD).Max(di => di.FreeSpaceMB / 1000f);
            }
            if (maxSSDStorageSpace > recVMSpec.Storage.ClusterDiskSizeGB ||
                (minVMSpec.Storage.ClusterDiskIsSSD && maxSSDStorageSpace > minVMSpec.Storage.ClusterDiskSizeGB) ||
                maxSSDStorageSpace > maxAnyStorageSpace)
            {
                maxVMSpec.Storage.ClusterDiskSizeGB = maxSSDStorageSpace;
                maxVMSpec.Storage.ClusterDiskIsSSD = true;
            }
            else
            {
                maxVMSpec.Storage.ClusterDiskSizeGB = maxAnyStorageSpace;
                maxVMSpec.Storage.ClusterDiskIsSSD = false;
            }
            if (maxSSDStorageSpace > recVMSpec.Storage.DataDiskSizeGB ||
                (minVMSpec.Storage.DataDiskIsSSD && maxSSDStorageSpace > minVMSpec.Storage.DataDiskSizeGB) ||
                maxSSDStorageSpace > maxAnyStorageSpace)
            {
                maxVMSpec.Storage.DataDiskSizeGB = maxSSDStorageSpace;
                maxVMSpec.Storage.DataDiskIsSSD = true;
            }
            else
            {
                maxVMSpec.Storage.DataDiskSizeGB = maxAnyStorageSpace;
                maxVMSpec.Storage.DataDiskIsSSD = false;
            }           

            return new RecommendedVMSpecs(minVMSpec, minRequirementsOK, minVMSpecErrorMessage, recVMSpec, recRequirementsOK, recVMSpecErrorMessage, maxVMSpec);
        }
    }

    public class RecommendedVMSpecs
    {
        public RecommendedVMSpecs(VirtualMachineSpec minVMSpec, bool minVMSpecIsOK, string minVMSpecErrorMessage, VirtualMachineSpec recVMSpec, bool recVMSpecIsOK, string recVMSpecErrorMessage, VirtualMachineSpec maxVMSpec)
        {
            MinimumVMSpec = minVMSpec;
            MinimumVMSpecIsSupportedOnThisMachine = minVMSpecIsOK;
            MinimunVMSpecErrorMessage = minVMSpecErrorMessage;

            RecommendedVMSpec = recVMSpec;
            RecommendedVMSpecIsSupportedOnThisMachine = recVMSpecIsOK;
            RecommendedVMSpecErrorMessage = recVMSpecErrorMessage;

            MaximumVMSpecOnThisMachine = maxVMSpec;
        }

        public VirtualMachineSpec MinimumVMSpec { get; private set; }
        public bool MinimumVMSpecIsSupportedOnThisMachine { get; private set; }
        public string MinimunVMSpecErrorMessage { get; private set; }

        public VirtualMachineSpec RecommendedVMSpec { get; private set; }
        public bool RecommendedVMSpecIsSupportedOnThisMachine { get; private set; }
        public string RecommendedVMSpecErrorMessage { get; private set; }

        public VirtualMachineSpec MaximumVMSpecOnThisMachine { get; private set; }
    }
}
