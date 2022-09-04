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

        public override VirtualMachineEndpoint Start(int? processors = null, int? memoryGB = null, int? hostSSHPort = null, int? hostKubernetesPort = null, int? hostHttpIngressPort = null)
        {
            if (IsRunning())
            {
                return Endpoint;
            }

            // Update VM properties default values
            if (processors.HasValue) Processors = processors.Value;
            if (memoryGB.HasValue) MemoryGB = memoryGB.Value;
            if (hostSSHPort.HasValue) HostSSHPort = hostSSHPort.Value;
            if (hostKubernetesPort.HasValue) HostKubernetesPort = hostKubernetesPort.Value;
            if (hostHttpIngressPort.HasValue) HostHttpIngressPort = hostHttpIngressPort.Value;

            // Check if the requested ports are available right before startup
            var usedPorts = Network.GetAllTcpPortsInUse();
            if (usedPorts.Contains(HostSSHPort))
            {
                throw new InvalidOperationException($"Host port for SSH: {HostSSHPort} is already in use, please select another port");
            }
            if (usedPorts.Contains(HostKubernetesPort))
            {
                throw new InvalidOperationException($"Host port for Kubernetes: {HostKubernetesPort} is already in use, please select another port");
            }
            if (usedPorts.Contains(HostHttpIngressPort))
            {
                throw new InvalidOperationException($"Host port for HTTP ingress: {HostHttpIngressPort} is already in use, please select another port");
            }

            // Start qemu process
            processId = Qemu.StartVirtualMachine(Processors, MemoryGB, OsDisk.StoragePath, ClusterDisk.StoragePath, DataDisk.StoragePath, HostSSHPort, HostHttpIngressPort, HostKubernetesPort);
               
            // Start k3s inside the virtual machine
            SshClient.ExecuteRemoteCommand("ubuntu", "127.0.0.1", HostSSHPort, $"sudo ./{QemuDisk.k3sStartupScript}");

            // Get virtual machine IP and kubeconfig
            string ip = null;
            SshClient.ExecuteRemoteCommand("ubuntu", "127.0.0.1", HostSSHPort, "hostname -I | grep -Eo \"^[0-9\\.]+\"", outputHandler: output => ip = output);
            string kubeconfig = null;
            SshClient.ExecuteRemoteCommand("ubuntu", "127.0.0.1", HostSSHPort, "cat /etc/rancher/k3s/k3s.yaml", outputHandler: output => kubeconfig = output);

            // Save it to the endpoint & kubeconfig files
            endpoint = new VirtualMachineEndpoint(Name, ip, HostSSHPort, HostKubernetesPort, HostHttpIngressPort, kubeconfig);
            endpoint.Save(storage);
            return endpoint;
        }

        public override void Stop()
        {
            if (endpoint != null)
            {
                endpoint.Delete(storage);
                endpoint = null;
            }

            if (IsRunning())
            {
                Qemu.StopVirtualMachine(processId);
            }
            processId = -1;
        }
    }
}
