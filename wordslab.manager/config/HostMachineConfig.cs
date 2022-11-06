using System.ComponentModel.DataAnnotations;
using wordslab.manager.storage;

namespace wordslab.manager.config
{
    public class HostMachineConfig : BaseConfig
    {
        public HostMachineConfig() { }

        public HostMachineConfig(
            string hostName, HostStorage storage, 
            int processors, int memoryGB, bool canUseGPUs,
            int vmClusterSizeGB, int vmDataSizeGB, int backupSizeGB,
            int sshPort, int kubernetesPort, int httpPort, bool canExposeHttpOnLAN, int httpsPort, bool canExposeHttpsOnLAN)
        {
            HostName = hostName;
            // Host storage dirs
            VirtualMachineClusterPath = storage.VirtualMachineClusterDirectory;
            VirtualMachineDataPath = storage.VirtualMachineDataDirectory;
            BackupPath = storage.BackupDirectory;
            // Compute
            Processors = processors;
            MemoryGB = memoryGB;
            CanUseGPUs = canUseGPUs;
            // Storage
            VirtualMachineClusterSizeGB = vmClusterSizeGB;
            VirtualMachineDataSizeGB = vmDataSizeGB;
            BackupSizeGB = backupSizeGB;
            // Network
            SSHPort = sshPort;
            KubernetesPort = kubernetesPort;
            HttpPort = httpPort;
            CanExposeHttpOnLAN = canExposeHttpOnLAN;
            HttpsPort = httpsPort;
            CanExposeHttpsOnLAN = canExposeHttpsOnLAN;
        }

        [Key]
        public string HostName { get; set; }

        // Host storage directories

        public string VirtualMachineClusterPath { get; set; }

        public string VirtualMachineDataPath { get; set; }

        public string BackupPath { get; set; }

        // Sandbox limits

        // --- Compute ---

        public int Processors { get; set; }

        public int MemoryGB { get; set; }

        public bool CanUseGPUs { get; set; }

        // -- Storage ---

        public int VirtualMachineClusterSizeGB { get; set; }

        public int VirtualMachineDataSizeGB { get; set; }

        public int BackupSizeGB { get; set; }

        // --- Network ---

        public int SSHPort { get; set; }

        public int KubernetesPort { get; set; }

        public int HttpPort { get; set; }

        public bool CanExposeHttpOnLAN { get; set; }

        public int HttpsPort { get; set; }

        public bool CanExposeHttpsOnLAN { get; set; }
    }
}
