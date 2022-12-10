using wordslab.manager.config;
using wordslab.manager.os;
using wordslab.manager.storage;
using wordslab.manager.vm.qemu;
using wordslab.manager.vm.wsl;

namespace wordslab.manager.vm
{
    public class VirtualMachinesManager
    {
        private HostStorage hostStorage;
        private ConfigStore configStore;

        public VirtualMachinesManager(HostStorage hostStorage, ConfigStore configStore)
        {
            this.hostStorage = hostStorage;
            this.configStore = configStore;
        }

        public async Task<HostMachineConfig> ConfigureHostMachine(InstallProcessUI installUI)
        {
            var cmd1 = installUI.DisplayCommandLaunch($"Checking if host machine {OS.GetMachineName()} is already configured");
            if (configStore.HostMachineConfig != null)
            {
                installUI.DisplayCommandResult(cmd1, true);
                return configStore.HostMachineConfig;
            }
            else
            {
                installUI.DisplayCommandResult(cmd1, false);
            }

            HostMachineConfig machineConfig = null;
            if (OS.IsWindows)
            {
                machineConfig = await WslVMInstaller.ConfigureHostMachine(hostStorage, installUI);
            }
            else if (OS.IsLinux || OS.IsMacOS)
            {
                machineConfig = await QemuVMInstaller.ConfigureHostMachine(hostStorage, installUI);
            }
            if (machineConfig != null)
            {
                configStore.InitializeHostMachineConfig(machineConfig);
            }
            return configStore.HostMachineConfig;
        }

        internal void CheckLocalVMConfig(VirtualMachineConfig vmConfig, ConfigStore configStore, HostStorage hostStorage)
        {
            // Check if a virtual machine with the same name already exists
            if (TryFindLocalVM(vmConfig.Name) != null)
            {
                throw new Exception($"A virtual machine with the name {vmConfig.Name} already exists");
            }

            // Check if the host machine sandbox was configured
            if (configStore.HostMachineConfig == null)
            {
                throw new Exception("You need to configure the host machine sandbox first");
            }
            var hostConfig = configStore.HostMachineConfig;

            // Checking host machine resources
            if (vmConfig.VmProvider == VirtualMachineProvider.Wsl || vmConfig.VmProvider == VirtualMachineProvider.Qemu)
            {
                var errors = new List<string>();

                // Checking compute requirements
                var requestedProcessors = vmConfig.Spec.Compute.Processors;
                var availableProcessors = hostConfig.Processors;
                if (requestedProcessors < VMRequirements.MIN_VM_PROCESSORS)
                {
                    errors.Add($"Not enough processors requested to create the VM: {requestedProcessors} processors requested / {VMRequirements.MIN_VM_PROCESSORS} processors minimum");
                }
                if (availableProcessors < requestedProcessors)
                {
                    errors.Add($"{requestedProcessors} processors are requested to create the VM but ony {availableProcessors} processors are allowed on the host machine");
                }
                var requestedMemoryGB = vmConfig.Spec.Compute.MemoryGB;
                var availableMemoryGB = hostConfig.MemoryGB;
                if (requestedMemoryGB < VMRequirements.MIN_VM_MEMORY_GB)
                {
                    errors.Add($"Not enough memory requested to create the VM: {requestedMemoryGB} GB requested / {VMRequirements.MIN_VM_MEMORY_GB} GB minimum");
                }
                if (availableMemoryGB < requestedMemoryGB)
                {
                    errors.Add($"{requestedMemoryGB} GB memory are requested to create the VM but ony {availableMemoryGB} GB are allowed on the host machine");
                }

                // Checking GPU requirements (local GPUs are time shared between all the VMs)
                var requestedGPUCount = vmConfig.Spec.GPU.GPUCount;
                var canUseGPUs = hostConfig.CanUseGPUs;
                if (requestedGPUCount > 0 && !canUseGPUs)
                {
                    errors.Add($"A GPU is requested to create the VM but GPU access is disabled on this host machine");
                }

                // Checking network ports requirements
                // A range of 10 ports is allowed above the sandbox port
                var portsName = new string[] { "SSH", "Kubernetes", "HTTP", "HTTPS" };
                var forwardsRequested = new bool[] { vmConfig.ForwardSSHPortOnLocalhost, vmConfig.ForwardKubernetesPortOnLocalhost, vmConfig.ForwardHttpIngressPortOnLocalhost, vmConfig.ForwardHttpsIngressPortOnLocalhost };
                var requestedPorts = new int[] { vmConfig.HostSSHPort, vmConfig.HostKubernetesPort, vmConfig.HostHttpIngressPort, vmConfig.HostHttpsIngressPort };
                var allowedPorts = new int[] { hostConfig.SSHPort, hostConfig.KubernetesPort, hostConfig.HttpPort, hostConfig.HttpsPort };
                for (var i = 0; i < portsName.Length; i++)
                {
                    var portName = portsName[i];
                    var forwardRequested = forwardsRequested[i];
                    var requestedPort = requestedPorts[i];
                    var allowedPort = allowedPorts[i];
                    if (forwardRequested && requestedPort - allowedPort >= 10)
                    {
                        errors.Add($"Host port requested for {portName}: {requestedPort} is not in the range allowed on the host machine [{allowedPort}-{allowedPort + 9}], please select another port");
                    }
                }
                if (vmConfig.AllowHttpAccessFromLAN && !hostConfig.CanExposeHttpOnLAN)
                {
                    errors.Add("HTTP access from LAN is requested for the VM, but this is not allowed by the host machine config");
                }
                if (vmConfig.AllowHttpsAccessFromLAN && !hostConfig.CanExposeHttpsOnLAN)
                {
                    errors.Add("HTTPS access from LAN is requested for the VM, but this is not allowed by the host machine config");
                }

                if (errors.Count > 0)
                {
                    throw new Exception($"VM configuration is invalid: {String.Join(". ", errors)}");
                }
            }
        }

