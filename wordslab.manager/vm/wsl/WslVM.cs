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
            
            var vmNames = WslDisk.ListVMNamesFromClusterDisks(storage);            
            var wslDistribs = Wsl.list();
            foreach(var vmName in wslDistribs.Join(vmNames, d => d.Distribution, name => VirtualDisk.GetServiceName(name, VirtualDiskFunction.Cluster), (d,n) => n).OrderBy(s => s))
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
            var clusterDisk = WslDisk.TryFindByName(vmName, VirtualDiskFunction.Cluster, storage);
            if(clusterDisk == null)
            {
                return null;
            }

            var dataDisk = WslDisk.TryFindByName(vmName, VirtualDiskFunction.Data, storage);
            if (dataDisk == null)
            {
                throw new FileNotFoundException($"Could not find virtual disks for a local virtual machine named {vmName}");
            }

            var wslConfig = Wsl.Read_wslconfig();
            return new WslVM(vmName, wslConfig.processors.Value, wslConfig.memoryMB.Value/1024, clusterDisk, dataDisk, storage);
        }


        public WslVM(string vmName, int processors, int memoryGB, VirtualDisk clusterDisk, VirtualDisk dataDisk, HostStorage storage) 
            : base(vmName, processors, memoryGB, clusterDisk, dataDisk, storage) 
        {
            Type = VirtualMachineType.Wsl;
        }

        public override bool IsRunning()
        {
            try
            {
                var wslDistribs = Wsl.list();
                return wslDistribs.Any(d => d.Distribution == ClusterDisk.ServiceName && d.IsRunning);
            }
            catch { }
            return false;
        }

        public override VirtualMachineEndpoint Start(int? processors = null, int? memoryGB = null, int? hostSSHPort = null, int? hostKubernetesPort = null, int? hostHttpIngressPort = null, int? hostHttpsIngressPort = null)
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
            if (hostHttpsIngressPort.HasValue) HostHttpsIngressPort = hostHttpsIngressPort.Value;

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
            if (usedPorts.Contains(HostHttpsIngressPort))
            {
                throw new InvalidOperationException($"Host port for HTTPS ingress: {HostHttpsIngressPort} is already in use, please select another port");
            }

            // Start the VM
            DataDisk.StartService();
            ClusterDisk.StartService();

            // Start k3s inside the virtual machine
            Wsl.execShell($"/root/{WslDisk.clusterDiskStartupScript} {DataDisk.ServiceName}", ClusterDisk.ServiceName, ignoreError: "screen size is bogus");

            // Here's an example PowerShell command to add a port proxy that listens on port 4000 on the host and connects it to port 4000 to the WSL 2 VM with IP address 192.168.101.100.
            // netsh interface portproxy add v4tov4 listenport=4000 listenaddress=0.0.0.0 connectport=4000 connectaddress=192.168.101.100

            // Get virtual machine IP and kubeconfig
            string ip = null;
            Wsl.execShell("hostname -I | grep -Eo \"^[0-9\\.]+\"", ClusterDisk.ServiceName, outputHandler: output => ip = output);
            string kubeconfig = null;
            Wsl.execShell("cat /etc/rancher/k3s/k3s.yaml", ClusterDisk.ServiceName, outputHandler: output => kubeconfig = output);
            
            // Save it to the endpoint & kubeconfig files
            endpoint = new VirtualMachineEndpoint(Name, ip, 0, HostKubernetesPort, HostHttpIngressPort, HostHttpsIngressPort, kubeconfig);
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
                Wsl.terminate(ClusterDisk.ServiceName);
                DataDisk.StopService();
            }
        }
    }
}
