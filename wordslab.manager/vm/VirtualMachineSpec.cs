﻿using wordslab.manager.os;
using wordslab.manager.storage;
using wordslab.manager.storage.config;

namespace wordslab.manager.vm
{
    public class VirtualMachineSpec : VirtualMachineConfig
    {
        public VirtualMachineSpec()
        {
            // Initialize spec with all recommended values
            // in VirtualMachineConfig constructor 
        }

        public VirtualMachineSpec(string vmName)
        {
            Name = vmName;
        }

        public bool   WithGPU { get { return GPUModel != null; } }

        // Recommended Virtual Machine specs

        public const string DEFAULT_LOCALVM_NAME = "localvm";

        public const int MIN_HOST_PROCESSORS = 2;
        public const int MIN_HOST_MEMORY_GB = 4;
        public const int MIN_HOST_DISK_GB = 5;
        public const int MIN_HOST_DOWNLOADDIR_GB = 2;

        public const int MIN_VM_PROCESSORS = 2;
        public const int MIN_VM_MEMORY_GB = 4;
        public const int MIN_VM_CLUSTERDISK_GB = 5;
        public const int MIN_VM_DATADISK_GB = 1;

        public const string MIN_VM_GPUMODEL = null;
        public const int    MIN_VM_GPUMEMORY_GB = 0;

        public const int REC_VM_PROCESSORS = 6;
        public const int REC_VM_MEMORY_GB = 12;
        public const int REC_VM_CLUSTERDISK_GB = 10;
        public const int REC_VM_DATADISK_GB = 25;

        public const string REC_VM_GPUMODEL = "NVIDIA GeForce GTX 1060";
        public const int    REC_VM_GPUMEMORY_GB = 6;

        public const int DEFAULT_HOST_SSH_PORT = 3022;
        public const int DEFAULT_HOST_Kubernetes_PORT = 6443;
        public const int DEFAULT_HOST_HttpIngress_PORT = 80;
        public const int DEFAULT_HOST_HttpsIngress_PORT = 443;

        public bool CheckCPURequirements(Compute.CPUInfo cpuInfo, out string cpuErrorMessage)
        {
            var cpuSpecOK = cpuInfo.NumberOfLogicalProcessors >= (MIN_HOST_PROCESSORS + Processors);
            if (cpuSpecOK)
            {
                cpuErrorMessage = "";
            }
            else
            {
                cpuErrorMessage = $"The CPU is not powerful enough: {MIN_HOST_PROCESSORS + Processors} logical processors are required, {cpuInfo.NumberOfLogicalProcessors} are available.";
            }
            return cpuSpecOK;
        }

        public bool CheckMemoryRequirements(Memory.MemoryInfo memInfo, out string memoryErrorMessage)
        {
            var memorySpecOK = memInfo.TotalPhysicalMB >= (ulong)((MIN_HOST_MEMORY_GB + MemoryGB) * 1000);
            if (memorySpecOK)
            {
                memoryErrorMessage = "";
            }
            else
            {
                memoryErrorMessage = $"Not enough physical memory: {MIN_HOST_MEMORY_GB + MemoryGB} GBs are required, {memInfo.TotalPhysicalMB/1024f:F1} GB are available.";
            }
            return memorySpecOK;
        }

        public bool CheckStorageRequirements(HostStorage hostStorage, out Dictionary<os.DriveInfo, int> storageReqsGB, out string storageErrorMessage)
        {
            storageReqsGB = new Dictionary<os.DriveInfo, int>();

            var userProfileDrive = Storage.GetDriveInfoForUserProfile();
            storageReqsGB.Add(userProfileDrive, MIN_HOST_DISK_GB);
            var downloadCacheDrive = Storage.GetDriveInfoFromPath(hostStorage.DownloadCacheDirectory);
            if (!storageReqsGB.ContainsKey(downloadCacheDrive))
            {
                storageReqsGB.Add(downloadCacheDrive, MIN_HOST_DOWNLOADDIR_GB);
            }
            else
            {
                storageReqsGB[downloadCacheDrive] += MIN_HOST_DOWNLOADDIR_GB;
            }

            var vmDrives = new os.DriveInfo[] {
                    Storage.GetDriveInfoFromPath(hostStorage.VirtualMachineClusterDirectory),
                    Storage.GetDriveInfoFromPath(hostStorage.VirtualMachineDataDirectory) };

            var vmStorageReqs = new int[] {
                ClusterDiskSizeGB,
                DataDiskSizeGB };

            var vmSSDReqs = new bool[] {
                ClusterDiskIsSSD,
                DataDiskIsSSD };

            bool storageSpecOK = true;
            storageErrorMessage = "";

            foreach (var tuple in vmDrives.Zip(vmStorageReqs, vmSSDReqs))
            {
                var (vmDrive, vmStorageReq, vmSSDReq) = tuple;
                if (storageReqsGB.ContainsKey(vmDrive))
                {
                    storageReqsGB[vmDrive] += vmStorageReq;
                }
                else
                {
                    storageReqsGB.Add(vmDrive, vmStorageReq);
                }
                if (vmSSDReq && !vmDrive.IsSSD)
                {
                    storageSpecOK = false;
                    storageErrorMessage += $"The volume {vmDrive.VolumeName} is not an SSD. ";
                }
            }
            foreach (var drive in storageReqsGB.Keys)
            {
                if (drive.FreeSpaceMB < (storageReqsGB[drive] * 1000))
                {
                    storageSpecOK = false;
                    storageErrorMessage += $"Not enough free storage space on {drive.VolumeName}: {(int)(drive.FreeSpaceMB / 1000)} GB avaible but {storageReqsGB[drive]} GB required. ";
                }
            }

            return storageSpecOK;
        }

