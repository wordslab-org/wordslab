using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm.wsl
{
    public class WslVM : VirtualMachine
    {
        public static VirtualMachine TryFindByName(string vmName, HostStorage storage)
        {
            var osDisk = WslDisk.TryFindByName(vmName, VirtualDiskFunction.OS, storage);
            var clusterDisk = WslDisk.TryFindByName(vmName, VirtualDiskFunction.Cluster, storage);
            var dataDisk = WslDisk.TryFindByName(vmName, VirtualDiskFunction.Data, storage);
            if (osDisk == null || clusterDisk == null || dataDisk == null)
            {
                throw new Exception($"Could not find virtual disks for a local virtual machine named {vmName}");
            }

            var wslConfig = Wsl.Read_wslconfig();
            return new WslVM(vmName, wslConfig.processors.Value, wslConfig.memoryMB.Value/1024, osDisk, clusterDisk, dataDisk, storage);
        }


        public WslVM(string vmName, int processors, int memoryGB, VirtualDisk osDisk, VirtualDisk clusterDisk, VirtualDisk dataDisk, HostStorage storage) 
            : base(vmName, processors, memoryGB, osDisk, clusterDisk, dataDisk, storage) 
        { }

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

        public override VMEndpoint Start(VirtualMachineSpec vmSpec)
        {
            DataDisk.StartService();
            ClusterDisk.StartService();

            Wsl.execShell($"/root/{WslDisk.vmStartupScript} {vmSpec.HostKubernetesPort}", OsDisk.ServiceName, ignoreError: "screen size is bogus");

            string ip = null;
            Wsl.execShell("hostname -I | grep -Eo \"^[0-9\\.]+\"", OsDisk.ServiceName, outputHandler: output => ip = output);
            var kubeconfig = Wsl.execShell("cat /etc/rancher/k3s/k3s.yaml", OsDisk.ServiceName);
            Directory.CreateDirectory(KubeconfigPath);
            using (StreamWriter sw = new StreamWriter(KubeconfigPath))
            {
                sw.Write(kubeconfig);
            }

            endpoint = new VMEndpoint(Name, ip, 0, vmSpec.HostKubernetesPort, vmSpec.HostHttpIngressPort);
            return endpoint;
        }

        public override void Stop()
        {            
            endpoint = null;
            VMEndpoint.Delete(storage, Name);

            Wsl.terminate(OsDisk.ServiceName);
            ClusterDisk.StopService();
            DataDisk.StopService();
        }
    }
}
