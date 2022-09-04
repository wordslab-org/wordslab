using System.Text.RegularExpressions;
using wordslab.manager.storage;
using wordslab.manager.storage.config;

namespace wordslab.manager.vm
{
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

        public int HostSSHPort { get; internal set; }
        public int HostKubernetesPort { get; internal set; }
        public int HostHttpIngressPort { get; internal set; }

        public abstract bool IsRunning();

        public abstract VirtualMachineEndpoint Start(int? processors = null, int? memoryGB = null, int? hostSSHPort = null, int? hostKubernetesPort = null, int? hostHttpIngressPort = null);

        public VirtualMachineEndpoint Start(VirtualMachineConfig vmConfig)
        {
            return Start(vmConfig.Processors, vmConfig.MemoryGB, vmConfig.HostSSHPort, vmConfig.HostKubernetesPort, vmConfig.HostHttpIngressPort);
        }

        protected VirtualMachineEndpoint endpoint;

        public VirtualMachineEndpoint Endpoint
        {
            get
            {
                if(endpoint == null)
                {
                    if(IsRunning())
                    {
                        endpoint = VirtualMachineEndpoint.Load(storage, Name);
                    }
                }
                return endpoint;
            }
        }

        public abstract void Stop();

        // --- wordslab virtual machine software ---

        // Versions last updated : August 16 2022

        // Rancher k3s releases: https://github.com/k3s-io/k3s/releases/
        internal static readonly string k3sVersion = "1.24.3+k3s1";
        internal static readonly string k3sExecutableURL = $"https://github.com/k3s-io/k3s/releases/download/v{k3sVersion}/k3s";
        internal static readonly int    k3sExecutableSize = 65921024;
        internal static readonly string k3sExecutableFileName = $"k3s-{k3sVersion}";
        internal static readonly string k3sImagesURL = $"https://github.com/k3s-io/k3s/releases/download/v{k3sVersion}/k3s-airgap-images-amd64.tar.gz";
        internal static readonly int    k3sImagesDownloadSize = 167319501;
        internal static readonly int    k3sImagesDiskSize = 504174592;
        internal static readonly string k3sImagesFileName = $"k3s-airgap-images-{k3sVersion}.tar";

        // Helm releases: https://github.com/helm/helm/releases
        internal static readonly string helmVersion = "3.9.3";
        internal static readonly string helmExecutableURL = $"https://get.helm.sh/helm-v{helmVersion}-linux-amd64.tar.gz";
        internal static readonly int    helmExecutableDownloadSize = 14025325;
        internal static readonly int    helmExecutableDiskSize = 46374912;
        internal static readonly string helmFileName = $"heml-{helmVersion}.tar";

        // nvidia container runtime versions: https://github.com/NVIDIA/nvidia-container-runtime/releases
        internal static readonly string nvidiaContainerRuntimeVersion = "3.10.0-1";
    }    
}
