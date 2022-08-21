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

        public override VirtualMachineEndpoint Start(VirtualMachineConfig vmSpec)
        {
            DataDisk.StartService();
            ClusterDisk.StartService();

            Wsl.execShell($"/root/{WslDisk.vmStartupScript} {vmSpec.HostKubernetesPort}", OsDisk.ServiceName, ignoreError: "screen size is bogus");

            string ip = null;
            Wsl.execShell("hostname -I | grep -Eo \"^[0-9\\.]+\"", OsDisk.ServiceName, outputHandler: output => ip = output);
            var kubeconfig = Wsl.execShell("cat /etc/rancher/k3s/k3s.yaml", OsDisk.ServiceName);
            Directory.CreateDirectory(Path.GetDirectoryName(KubeconfigPath));
            using (StreamWriter sw = new StreamWriter(KubeconfigPath))
            {
                sw.Write(kubeconfig);
            }

            endpoint = new VirtualMachineEndpoint(Name, ip, 0, vmSpec.HostKubernetesPort, vmSpec.HostHttpIngressPort);
            endpoint.Save(storage);
            return endpoint;
        }

        public override void Stop()
        {            
            endpoint = null;
            VirtualMachineEndpoint.Delete(storage, Name);

            Wsl.terminate(OsDisk.ServiceName);
            ClusterDisk.StopService();
            DataDisk.StopService();
        }
    }
}
