using wordslab.manager.os;
using wordslab.manager.storage;
using wordslab.manager.storage.config;

namespace wordslab.manager.vm.wsl
{
    public class WslVM : VirtualMachine
    {
        public static List<VirtualMachine> ListLocalVMs(HostStorage storage)
        {
            var vms = new List<VirtualMachine>();
            
            var vmNames = WslDisk.ListVMNamesFromOsDisks(storage);            
            var wslDistribs = Wsl.list();
            foreach(var vmName in wslDistribs.Join(vmNames, d => d.Distribution, name => VirtualDisk.GetServiceName(name, VirtualDiskFunction.OS), (d,n) => n).OrderBy(s => s))
            {
                try
                {
                    var vm = TryFindByName(vmName, storage);
                    vms.Add(vm);
                }
                catch { }
            }
            return vms;
        }

        public static VirtualMachine TryFindByName(string vmName, HostStorage storage)
        {
            var osDisk = WslDisk.TryFindByName(vmName, VirtualDiskFunction.OS, storage);
            if(osDisk == null)
            {
                return null;
            }

            var clusterDisk = WslDisk.TryFindByName(vmName, VirtualDiskFunction.Cluster, storage);
            var dataDisk = WslDisk.TryFindByName(vmName, VirtualDiskFunction.Data, storage);
            if (clusterDisk == null || dataDisk == null)
            {
                throw new FileNotFoundException($"Could not find virtual disks for a local virtual machine named {vmName}");
            }

            var wslConfig = Wsl.Read_wslconfig();
            return new WslVM(vmName, wslConfig.processors.Value, wslConfig.memoryMB.Value/1024, osDisk, clusterDisk, dataDisk, storage);
        }


        public WslVM(string vmName, int processors, int memoryGB, VirtualDisk osDisk, VirtualDisk clusterDisk, VirtualDisk dataDisk, HostStorage storage) 
            : base(vmName, processors, memoryGB, osDisk, clusterDisk, dataDisk, storage) 
        {
            Type = VirtualMachineType.Wsl;
        }

        public override bool IsRunning()
        {
            try
            {
                var wslDistribs = Wsl.list();
                return wslDistribs.Any(d => d.Distribution == OsDisk.ServiceName && d.IsRunning);
            }
            catch { }
            return false;
        }

        public override VirtualMachineEndpoint Start(int? processors = null, int? memoryGB = null, int? hostSSHPort = null, int? hostKubernetesPort = null, int? hostHttpIngressPort = null)
        {
            if (IsRunning())
            {
                return Endpoint;
            }

            // Update VM properties default values
            if (processors.HasValue) Processors = processors.Value;
            if (memoryGB.HasValue) MemoryGB = memoryGB.Value;
            // HostSSHPort is always 0 because we don't use SSH to launch WSL commands on Windows
            if (hostKubernetesPort.HasValue) HostKubernetesPort = hostKubernetesPort.Value;
            if (hostHttpIngressPort.HasValue) HostHttpIngressPort = hostHttpIngressPort.Value;

            // Update machine-wide WSL config if needed and allowed
            var wslConfig = Wsl.Read_wslconfig();
            wslConfig.UpdateToVMSpec(Processors, MemoryGB, restartIfNeeded: true);

            // Check if the requested ports are available right before startup
            var usedPorts = Network.GetAllTcpPortsInUse();
            if (usedPorts.Contains(HostKubernetesPort))
            {
                throw new InvalidOperationException($"Host port for Kubernetes: {HostKubernetesPort} is already in use, please select another port");
            }
            if (usedPorts.Contains(HostHttpIngressPort))
            {
                throw new InvalidOperationException($"Host port for HTTP ingress: {HostHttpIngressPort} is already in use, please select another port");
            }

            // Start the VM
            DataDisk.StartService();
            ClusterDisk.StartService();

            // Start k3s inside the virtual machine
            Wsl.execShell($"/root/{WslDisk.vmStartupScript} {ClusterDisk.ServiceName} {DataDisk.ServiceName} {HostKubernetesPort} {HostHttpIngressPort}", OsDisk.ServiceName, ignoreError: "screen size is bogus");

            // Get virtual machine IP and kubeconfig
            string ip = null;
            Wsl.execShell("hostname -I | grep -Eo \"^[0-9\\.]+\"", OsDisk.ServiceName, outputHandler: output => ip = output);
            string kubeconfig = null;
            Wsl.execShell("cat /etc/rancher/k3s/k3s.yaml", OsDisk.ServiceName, outputHandler: output => kubeconfig = output);
            
            // Save it to the endpoint & kubeconfig files
            endpoint = new VirtualMachineEndpoint(Name, ip, 0, HostKubernetesPort, HostHttpIngressPort, kubeconfig);
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
                Wsl.terminate(OsDisk.ServiceName);
                ClusterDisk.StopService();
                DataDisk.StopService();
            }
        }
    }
}
