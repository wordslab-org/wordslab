using System.Text.RegularExpressions;
using wordslab.manager.config;
using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm
{
    public abstract class VirtualMachine : IVirtualMachineShell
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

            // Initialize the running state
            IsRunning();
        }

        private static readonly Regex NAME_REGEX = new Regex("^[a-z0-9-]+$");

        public string Name { get; init; }

        public VirtualMachineConfig Config {  get; init; }

        public VirtualDisk ClusterDisk { get; init; }

        public VirtualDisk DataDisk { get; init; }

        protected abstract int FindRunningProcessId();

        public bool IsRunning()
        {
            // Check if the distribution is currently running on the host machine
            var vmProcessId = FindRunningProcessId();
            var vmProcessFound = vmProcessId >= 0;

            // Get the last instance found in the database
            RunningInstance = configStore.TryGetLastVirtualMachineInstance(Name);

            // If this VM is currently stopped
            if (!vmProcessFound)
            {
                // Database state: never launched or failed / stopped / killed instance
                if (RunningInstance == null || (RunningInstance.State != VirtualMachineState.Starting && RunningInstance.State != VirtualMachineState.Running))
                {
                    // State is consistent, nothing to do
                }
                // Database state: instance starting / running
                else
                {
                    // State is inconsistent, fix the database state
                    Kill(RunningInstance, $"A running process for virtual machine '{Name}' was not found: it was killed outside of wordslab manager");
                }
                RunningInstance = null;
            }

            // If this VM is currently running
            if (vmProcessFound)
            {
                // Database state: never launched or failed / stopped / killed instance
                if (RunningInstance == null || (RunningInstance.State != VirtualMachineState.Starting && RunningInstance.State != VirtualMachineState.Running))
                {
                    // VM was launched outside of wordslab manager and must now be stopped outside wordslab manager to resynchronize state
                    throw new InvalidOperationException($"The virtual machine {Name} was launched outside of wordslab manager: please stop it with native commands before you can use it from within wordslab manager again");
                }

                // Database state: instance starting / running but with a different process ID
                if(RunningInstance != null && 
                    (RunningInstance.State == VirtualMachineState.Starting || RunningInstance.State == VirtualMachineState.Running) &&
                    RunningInstance.VmProcessId != vmProcessId)
                {
                    // State is inconsistent, fix the database state
                    Kill(RunningInstance, $"A running process with the right process id for virtual machine '{Name}' was not found: it was killed outside of wordslab manager");

                    // VM was launched outside of wordslab manager and must now be stopped outside wordslab manager to resynchronize state
                    throw new InvalidOperationException($"The virtual machine {Name} was launched outside of wordslab manager: please stop it with native commands before you can use it from within wordslab manager again");
                }
            }

            return vmProcessFound;
        }

        public VirtualMachineInstance RunningInstance { get; protected set; }

        public abstract VirtualMachineInstance Start(ComputeSpec computeStartArguments = null, GPUSpec gpuStartArguments = null);

        public abstract void Stop();

        protected void Kill(VirtualMachineInstance instance, string message)
        {
            instance.Killed(message);
            configStore.SaveChanges();

            CleanupAfterStopOrKill();
        }

        protected abstract void CleanupAfterStopOrKill();

        public abstract int ExecuteCommand(string command, string commandArguments = "", int timeoutSec = 10, Action<string> outputHandler = null, Action<string> errorHandler = null, Action<int> exitCodeHandler = null);

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

        // Display

        public class DisplayStatus
        {
            public string FirstStart;
            public string LastStop;
            public string LastState;
            public string TotalTime;
            public string Processors;
            public string Memory;
            public string GPU;
        }

        public DisplayStatus GetDisplayStatus()
        {
            var firstInstance = configStore.TryGetFirstVirtualMachineInstance(Name);
            var lastInstance = configStore.TryGetLastVirtualMachineInstance(Name);
            var totalRunningTime = configStore.GetVirtualMachineInstanceTotalRunningTime(Name);

            var status = new DisplayStatus();

            status.FirstStart = firstInstance == null ? "" : firstInstance.StartTimestamp.ToString("MM/dd/yy HH:mm:ss");
            status.LastStop = lastInstance == null || !lastInstance.StopTimestamp.HasValue ? "" : lastInstance.StopTimestamp.Value.ToString("MM/dd/yy HH:mm:ss");
            status.LastState = lastInstance == null ? "" : lastInstance.State.ToString().ToLowerInvariant();
            status.TotalTime = totalRunningTime.ToString(@"d\.hh\:mm\:ss");
            status.Processors = Config.Spec.Compute.Processors.ToString();
            status.Memory = $"{Config.Spec.Compute.MemoryGB} GB";
            status.GPU = Config.Spec.GPU.ToString();
            return status;
        }

        // --- wordslab virtual machine software ---

        // Versions last updated : February 05 2023

        // Rancher k3s releases: https://github.com/k3s-io/k3s/releases/latest
        internal static readonly string k3sVersion = "1.26.1+k3s1";
        internal static readonly string k3sExecutableURL = $"https://github.com/k3s-io/k3s/releases/download/v{k3sVersion}/k3s";
        internal static readonly int    k3sExecutableSize = 66752512;
        internal static readonly string k3sExecutableFileName = $"k3s-{k3sVersion}";
        internal static readonly string k3sImagesURL = $"https://github.com/k3s-io/k3s/releases/download/v{k3sVersion}/k3s-airgap-images-amd64.tar.gz";
        internal static readonly int    k3sImagesDownloadSize = 188407521;
        internal static readonly int    k3sImagesDiskSize = 554935808;
        internal static readonly string k3sImagesFileName = $"k3s-airgap-images-{k3sVersion}.tar";

        // Helm releases: https://github.com/helm/helm/releases/latest
        internal static readonly string helmVersion = "3.11.0";
        internal static readonly string helmExecutableURL = $"https://get.helm.sh/helm-v{helmVersion}-linux-amd64.tar.gz";
        internal static readonly int    helmExecutableDownloadSize = 15023353;
        internal static readonly int    helmExecutableDiskSize = 46870528;
        internal static readonly string helmFileName = $"helm-{helmVersion}.tar";

        // nerdctl releases: https://github.com/containerd/nerdctl/releases/latest
        internal static readonly string nerdctlVersion = "1.2.0";
        internal static readonly string nerdctlBundleURL = $"https://github.com/containerd/nerdctl/releases/download/v{nerdctlVersion}/nerdctl-full-{nerdctlVersion}-linux-amd64.tar.gz";
        internal static readonly int    nerdctlBundleDownloadSize = 241421255;
        //internal static readonly int  nerdctlBundleDiskSize = 567968768;
        internal static readonly int    nerdctlExecutableDiskSize = 25710592;
        internal static readonly string nerdctlFileName = $"nerdctl-full-{nerdctlVersion}.tar";

        // nvidia container runtime versions:
        // Don't use: https://github.com/NVIDIA/nvidia-container-runtime/releases/latest 
        // Use this command to get the latest version: apt show nvidia-container-runtime
        internal static readonly string nvidiaContainerRuntimeVersion = "3.12.0-1";
    }    
}