        public bool CheckGPURequirements(List<Compute.GPUInfo> gpusInfo, out string gpuErrorMessage)
        {
            bool gpuSpecOK = true;
            gpuErrorMessage = "";
            if (WithGPU)
            {
                var availableGPU = gpusInfo.Where(gpu => gpu.ModelName == GPUModel).FirstOrDefault();
                if (availableGPU != null)
                {
                    gpuSpecOK = availableGPU.MemoryMB >= (GPUMemoryGB * 1000);
                }
                else
                {
                    gpuSpecOK = false;
                }
                if (!gpuSpecOK)
                {
                    gpuErrorMessage = $"Could not find the required Nvidia GPU (\"{GPUModel}\" with {GPUMemoryGB} GB memory) on the host machine. If the card is physically inserted in the machine, you can try to update your Nvidia drivers and to install nvidia-smi.";
                }
            }
            return gpuSpecOK;
        }

        public static RecommendedVMSpecs GetRecommendedVMSpecs(HostStorage hostStorage)
        {
            var cpuInfo = Compute.GetCPUInfo();
            var memInfo = Memory.GetMemoryInfo();
            var gpusInfo = Compute.GetNvidiaGPUsInfo();

            var minVMSpec = new VirtualMachineSpec("minimum-vm");
            minVMSpec.Processors = MIN_VM_PROCESSORS;
            minVMSpec.MemoryGB = MIN_VM_MEMORY_GB;
            minVMSpec.GPUModel = MIN_VM_GPUMODEL;
            minVMSpec.GPUMemoryGB = MIN_VM_GPUMEMORY_GB;
            minVMSpec.ClusterDiskSizeGB = MIN_VM_CLUSTERDISK_GB;
            minVMSpec.ClusterDiskIsSSD = false;
            minVMSpec.DataDiskSizeGB = MIN_VM_DATADISK_GB;
            minVMSpec.DataDiskIsSSD = true;

            Dictionary<os.DriveInfo, int> minStorageReqsGB = null;
            string minCPUErrorMessage = null;
            string minMemoryErrorMessage = null;
            string minStorageErrorMessage = null;
            string minGPUErrorMessage = null;
            var minRequirementsOK =
               minVMSpec.CheckCPURequirements(cpuInfo, out minCPUErrorMessage) &&
               minVMSpec.CheckMemoryRequirements(memInfo, out minMemoryErrorMessage) &&
               minVMSpec.CheckStorageRequirements(hostStorage, out minStorageReqsGB, out minStorageErrorMessage) && 
               minVMSpec.CheckGPURequirements(gpusInfo, out minGPUErrorMessage);
            string minVMSpecErrorMessage = null;
            if (!minRequirementsOK)
            {
                minVMSpecErrorMessage = $"{minCPUErrorMessage} {minMemoryErrorMessage} {minStorageErrorMessage} {minGPUErrorMessage}";
            }

            var recVMSpec = new VirtualMachineSpec("recommended-vm");
            recVMSpec.Processors = REC_VM_PROCESSORS;
            recVMSpec.MemoryGB = REC_VM_MEMORY_GB;
            recVMSpec.GPUModel = REC_VM_GPUMODEL;
            recVMSpec.GPUMemoryGB = REC_VM_GPUMEMORY_GB;
            recVMSpec.ClusterDiskSizeGB = REC_VM_CLUSTERDISK_GB;
            recVMSpec.ClusterDiskIsSSD = true;
            recVMSpec.DataDiskSizeGB = REC_VM_DATADISK_GB;
            recVMSpec.DataDiskIsSSD = true;

            Dictionary<os.DriveInfo, int> recStorageReqsGB = null;
            string recCPUErrorMessage = null;
            string recMemoryErrorMessage = null;
            string recStorageErrorMessage = null;
            string recGPUErrorMessage = null;
            var recRequirementsOK =
               recVMSpec.CheckCPURequirements(cpuInfo, out recCPUErrorMessage) &&
               recVMSpec.CheckMemoryRequirements(memInfo, out recMemoryErrorMessage) &&
               recVMSpec.CheckStorageRequirements(hostStorage, out recStorageReqsGB, out recStorageErrorMessage) &&
               recVMSpec.CheckGPURequirements(gpusInfo, out recGPUErrorMessage);
            string recVMSpecErrorMessage = null;
            if (!recRequirementsOK)
            {
                recVMSpecErrorMessage = $"{recCPUErrorMessage} {recMemoryErrorMessage} {recStorageErrorMessage} {recGPUErrorMessage}";
            }

            var maxVMSpec = new VirtualMachineSpec("maximum-vm");
            maxVMSpec.Processors = (int)cpuInfo.NumberOfLogicalProcessors - MIN_HOST_PROCESSORS;
            maxVMSpec.MemoryGB = (int)(memInfo.TotalPhysicalMB/1024) - MIN_HOST_MEMORY_GB;

            var bestGPU = gpusInfo.OrderByDescending(gpu => (int)gpu.Architecture).ThenByDescending(gpu => gpu.MemoryMB).FirstOrDefault();
            maxVMSpec.GPUModel = bestGPU != null ? bestGPU.ModelName : null;
            maxVMSpec.GPUMemoryGB = bestGPU != null ? (bestGPU.MemoryMB/1024) : 0;
            
            var vmClusterDrive = Storage.GetDriveInfoFromPath(hostStorage.VirtualMachineClusterDirectory);
            var vmDataDrive = Storage.GetDriveInfoFromPath(hostStorage.VirtualMachineDataDirectory);

            if (recRequirementsOK)
            {
                maxVMSpec.ClusterDiskSizeGB = (int)(vmClusterDrive.TotalSizeMB / 1000) - recStorageReqsGB[vmClusterDrive] + REC_VM_CLUSTERDISK_GB;
                maxVMSpec.DataDiskSizeGB = (int)(vmDataDrive.TotalSizeMB / 1000) - recStorageReqsGB[vmDataDrive] + REC_VM_DATADISK_GB;
            }
            else if(minRequirementsOK)
            {
                maxVMSpec.ClusterDiskSizeGB = (int)(vmClusterDrive.TotalSizeMB / 1000) - minStorageReqsGB[vmClusterDrive] + MIN_VM_CLUSTERDISK_GB;
                maxVMSpec.DataDiskSizeGB = (int)(vmDataDrive.TotalSizeMB / 1000) - minStorageReqsGB[vmDataDrive] + MIN_VM_DATADISK_GB;
            }
            else
            {
                maxVMSpec.ClusterDiskSizeGB = (int)(vmClusterDrive.TotalSizeMB / 1000);
                maxVMSpec.DataDiskSizeGB = (int)(vmDataDrive.TotalSizeMB / 1000);
            }
            maxVMSpec.ClusterDiskIsSSD = vmClusterDrive.IsSSD;
            maxVMSpec.DataDiskIsSSD = vmDataDrive.IsSSD;

            foreach(var vmSpec in new VirtualMachineSpec[] { minVMSpec, recVMSpec, maxVMSpec })
            {
                vmSpec.HostSSHPort = DEFAULT_HOST_SSH_PORT;
                vmSpec.HostKubernetesPort = DEFAULT_HOST_Kubernetes_PORT;
                vmSpec.HostHttpIngressPort = DEFAULT_HOST_HttpIngress_PORT;
                vmSpec.HostHttpsIngressPort = DEFAULT_HOST_HttpsIngress_PORT;
            }

            return new RecommendedVMSpecs(minVMSpec, minRequirementsOK, minVMSpecErrorMessage, recVMSpec, recRequirementsOK, recVMSpecErrorMessage, maxVMSpec);
        }

        public static void ApplyRecommendedSpec(VirtualMachineConfig vmConfig)
        {
            vmConfig.Name = DEFAULT_LOCALVM_NAME;

            vmConfig.Processors = REC_VM_PROCESSORS;
            vmConfig.MemoryGB = REC_VM_MEMORY_GB;

            // No GPU by default
            vmConfig.GPUModel = null;
            vmConfig.GPUMemoryGB = 0;

            vmConfig.ClusterDiskSizeGB = REC_VM_CLUSTERDISK_GB;
            vmConfig.DataDiskSizeGB = REC_VM_DATADISK_GB;

            vmConfig.HostSSHPort = DEFAULT_HOST_SSH_PORT;
            vmConfig.HostKubernetesPort = DEFAULT_HOST_Kubernetes_PORT;
            vmConfig.HostHttpIngressPort = DEFAULT_HOST_HttpIngress_PORT;
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
