using System.Text.RegularExpressions;
using wordslab.manager.config;
using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm
{
    public abstract class VirtualMachine
    {
        protected HostStorage storage;
        protected ConfigStore configStore;

        protected VirtualMachine(VirtualMachineConfig vmConfig, VirtualDisk clusterDisk, VirtualDisk dataDisk, ConfigStore configStore, HostStorage storage)
        {
            if (!NAME_REGEX.IsMatch(vmConfig.Name))
            {
                throw new ArgumentException("A virtual machine name can only contain lowercase letters, digits and -");
            }
            else
            {
                Name = vmConfig.Name;
            }
            Config = vmConfig;
            ClusterDisk = clusterDisk;
            DataDisk = dataDisk;
            this.configStore = configStore;
            this.storage = storage;
        }

        private static readonly Regex NAME_REGEX = new Regex("^[a-z0-9-]+$");

        public string Name { get; init; }

        public VirtualMachineConfig Config {  get; init; }

        public VirtualDisk ClusterDisk { get; init; }

        public VirtualDisk DataDisk { get; init; }

        public abstract bool IsRunning();

        public VirtualMachineInstance RunningInstance { get; protected set; }

        public abstract VirtualMachineInstance Start(ComputeSpec computeStartArguments = null, GPUSpec gpuStartArguments = null);

        public abstract void Stop();

        protected VirtualMachineInstance CheckStartArgumentsAndCreateInstance(ComputeSpec computeStartArguments, GPUSpec gpuStartArguments)
        {
            // Applying VM config
            if (computeStartArguments == null) computeStartArguments = Config.Spec.Compute;
            if (gpuStartArguments == null) gpuStartArguments = Config.Spec.GPU;
         
            var warnings = new List<string>();

            // Checking host machine resources
            if (Config.VmProvider == VirtualMachineProvider.Wsl || Config.VmProvider == VirtualMachineProvider.Qemu)
            {
                // Computing resources already used by other VMs
                var usedProcessors = 0;
                var usedMemoryGB = 0;
                var runningVms = configStore.VirtualMachineInstances.Where(vmi => vmi.Config.VmProvider == Config.VmProvider && (vmi.State == VirtualMachineState.Running || vmi.State == VirtualMachineState.Starting));
                foreach(var vmi in runningVms)
                {
                    usedProcessors += vmi.ComputeStartArguments.Processors;
                    usedMemoryGB += vmi.ComputeStartArguments.MemoryGB;
                }

                // Computing resources still available inside the host sandbox
                var hostConfig = configStore.HostMachineConfig;
                var availableProcessors = hostConfig.Processors - usedProcessors;
                var availableMemoryGB = hostConfig.MemoryGB - usedMemoryGB;

                // Checking compute requirements
                if(availableProcessors < VMRequirements.MIN_VM_PROCESSORS)
                {
                    throw new InvalidOperationException($"Not enough processors available on the host machine to start a VM: {hostConfig.Processors} allowed, {usedProcessors} used by other vms, {VMRequirements.MIN_VM_PROCESSORS} required to start a VM");
                }
                if (availableProcessors < computeStartArguments.Processors)
                {
                    warnings.Add($"{computeStartArguments.Processors} requested but ony {availableProcessors} processors are available on the host machine to start a VM: ({hostConfig.Processors} allowed, {usedProcessors} used by other vms)");
                    computeStartArguments.Processors = hostConfig.Processors;
                }
                if (availableMemoryGB < VMRequirements.MIN_VM_MEMORY_GB)
                {
                    throw new InvalidOperationException($"Not enough memory available on the host machine to start a VM: {hostConfig.MemoryGB} GB allowed, {usedMemoryGB} GB used by other vms, {VMRequirements.MIN_VM_MEMORY_GB} GB required to start a VM");
                }
                if (availableMemoryGB < computeStartArguments.MemoryGB)
                {
                    warnings.Add($"{computeStartArguments.MemoryGB} GB memory requested but ony {availableMemoryGB} GB are available on the host machine to start a VM: ({hostConfig.MemoryGB} GB allowed, {usedMemoryGB} GB used by other vms)");
                    computeStartArguments.Processors = hostConfig.Processors;
                }

                // Checking GPU requirements (local GPUs are time shared between all the VMs)
                if(gpuStartArguments.GPUCount > 0 && !hostConfig.CanUseGPUs)
                {
                    warnings.Add($"Can not launch VM with {gpuStartArguments.GPUCount} GPUs as requested: GPU access is disabled on this host machine");
                    gpuStartArguments.GPUCount = 0;
                    gpuStartArguments.ModelName = "";
                    gpuStartArguments.MemoryGB = 0;
                }

                // Checking network ports requirements
                // Check if the requested ports are available right before startup
                var usedPorts = Network.GetAllTcpPortsInUse();
                if (Config.ForwardSSHPortOnLocalhost && usedPorts.Contains(Config.HostSSHPort))
                {
                    throw new InvalidOperationException($"Host port for SSH: {Config.HostSSHPort} is already in use, please select another port");
                }
                if (Config.ForwardKubernetesPortOnLocalhost && usedPorts.Contains(Config.HostKubernetesPort))
                {
                    throw new InvalidOperationException($"Host port for Kubernetes: {Config.HostKubernetesPort} is already in use, please select another port");
                }
                if (Config.ForwardHttpIngressPortOnLocalhost && usedPorts.Contains(Config.HostHttpIngressPort))
                {
                    throw new InvalidOperationException($"Host port for HTTP ingress: {Config.HostHttpIngressPort} is already in use, please select another port");
                }
                if (Config.ForwardHttpsIngressPortOnLocalhost && usedPorts.Contains(Config.HostHttpsIngressPort))
                {
                    throw new InvalidOperationException($"Host port for HTTPS ingress: {Config.HostHttpsIngressPort} is already in use, please select another port");
                }
            }

            // Create and register VM instance
            var vmInstance = new VirtualMachineInstance(Name, Config, computeStartArguments, gpuStartArguments, warnings);
            configStore.AddVirtualMachineInstance(vmInstance);
            configStore.SaveChanges();
            return vmInstance;
        }

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
        internal static readonly string nvidiaContainerRuntimeVersion = "3.11.0";
    }    
}
