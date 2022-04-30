using System.Net;
using System.Text.RegularExpressions;
using wordslab.manager.storage;

namespace wordslab.manager.vm
{
    public abstract class VirtualMachine
    {
        protected HostStorage storage;

        public VirtualMachine(string name, int processors, int memoryGB, int osDiskSizeGB, int clusterDiskSizeGB, int dataDiskSizeGB, HostStorage storage)
        {
            if(!NAME_REGEX.IsMatch(name))
            {
                throw new ArgumentException("A virtual machine name can only contain lowercase letters, digits and -");
            }
            Name = name;
            Processors = processors;
            MemoryGB = memoryGB;

            this.osDiskSizeGB = osDiskSizeGB;
            this.clusterDiskSizeGB = clusterDiskSizeGB;
            this.dataDiskSizeGB = dataDiskSizeGB;

            this.storage = storage;
        }

        private static readonly Regex NAME_REGEX = new Regex("^[a-z0-9-]+$");

        public string Name { get; protected set; }

        public int Processors { get; protected set; }

        public int MemoryGB { get; protected set; }

        protected int osDiskSizeGB;
        protected int clusterDiskSizeGB;
        protected int dataDiskSizeGB;

        public VirtualDisk OsDisk { get; protected set; }

        public VirtualDisk ClusterDisk { get; protected set; }

        public VirtualDisk DataDisk { get; protected set; }

        public abstract bool IsRunning();

        public abstract VMEndpoint Start();

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
        public VMEndpoint(string name, string ip, int sshPort, string kubeConfigPath)
        {
            Name = name;
            Address = IPAddress.Parse(ip);
            SSHPort = sshPort;
            KubeConfigPath = kubeConfigPath;
        }

        public string Name {  get; private set; }

        public IPAddress Address { get; private set; }

        public int SSHPort { get; private set; }

        public string KubeConfigPath { get; private set; }

        public static string GetFilePath(HostStorage storage, string name)
        {
            return Path.Combine(storage.ConfigDirectory, "vm", $"{name}.endpoint");
        }

        public void Save(HostStorage storage)
        {            
            using(StreamWriter sw = new StreamWriter(GetFilePath(storage, Name)))
            {
                sw.WriteLine(Address.ToString());
                sw.WriteLine(SSHPort);
                sw.WriteLine(KubeConfigPath);
            }
        }

        public static VMEndpoint Load(HostStorage storage, string name)
        {
            var filepath = GetFilePath(storage, name);
            if (File.Exists(filepath))
            {
                using (StreamReader sr = new StreamReader(filepath))
                {
                    var ip = sr.ReadLine();
                    var sshPort = Int32.Parse(sr.ReadLine());
                    var kubeconfigPath = sr.ReadLine();
                    return new VMEndpoint(name, ip, sshPort, kubeconfigPath);
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
