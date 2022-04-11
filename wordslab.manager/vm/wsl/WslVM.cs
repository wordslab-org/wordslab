using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm.wsl
{
    public class WslVM : VirtualMachine
    {
        public const string VM_DISTRIB_NAME = "wordslab-vm";
        public const string CLUSTER_DISTRIB_NAME = "wordslab-cluster-disk";
        public const string DATA_DISTRIB_NAME = "wordslab-data-disk";

        public const int MIN_CORES = 2;
        public const int MIN_MEMORY_GB = 4;

        public WslVM(string name, int processors, int memoryGB, int osDiskSizeGB, int clusterDiskSizeGB, int dataDiskSizeGB, HostStorage storage) 
            : base(name, processors, memoryGB, osDiskSizeGB, clusterDiskSizeGB, dataDiskSizeGB, storage) 
        { }

        public override bool IsRunning()
        {
            try
            {
                var wslDistribs = Wsl.list();
                return wslDistribs.Any(d => d.Distribution == VM_DISTRIB_NAME && d.IsRunning);
            }
            catch { }
            return false;
        }

        public override VMEndpoint Start()
        {
            Wsl.execShell("/root/wordslab-data-start.sh", DATA_DISTRIB_NAME);
            Wsl.execShell("/root/wordslab-cluster-start.sh", CLUSTER_DISTRIB_NAME);
            Wsl.execShell("/root/wordslab-vm-start.sh", VM_DISTRIB_NAME, ignoreError: "screen size is bogus");

            string ip = null;
            Wsl.execShell("hostname -I | grep -Eo \"^[0-9\\.]+\"", VM_DISTRIB_NAME, outputHandler: output => ip = output);
            var kubeconfig = Wsl.execShell("cat /etc/rancher/k3s/k3s.yaml", VM_DISTRIB_NAME);
            var kubeconfigPath = Path.Combine(storage.ConfigDirectory, ".kube", "wslvm.config");
            Directory.CreateDirectory(kubeconfigPath);
            using (StreamWriter sw = new StreamWriter(kubeconfigPath))
            {
                sw.Write(kubeconfig);
            }

            var vmEndpoint = new VMEndpoint(Name, ip, 0, kubeconfigPath);
            return vmEndpoint;
        }

        public override void Stop()
        {
            Wsl.terminate(VM_DISTRIB_NAME);
            Wsl.terminate(CLUSTER_DISTRIB_NAME);
            Wsl.terminate(DATA_DISTRIB_NAME);
        }
    }
}
