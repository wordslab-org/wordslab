using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm.wsl
{
    public class WslVM : VirtualMachine
    {
        public const string VM_DISTRIB_NAME = "wordslab-vm";
        public const string CLUSTER_DISTRIB_NAME = "wordslab-cluster-disk";
        public const string DATA_DISTRIB_NAME = "wordslab-data-disk";

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

            /*var vmEndPoint = new VMEndpoint();
            var ip = Wsl.execShell("hostname -I | grep -Eo \"^[0-9\\.]+\"", VM_DISTRIB_NAME);
            vmEndPoint.Address = new System.Net.IPAddress(ip);
            var kubeconfig = wsl.execShell("cat /etc/rancher/k3s/k3s.yaml", VM_DISTRIB_NAME);
            kubeconfigPath = Path.Combine(storage.ConfigDirectory.FullName, ".kube", "config");
            Directory.CreateDirectory(kubeconfigPath);
            using (StreamWriter sw = new StreamWriter(kubeconfigPath))
            {
                sw.Write(kubeconfig);
            }*/

            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }


        public static void StartCluster(HostStorage storage, out int vmIP, out string kubeconfigPath)
        {
            throw new NotImplementedException();
        }

        public static void StopCluster()
        {
            Wsl.terminate(VM_DISTRIB_NAME);
            Wsl.terminate(CLUSTER_DISTRIB_NAME);
            Wsl.terminate(DATA_DISTRIB_NAME);
        }
    }
}
