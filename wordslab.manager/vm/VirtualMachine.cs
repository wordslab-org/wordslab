using System.Text.RegularExpressions;
using wordslab.manager.storage;
using wordslab.manager.storage.config;

namespace wordslab.manager.vm
{
    public enum VirtualMachineType
    {
        Wsl,
        Qemu,
        GoogleCloud
    }

    public abstract class VirtualMachine
    {
        protected HostStorage storage;

        protected VirtualMachine(string vmName, int processors, int memoryGB, VirtualDisk osDisk, VirtualDisk clusterDisk, VirtualDisk dataDisk, HostStorage storage)
        {
            if(!NAME_REGEX.IsMatch(vmName))
            {
                throw new ArgumentException("A virtual machine name can only contain lowercase letters, digits and -");
            }
            Name = vmName;
            Processors = processors;
            MemoryGB = memoryGB;
            OsDisk = osDisk;
            ClusterDisk = clusterDisk;
            DataDisk = dataDisk;
            this.storage = storage;
        }

        public VirtualMachineType Type { get; internal set; }

        private static readonly Regex NAME_REGEX = new Regex("^[a-z0-9-]+$");

        public string Name { get; internal set; }

        public int Processors { get; internal set; }

        public int MemoryGB { get; internal set; }

        public string GPUModel { get; internal set; }

        public int GPUMemoryGB { get; internal set; }

        public VirtualDisk OsDisk { get; internal set; }

        public VirtualDisk ClusterDisk { get; internal set; }

        public VirtualDisk DataDisk { get; internal set; }

        public int RequestedSSHPort { get; internal set; }
        public int RequestedKubernetesPort { get; internal set; }
        public int RequestedHttpIngressPort { get; internal set; }

        public abstract bool IsRunning();

        public abstract VMEndpoint Start(VirtualMachineConfig vmSpec);

        protected VMEndpoint endpoint;

        public VMEndpoint Endpoint
        {
            get
            {
                if(endpoint == null)
                {
                    if(IsRunning())
                    {
                        endpoint = VMEndpoint.Load(storage, Name);
                    }
                }
                return endpoint;
            }
        }

        public string KubeconfigPath => Path.Combine(storage.ConfigDirectory, ".kube", $"{Name}.config");

        public abstract void Stop();

        // --- wordslab virtual machine software ---

        // Versions last updated : January 9 2022

        // Rancher k3s releases: https://github.com/k3s-io/k3s/releases/
        internal static readonly string k3sVersion = "1.22.5+k3s1";
        internal static readonly string k3sExecutableURL = $"https://github.com/k3s-io/k3s/releases/download/v{k3sVersion}/k3s";
        internal static readonly int    k3sExecutableSize = 53473280;
        internal static readonly string k3sExecutableFileName = $"k3s-{k3sVersion}";
        internal static readonly string k3sImagesURL = $"https://github.com/k3s-io/k3s/releases/download/v{k3sVersion}/k3s-airgap-images-amd64.tar";
        internal static readonly int    k3sImagesSize = 492856320;
        internal static readonly string k3sImagesFileName = $"k3s-airgap-images-{k3sVersion}.tar";

        // Helm releases: https://github.com/helm/helm/releases
        internal static readonly string helmVersion = "3.7.2";
        internal static readonly string helmExecutableURL = $"https://get.helm.sh/helm-v{helmVersion}-linux-amd64.tar.gz";
        internal static readonly int    helmExecutableSize = 45731840; // 13870692 compressed
        internal static readonly int    helmExtractedSize = 0;
        internal static readonly string helmFileName = $"heml-{helmVersion}.tar";

        // nvidia container runtime versions: https://github.com/NVIDIA/nvidia-container-runtime/releases
        internal static readonly string nvidiaContainerRuntimeVersion = "3.7.0-1";
    }

    public class VMEndpoint
    {
        public VMEndpoint(string vmName, string ipAddress, int sshPort, int kubernetesPort, int httpIngressPort)
        {
            VMName = vmName;
            IPAddress = ipAddress;
            SSHPort = sshPort;
            KubernetesPort = kubernetesPort;
            HttpIngressPort = httpIngressPort; 
        }

        public string VMName {  get; private set; }

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
            using(StreamWriter sw = new StreamWriter(GetFilePath(storage, VMName)))
            {
                sw.WriteLine(IPAddress);
                sw.WriteLine(SSHPort);
                sw.WriteLine(KubernetesPort);
                sw.WriteLine(HttpIngressPort);
            }
        }

        public static VMEndpoint Load(HostStorage storage, string name)
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
                    return new VMEndpoint(name, ipAddress, sshPort, kubernetesPort, httpIngressPort);
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