        public async Task<VirtualMachineConfig> CreateLocalVMConfig(string vmName, VirtualMachineSpec? vmPresetSpec, HostMachineConfig hostConfig, InstallProcessUI installUI)
        {
            installUI.DisplayInstallStep(1, 2, $"Configure the new virtual machine: {vmName}");

            var vmProvider = OS.IsWindows ? VirtualMachineProvider.Wsl : VirtualMachineProvider.Qemu;

            // Option 1: shortcut - use a preset spec -> goal = no user interaction 
            if (vmPresetSpec != null)
            {
                var cmd = installUI.DisplayCommandLaunch("Using the default config passed as a parameter");                

                var vmConfig = new VirtualMachineConfig(vmName, vmPresetSpec,  vmProvider, 
                    forwardHttpIngressPortOnLocalhost: true, hostHttpIngressPort: hostConfig.HttpPort, allowHttpAccessFromLAN: hostConfig.CanExposeHttpOnLAN,
                    forwardHttpsIngressPortOnLocalhost: true, hostHttpsIngressPort: hostConfig.HttpsPort, allowHttpsAccessFromLAN: hostConfig.CanExposeHttpsOnLAN);

                installUI.DisplayCommandResult(cmd, true, $"{vmPresetSpec.Compute.Processors} processors, {vmPresetSpec.Compute.MemoryGB} GB memory, {(vmPresetSpec.GPU.GPUCount>0?"use GPU":"no GPU")}, cluster disk: max {vmPresetSpec.Storage.ClusterDiskSizeGB} GB, data disk: max {vmPresetSpec.Storage.ClusterDiskSizeGB} GB");

                return vmConfig;
            }
            // Option 2: expert mode - set each property via user interaction 
            else
            {
                try
                {
                    var vmSpec = new VirtualMachineSpec();

                    // Compute spec
                    var processors = await installUI.DisplayInputQuestionAsync($"Number of processors (min {VMRequirements.MIN_VM_PROCESSORS} - max {hostConfig.Processors})", hostConfig.Processors.ToString());
                    var memory = await installUI.DisplayInputQuestionAsync($"Memory in GB (min {VMRequirements.MIN_VM_MEMORY_GB} - max {hostConfig.MemoryGB})", hostConfig.MemoryGB.ToString());
                    vmSpec.Compute.Processors = int.Parse(processors);
                    vmSpec.Compute.MemoryGB = int.Parse(memory);

                    // GPU spec
                    if (hostConfig.CanUseGPUs)
                    {
                        var bestGPU = Compute.GetNvidiaGPUsInfo().OrderByDescending(gpu => (int)gpu.Architecture).ThenByDescending(gpu => gpu.MemoryMB).FirstOrDefault();
                        if (bestGPU != null)
                        {
                            var useGPU = await installUI.DisplayQuestionAsync($"Use Nvidia GPU: {bestGPU.ModelName} {bestGPU.MemoryMB/1024} GB");
                            if (useGPU)
                            {
                                vmSpec.GPU = new GPUSpec() { ModelName = bestGPU.ModelName, MemoryGB = bestGPU.MemoryMB / 1024, GPUCount = 1 };
                            }
                        }
                    }

                    // Storage spec
                    vmSpec.Storage.ClusterDiskIsSSD = Storage.IsPathOnSSD(hostConfig.VirtualMachineClusterPath);
                    vmSpec.Storage.DataDiskIsSSD = Storage.IsPathOnSSD(hostConfig.VirtualMachineDataPath);
                    var clusterDiskSize = await installUI.DisplayInputQuestionAsync($"Max cluster disk size in GB (min {VMRequirements.MIN_VM_CLUSTERDISK_GB} - max {hostConfig.VirtualMachineClusterSizeGB})", hostConfig.VirtualMachineClusterSizeGB.ToString());
                    var dataDiskSize = await installUI.DisplayInputQuestionAsync($"Max data disk size in GB (min {VMRequirements.MIN_VM_DATADISK_GB} - max {hostConfig.VirtualMachineDataSizeGB})", hostConfig.VirtualMachineDataSizeGB.ToString());
                    vmSpec.Storage.ClusterDiskSizeGB = int.Parse(clusterDiskSize);
                    vmSpec.Storage.DataDiskSizeGB = int.Parse(dataDiskSize);

                    // Network config
                    var forwardHttpIngressPortOnLocalhost = true;
                    var hostHttpIngressPort = hostConfig.HttpPort;
                    var allowHttpAccessFromLAN = hostConfig.CanExposeHttpOnLAN;
                    var forwardHttpsIngressPortOnLocalhost = true;
                    var hostHttpsIngressPort = hostConfig.HttpsPort;
                    var allowHttpsAccessFromLAN = hostConfig.CanExposeHttpsOnLAN;
                    var customNetworkConfig = await installUI.DisplayQuestionAsync("Do you want to customize the VM network config ?");
                    if(customNetworkConfig)
                    {
                        if (OS.IsWindows)
                        {
                            forwardHttpIngressPortOnLocalhost = await installUI.DisplayQuestionAsync("Do you want to forward the HTTP ingress port to the host machine ?");
                        }
                        if(forwardHttpIngressPortOnLocalhost)
                        {
                            var httpPort = await installUI.DisplayInputQuestionAsync("Choose a port for HTTP ingress on the host machine", $"{hostConfig.HttpPort}");
                            hostHttpIngressPort = int.Parse(httpPort);
                            allowHttpAccessFromLAN = await installUI.DisplayQuestionAsync("Do you want to allow acces to the HTTP ingress port from the local network (firewall config) ?");
                        }
                        if (OS.IsWindows)
                        {
                            forwardHttpsIngressPortOnLocalhost = await installUI.DisplayQuestionAsync("Do you want to forward the HTTPS ingress port to the host machine ?");
                        }
                        if (forwardHttpsIngressPortOnLocalhost)
                        {
                            var httpsPort = await installUI.DisplayInputQuestionAsync("Choose a port for HTTPS ingress on the host machine", $"{hostConfig.HttpsPort}");
                            hostHttpsIngressPort = int.Parse(httpsPort);
                            allowHttpsAccessFromLAN = await installUI.DisplayQuestionAsync("Do you want to allow acces to the HTTPS ingress port from the local network (firewall config) ?");
                        }
                    }

                    var vmConfig = new VirtualMachineConfig(vmName, vmSpec, vmProvider,
                    forwardHttpIngressPortOnLocalhost: forwardHttpIngressPortOnLocalhost, hostHttpIngressPort: hostHttpIngressPort, allowHttpAccessFromLAN: allowHttpAccessFromLAN,
                    forwardHttpsIngressPortOnLocalhost: forwardHttpsIngressPortOnLocalhost, hostHttpsIngressPort: hostHttpsIngressPort, allowHttpsAccessFromLAN: allowHttpsAccessFromLAN);

                    return vmConfig;
                }
                catch(Exception ex) 
                {
                    installUI.DisplayCommandError($"Failed to parse user input: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<VirtualMachine> CreateLocalVM(VirtualMachineConfig vmConfig, InstallProcessUI installUI)
        {
            installUI.DisplayInstallStep(2, 2, $"Create the new virtual machine: {vmConfig.Name}");

            // Check if the vm configuration is compatible with the sandbox defined on the host machine
            CheckLocalVMConfig(vmConfig, configStore, hostStorage);

            // Create the virtual machine on disk
            if (OS.IsWindows)
            {
                vmConfig = await WslVMInstaller.CreateVirtualMachine(vmConfig, configStore.HostMachineConfig, hostStorage, installUI);
            }
            else if (OS.IsLinux || OS.IsMacOS)
            {
                vmConfig = await QemuVMInstaller.CreateVirtualMachine(vmConfig, configStore.HostMachineConfig, hostStorage, installUI);
            }

            // Store the virtual machine configuration in database
            if(vmConfig != null)
            {
                // Create and register VM config
                configStore.AddVirtualMachineConfig(vmConfig);
                configStore.SaveChanges();
            }
            else
            {
                return null;
            }

            // Initialize a VirtualMachine object to operate the local VM
            return TryFindLocalVM(vmConfig.Name);
        }

        public List<VirtualMachine> ListLocalVMs()
        {
            // List vms found on disk
            VirtualMachineProvider localVMProvider = VirtualMachineProvider.Unspecified;
            List<string> vmNames = null;
            if (OS.IsWindows)
            {
                localVMProvider = VirtualMachineProvider.Wsl;
                vmNames = WslVM.ListLocalVMs(hostStorage);
            }
            else if (OS.IsLinux || OS.IsMacOS)
            {
                localVMProvider = VirtualMachineProvider.Qemu;
                vmNames = QemuVM.ListLocalVMs(hostStorage);
            }
            var vmNamesOnDisk = new HashSet<string>(vmNames);

            // List configs found in database            
            var vmConfigs = configStore.VirtualMachines.Where(vm => vm.VmProvider == localVMProvider);
            var vmNamesInDatabase = new HashSet<string>(vmConfigs.Select(config => config.Name));

            // The TRUTH is on disk => align the database
            // Remove configs found in the database but not found on disk
            var vmNamesInDatabaseButNotOnDisk = vmNamesInDatabase.Except(vmNamesOnDisk).ToList();
            if (vmNamesInDatabaseButNotOnDisk.Count > 0)
            {
                foreach (var vmNameNotFoundOnDisk in vmNamesInDatabaseButNotOnDisk)
                {
                    configStore.RemoveVirtualMachineConfig(vmNameNotFoundOnDisk);
                }
                configStore.SaveChanges();
            }

            // Initialize VirtualMachine objects
            var vms = new List<VirtualMachine>();
            foreach (var vmName in vmNames)
            {
                var vmConfig = configStore.TryGetVirtualMachineConfig(vmName);
                if (vmConfig == null)
                {
                    throw new Exception($"Your host machine is in an inconsistent state: virtual machine '{vmName}' found on disk but not the configuration database");
                }
                try
                {
                    var vm = TryFindLocalVM(vmName);
                    vms.Add(vm);
                }
                catch(Exception e)
                {

                    throw new Exception($"Your host machine is in an inconsistent state: virtual machine '{vmName}' found in the configuration database but not on disk");
                }
            }         
            
            return vms;
        }

        public VirtualMachine TryFindLocalVM(string vmName)
        {
            // Try to find the virtual machine configuration in the database
            var vmConfig = configStore.TryGetVirtualMachineConfig(vmName);
            if (vmConfig == null)
            {
                return null;
            }

            // Try to find the virtual machine on disk
            VirtualMachine vm = null;
            if (OS.IsWindows)
            {
                vm = WslVM.TryFindByName(vmConfig, configStore, hostStorage);
            }
            else if (OS.IsLinux || OS.IsMacOS)
            {
                vm = QemuVM.TryFindByName(vmConfig, configStore, hostStorage);
            }
            return vm;
        }

        public async Task<bool> DeleteLocalVM(string vmName, InstallProcessUI installUI)
        {
            try
            {
                // Check if virtual machine exists
                var localVM = TryFindLocalVM(vmName);
                if (localVM == null)
                {
                    installUI.DisplayCommandError($"Nothing to delete: could not find a local virtual machine named '{vmName}'");
                    return true;
                }

                // Check if virtual machine is running
                if (localVM.IsRunning())
                {
                    var c1 = installUI.DisplayCommandLaunch($"Stopping local virtual machine '{vmName}'");
                    localVM.Stop();
                    installUI.DisplayCommandResult(c1, true);
                }

                // Delete virtual machine on disk
                var confirm = await installUI.DisplayQuestionAsync($"Are you sure you want to delete the local virtual machine '{vmName}' ? ALL DATA WILL BE LOST !!");
                if (confirm)
                {
                    var c2 = installUI.DisplayCommandLaunch($"Deleting local virtual machine '{vmName}'");
                    localVM.ClusterDisk.Delete();
                    localVM.DataDisk.Delete();
                    installUI.DisplayCommandResult(c2, true);

                    // If it was successful, delete virtual machine in database
                    configStore.RemoveVirtualMachineConfig(vmName);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                installUI.DisplayCommandError(ex.Message);
                return false;
            }
        }
    }
}
