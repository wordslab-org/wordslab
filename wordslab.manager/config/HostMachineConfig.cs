using System.ComponentModel.DataAnnotations;
using wordslab.manager.storage;

namespace wordslab.manager.config
{
    public class HostMachineConfig : BaseConfig
    {
        private HostMachineConfig() { }

        public HostMachineConfig(
            string hostName, HostStorage storage, 
            int processors, int memoryGB, bool canUseGPUs,
            string vmClusterPath, int vmClusterSizeGB, string vmDataPath , int vmDataSizeGB, string backupPath, int backupSizeGB,
            Range sshPorts, Range kubernetesPorts, Range httpPorts, bool canExposeHttpOnLAN, Range httpsPorts, bool canExposeHttpsOnLAN)
        {
            HostName = hostName;
            // Compute
            Processors = processors;
            MemoryGB = memoryGB;
            CanUseGPUs = canUseGPUs;
            // Storage
            if(vmClusterPath != storage.VirtualMachineClusterDirectory)
            {
                storage.MoveConfigurableDirectoryTo(StorageLocation.VirtualMachineCluster, vmClusterPath);
            }
            VirtualMachineClusterPath = vmClusterPath;
            VirtualMachineClusterSizeGB = vmClusterSizeGB;
            if (vmDataPath != storage.VirtualMachineDataDirectory)
            {
                storage.MoveConfigurableDirectoryTo(StorageLocation.VirtualMachineData, vmDataPath);
            }
            VirtualMachineDataPath = vmDataPath;
            VirtualMachineDataSizeGB = vmDataSizeGB;
            if (backupPath != storage.BackupDirectory)
            {
                storage.MoveConfigurableDirectoryTo(StorageLocation.Backup, backupPath);
            }
            BackupPath = backupPath;
            BackupSizeGB = backupSizeGB;
            // Network
            SSHPorts = sshPorts;
            KubernetesPorts = kubernetesPorts;
            HttpPorts = httpPorts;
            CanExposeHttpOnLAN = canExposeHttpOnLAN;
            HttpsPorts = httpsPorts;
            CanExposeHttpsOnLAN = canExposeHttpsOnLAN;
        }

        [Key]
        public string HostName { get; set; }

        // Sandbox limits

        // --- Compute ---

        public int Processors { get; set; }

        public int MemoryGB { get; set; }

        public bool CanUseGPUs { get; set; }

        // -- Storage ---

        public string VirtualMachineClusterPath { get; set; }

        public int VirtualMachineClusterSizeGB { get; set; }

        public string VirtualMachineDataPath { get; set; }

        public int VirtualMachineDataSizeGB { get; set; }

        public string BackupPath { get; set; }

        public int BackupSizeGB { get; set; }

        // --- Network ---

        public Range SSHPorts { get; private set; }

        public Range KubernetesPorts { get; private set; }

        public Range HttpPorts { get; private set; }

        public bool CanExposeHttpOnLAN { get; set; }

        public Range HttpsPorts { get; private set; }

        public bool CanExposeHttpsOnLAN { get; set; }
    }
}
