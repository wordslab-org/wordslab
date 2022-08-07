using wordslab.manager.os;
using wordslab.manager.storage;
using wordslab.manager.storage.config;

namespace wordslab.manager.vm.qemu
{
    public class QemuVM : VirtualMachine
    {
        public static List<VirtualMachine> ListLocalVMs(HostStorage storage)
        {
            var vms = new List<VirtualMachine>();
            try
            {
                var vmNames = QemuDisk.ListVMNamesFromOsDisks(storage);                
                foreach (var vmName in vmNames)
                {
                    vms.Add(TryFindByName(vmName, storage));
                }
            }
            catch { }
            return vms;
        }

        public static VirtualMachine TryFindByName(string vmName, HostStorage storage)
        {
            var osDisk = QemuDisk.TryFindByName(vmName, VirtualDiskFunction.OS, storage);
            var clusterDisk = QemuDisk.TryFindByName(vmName, VirtualDiskFunction.Cluster, storage);
            var dataDisk = QemuDisk.TryFindByName(vmName, VirtualDiskFunction.Data, storage);
            if (osDisk == null || clusterDisk == null || dataDisk == null)
            {
                throw new Exception($"Could not find virtual disks for a local virtual machine named {vmName}");
            }

            var qemuProc = Qemu.TryFindVirtualMachineProcess(osDisk.StoragePath);
            if(qemuProc == null)
            {
                throw new Exception($"Could not find a running qemu process for a local virtual machine named {vmName}");
            }

            return new QemuVM(vmName, qemuProc.Processors, qemuProc.MemoryGB, osDisk, clusterDisk, dataDisk, storage);
        }

        internal QemuVM(string name, int processors, int memoryGB, VirtualDisk osDisk, VirtualDisk clusterDisk, VirtualDisk dataDisk, HostStorage storage) 
            : base(name, processors, memoryGB, osDisk, clusterDisk, dataDisk, storage) 
        {
            Type = VirtualMachineType.Qemu;        
        }

        public override bool IsRunning()
        {
            var qemuProc = Qemu.TryFindVirtualMachineProcess(OsDisk.StoragePath);
            if(qemuProc != null)
            {
                processId = qemuProc.PID;
            }
            else
            {
                processId = -1;
                endpoint = null;
            }
            return qemuProc != null;
        }

        private int processId = -1;

        public override VirtualMachineEndpoint Start(VirtualMachineConfig vmSpec)
        {
            // Start qemu process
            processId = Qemu.StartVirtualMachine(vmSpec.Processors, vmSpec.MemoryGB, OsDisk.StoragePath, ClusterDisk.StoragePath, DataDisk.StoragePath, vmSpec.HostSSHPort, vmSpec.HostHttpIngressPort, vmSpec.HostKubernetesPort);
            Processors = vmSpec.Processors;
            MemoryGB = vmSpec.MemoryGB;

            // Start k3s inside virtual machine
            SshClient.ExecuteRemoteCommand("ubuntu", "127.0.0.1", vmSpec.HostSSHPort, $"sudo ./{QemuDisk.k3sStartupScript}");

            // Get virtual machine IP and kubeconfig file
            string ip = null;
            SshClient.ExecuteRemoteCommand("ubuntu", "127.0.0.1", vmSpec.HostSSHPort, "hostname -I | grep -Eo \"^[0-9\\.]+\"", outputHandler: output => ip = output);
            string kubeconfig = null;
            SshClient.ExecuteRemoteCommand("ubuntu", "127.0.0.1", vmSpec.HostSSHPort, "cat /etc/rancher/k3s/k3s.yaml", outputHandler: output => kubeconfig = output);
            Directory.CreateDirectory(KubeconfigPath);
            using (StreamWriter sw = new StreamWriter(KubeconfigPath))
            {
                sw.Write(kubeconfig);
            }

            endpoint = new VirtualMachineEndpoint(Name, ip, vmSpec.HostSSHPort, vmSpec.HostKubernetesPort, vmSpec.HostHttpIngressPort);
            endpoint.Save(storage);
            return endpoint;
        }

        public override void Stop()
        {
            if (IsRunning())
            {
                Qemu.StopVirtualMachine(processId);
            }
            processId = -1;
            endpoint = null;
        }
    }
}
