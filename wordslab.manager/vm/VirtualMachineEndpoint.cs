using wordslab.manager.storage;

namespace wordslab.manager.vm
{
    public class VirtualMachineEndpoint
    {
        public VirtualMachineEndpoint(string vmName, string ipAddress, int sshPort, int kubernetesPort, int httpIngressPort, string kubeconfig)
        {
            VMName = vmName;
            IPAddress = ipAddress;
            SSHPort = sshPort;
            KubernetesPort = kubernetesPort;
            HttpIngressPort = httpIngressPort;
            Kubeconfig = kubeconfig;
        }

        public string VMName { get; private set; }

        public string IPAddress { get; private set; }

        public int SSHPort { get; private set; }

        public int KubernetesPort { get; private set; }

        public int HttpIngressPort { get; private set; }

        public string Kubeconfig { get; private set; }

        public string KubeconfigPath { get; private set; }

        public static string GetEndpointFilePath(HostStorage storage, string name)
        {
            return Path.Combine(storage.ConfigDirectory, "vm", $"{name}.endpoint");
        }

        public static string GetKubeconfigFilePath(HostStorage storage, string name)
        {
            return Path.Combine(storage.ConfigDirectory, "vm", $"{name}.kubeconfig");
        }

        public void Save(HostStorage storage)
        {
            var endpointPath = GetEndpointFilePath(storage, VMName);
            Directory.CreateDirectory(Path.GetDirectoryName(endpointPath));
            using (StreamWriter sw = new StreamWriter(endpointPath))
            {
                sw.WriteLine(IPAddress);
                sw.WriteLine(SSHPort);
                sw.WriteLine(KubernetesPort);
                sw.WriteLine(HttpIngressPort);
            }

            KubeconfigPath = GetKubeconfigFilePath(storage, VMName);
            Directory.CreateDirectory(Path.GetDirectoryName(KubeconfigPath));
            File.WriteAllText(KubeconfigPath, Kubeconfig);
        }

        public static VirtualMachineEndpoint Load(HostStorage storage, string name)
        {
            string ipAddress = null;
            int sshPort, kubernetesPort, httpIngressPort;
            var endpointPath = GetEndpointFilePath(storage, name);
            if (File.Exists(endpointPath))
            {
                using (StreamReader sr = new StreamReader(endpointPath))
                {
                    ipAddress = sr.ReadLine();
                    sshPort = Int32.Parse(sr.ReadLine());
                    kubernetesPort = Int32.Parse(sr.ReadLine());
                    httpIngressPort = Int32.Parse(sr.ReadLine());
                }
            }
            else
            {
                throw new FileNotFoundException($"Could not find a VM endpoint file for {name} at path {endpointPath}");
            }

            string kubeconfig = null;
            var kubeconfigPath = GetKubeconfigFilePath(storage, name);
            if (File.Exists(kubeconfigPath))
            {
                kubeconfig = File.ReadAllText(kubeconfigPath);                
            }

            var endpoint = new VirtualMachineEndpoint(name, ipAddress, sshPort, kubernetesPort, httpIngressPort, kubeconfig);
            endpoint.KubeconfigPath = kubeconfigPath;
            return endpoint;
        }

        public void Delete(HostStorage storage)
        {
            if(File.Exists(KubeconfigPath))
            {
                File.Delete(KubeconfigPath);
            }

            var endpointPath = GetEndpointFilePath(storage, VMName);
            if (File.Exists(endpointPath))
            {
                File.Delete(endpointPath);
            }
        }
    }
}
