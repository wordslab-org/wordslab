using wordslab.manager.storage;

namespace wordslab.manager.vm
{
    public class VirtualMachineEndpoint
    {
        public VirtualMachineEndpoint(string vmName, string ipAddress, int sshPort, int kubernetesPort, int httpIngressPort)
        {
            VMName = vmName;
            IPAddress = ipAddress;
            SSHPort = sshPort;
            KubernetesPort = kubernetesPort;
            HttpIngressPort = httpIngressPort;
        }

        public string VMName { get; private set; }

        public string IPAddress { get; private set; }

        public int SSHPort { get; private set; }

        public int KubernetesPort { get; private set; }

        public int HttpIngressPort { get; private set; }

        public static string GetFilePath(HostStorage storage, string name)
        {
            return Path.Combine(storage.ConfigDirectory, "vm", $"{name}.endpoint");
        }

        public void Save(HostStorage storage)
        {
            using (StreamWriter sw = new StreamWriter(GetFilePath(storage, VMName)))
            {
                sw.WriteLine(IPAddress);
                sw.WriteLine(SSHPort);
                sw.WriteLine(KubernetesPort);
                sw.WriteLine(HttpIngressPort);
            }
        }

        public static VirtualMachineEndpoint Load(HostStorage storage, string name)
        {
            var filepath = GetFilePath(storage, name);
            if (File.Exists(filepath))
            {
                using (StreamReader sr = new StreamReader(filepath))
                {
                    var ipAddress = sr.ReadLine();
                    var sshPort = Int32.Parse(sr.ReadLine());
                    var kubernetesPort = Int32.Parse(sr.ReadLine());
                    var httpIngressPort = Int32.Parse(sr.ReadLine());
                    return new VirtualMachineEndpoint(name, ipAddress, sshPort, kubernetesPort, httpIngressPort);
                }
            }
            else
            {
                return null;
            }
        }

        public static void Delete(HostStorage storage, string name)
        {
            var filepath = GetFilePath(storage, name);
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }
        }
    }
}
